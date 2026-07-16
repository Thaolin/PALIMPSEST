#include "pali.h"
#include "platform.h"
#include "save.h"
#include "world.h"

#include <math.h>
#include <stdio.h>
#include <string.h>

static int failures = 0;
static World world_a;
static World world_b;
static World world_c;
static World loaded_world;
static World knowledge_world;
static World behavior_world;
static UniverseState universe_snapshot;
static uint8_t save_mutation[16384];
static uint8_t legacy_save[16384];

#define CHECK(condition, message)                                             \
    do {                                                                      \
        if (!(condition)) {                                                   \
            (void)fprintf(stderr, "FAIL %s:%d: %s\n", __FILE__, __LINE__,   \
                          (message));                                         \
            failures++;                                                       \
        }                                                                     \
    } while (0)

static bool rewrite_save_bytes(const char *path, size_t offset,
                               const uint8_t *replacement, size_t count) {
    FILE *file = fopen(path, "rb+");
    if (file == NULL || fseek(file, 0, SEEK_END) != 0) {
        if (file != NULL) {
            (void)fclose(file);
        }
        return false;
    }
    const long measured = ftell(file);
    if (measured < 24 || (unsigned long)measured > sizeof(save_mutation) ||
        fseek(file, 0, SEEK_SET) != 0) {
        (void)fclose(file);
        return false;
    }
    const size_t length = (size_t)measured;
    if (offset > length || count > length - offset ||
        fread(save_mutation, 1, length, file) != length) {
        (void)fclose(file);
        return false;
    }
    memcpy(save_mutation + offset, replacement, count);
    uint64_t hash = UINT64_C(1469598103934665603);
    for (size_t index = 24; index < length; ++index) {
        hash ^= save_mutation[index];
        hash *= UINT64_C(1099511628211);
    }
    for (int index = 0; index < 8; ++index) {
        save_mutation[16u + (size_t)index] =
            (uint8_t)(hash >> (index * 8));
    }
    const bool rewound = fseek(file, 0, SEEK_SET) == 0;
    const bool written =
        rewound && fwrite(save_mutation, 1, length, file) == length;
    const bool closed = fclose(file) == 0;
    return written && closed;
}

static bool find_save_bytes(const char *path, const char *needle,
                            size_t *out_offset) {
    FILE *file = fopen(path, "rb");
    if (file == NULL || fseek(file, 0, SEEK_END) != 0) {
        if (file != NULL) {
            (void)fclose(file);
        }
        return false;
    }
    const long measured = ftell(file);
    if (measured < 0 || (unsigned long)measured > sizeof(save_mutation) ||
        fseek(file, 0, SEEK_SET) != 0) {
        (void)fclose(file);
        return false;
    }
    const size_t length = (size_t)measured;
    const size_t needle_length = strlen(needle);
    const bool read_complete =
        fread(save_mutation, 1, length, file) == length;
    const bool read_ok = fclose(file) == 0 && read_complete;
    if (!read_ok || needle_length == 0 || needle_length > length) {
        return false;
    }
    for (size_t offset = 0; offset <= length - needle_length; ++offset) {
        if (memcmp(save_mutation + offset, needle, needle_length) == 0) {
            *out_offset = offset;
            return true;
        }
    }
    return false;
}

static void write_u32_le(uint8_t *bytes, uint32_t value) {
    for (int index = 0; index < 4; ++index) {
        bytes[index] = (uint8_t)(value >> (index * 8));
    }
}

static void write_u16_le(uint8_t *bytes, uint16_t value) {
    bytes[0] = (uint8_t)(value & UINT16_C(0xff));
    bytes[1] = (uint8_t)(value >> 8);
}

static uint32_t read_u32_le(const uint8_t *bytes) {
    uint32_t value = 0;
    for (int index = 0; index < 4; ++index) {
        value |= (uint32_t)bytes[index] << (index * 8);
    }
    return value;
}

static bool remove_save_bytes(const char *path, size_t offset, size_t count) {
    FILE *file = fopen(path, "rb");
    if (file == NULL || fseek(file, 0, SEEK_END) != 0) {
        if (file != NULL) {
            (void)fclose(file);
        }
        return false;
    }
    const long measured = ftell(file);
    if (measured < 24 || (unsigned long)measured > sizeof(save_mutation) ||
        fseek(file, 0, SEEK_SET) != 0) {
        (void)fclose(file);
        return false;
    }
    const size_t length = (size_t)measured;
    if (offset < 24u || offset > length || count > length - offset) {
        (void)fclose(file);
        return false;
    }
    const bool read_complete = fread(save_mutation, 1, length, file) == length;
    const bool closed = fclose(file) == 0;
    if (!read_complete || !closed) {
        return false;
    }
    const size_t rewritten_length = length - count;
    memmove(save_mutation + offset, save_mutation + offset + count,
            rewritten_length - offset);
    write_u32_le(save_mutation + 12u,
                 (uint32_t)(rewritten_length - 24u));
    uint64_t hash = UINT64_C(1469598103934665603);
    for (size_t index = 24; index < rewritten_length; ++index) {
        hash ^= save_mutation[index];
        hash *= UINT64_C(1099511628211);
    }
    for (int index = 0; index < 8; ++index) {
        save_mutation[16u + (size_t)index] =
            (uint8_t)(hash >> (index * 8));
    }
    file = fopen(path, "wb");
    if (file == NULL) {
        return false;
    }
    const bool written = fwrite(save_mutation, 1, rewritten_length, file) ==
                         rewritten_length;
    return fclose(file) == 0 && written;
}

typedef struct LegacyWire {
    size_t read;
    size_t write;
    size_t length;
    uint64_t descendant_ids[WORLD_MAX_ENTITIES];
    uint16_t descendant_count;
    bool ok;
} LegacyWire;

static bool legacy_skip(LegacyWire *wire, size_t count) {
    if (!wire->ok || wire->read > wire->length ||
        count > wire->length - wire->read) {
        wire->ok = false;
        return false;
    }
    wire->read += count;
    return true;
}

static bool legacy_append(LegacyWire *wire, const void *bytes, size_t count) {
    if (!wire->ok || wire->write > sizeof(legacy_save) ||
        count > sizeof(legacy_save) - wire->write) {
        wire->ok = false;
        return false;
    }
    memcpy(legacy_save + wire->write, bytes, count);
    wire->write += count;
    return true;
}

static bool legacy_copy(LegacyWire *wire, size_t count) {
    if (!wire->ok || wire->read > wire->length ||
        count > wire->length - wire->read) {
        wire->ok = false;
        return false;
    }
    const bool appended =
        legacy_append(wire, save_mutation + wire->read, count);
    wire->read += count;
    return appended;
}

static uint8_t legacy_take_u8(LegacyWire *wire) {
    uint8_t value = 0;
    if (wire->ok && wire->read < wire->length) {
        value = save_mutation[wire->read++];
    } else {
        wire->ok = false;
    }
    return value;
}

static uint16_t legacy_take_u16(LegacyWire *wire) {
    if (!wire->ok || wire->read > wire->length ||
        sizeof(uint16_t) > wire->length - wire->read) {
        wire->ok = false;
        return 0;
    }
    const uint16_t value =
        (uint16_t)((uint16_t)save_mutation[wire->read] |
                   ((uint16_t)save_mutation[wire->read + 1u] << 8));
    wire->read += sizeof(uint16_t);
    return value;
}

static uint64_t legacy_take_u64(LegacyWire *wire) {
    if (!wire->ok || wire->read > wire->length ||
        sizeof(uint64_t) > wire->length - wire->read) {
        wire->ok = false;
        return 0;
    }
    uint64_t value = 0;
    for (int index = 0; index < 8; ++index) {
        value |= (uint64_t)save_mutation[wire->read + (size_t)index]
                 << (index * 8);
    }
    wire->read += sizeof(uint64_t);
    return value;
}

static bool legacy_put_u8(LegacyWire *wire, uint8_t value) {
    return legacy_append(wire, &value, sizeof(value));
}

static bool legacy_put_u16(LegacyWire *wire, uint16_t value) {
    uint8_t bytes[2];
    write_u16_le(bytes, value);
    return legacy_append(wire, bytes, sizeof(bytes));
}

static bool legacy_put_u64(LegacyWire *wire, uint64_t value) {
    uint8_t bytes[8];
    for (int index = 0; index < 8; ++index) {
        bytes[index] = (uint8_t)(value >> (index * 8));
    }
    return legacy_append(wire, bytes, sizeof(bytes));
}

static bool legacy_copy_string(LegacyWire *wire) {
    const size_t start = wire->read;
    const uint16_t length = legacy_take_u16(wire);
    if (!legacy_skip(wire, length)) {
        return false;
    }
    return legacy_append(wire, save_mutation + start,
                         sizeof(uint16_t) + (size_t)length);
}

static bool legacy_skip_string(LegacyWire *wire) {
    const uint16_t length = legacy_take_u16(wire);
    return legacy_skip(wire, length);
}

static bool legacy_copy_value(LegacyWire *wire) {
    const uint8_t type = legacy_take_u8(wire);
    if (!legacy_put_u8(wire, type)) {
        return false;
    }
    switch ((PaliValueType)type) {
        case PALI_VALUE_NIL:
            return true;
        case PALI_VALUE_NUMBER:
            return legacy_copy(wire, sizeof(uint64_t));
        case PALI_VALUE_BOOL:
            return legacy_copy(wire, sizeof(uint8_t));
        case PALI_VALUE_TEXT:
            return legacy_copy_string(wire);
        default:
            wire->ok = false;
            return false;
    }
}

static bool legacy_is_descendant(const LegacyWire *wire, uint64_t id) {
    for (uint16_t index = 0; index < wire->descendant_count; ++index) {
        if (wire->descendant_ids[index] == id) {
            return true;
        }
    }
    return false;
}

static bool downgrade_save_to_version(const char *path, uint32_t version) {
    enum { LEGACY_OBSERVATION_COUNT = 15 };
    if (version < 2u || version > 4u ||
        !save_validate_file(path, NULL)) {
        return false;
    }
    FILE *file = fopen(path, "rb");
    if (file == NULL || fseek(file, 0, SEEK_END) != 0) {
        if (file != NULL) {
            (void)fclose(file);
        }
        return false;
    }
    const long measured = ftell(file);
    if (measured < 24 || (unsigned long)measured > sizeof(save_mutation) ||
        fseek(file, 0, SEEK_SET) != 0) {
        (void)fclose(file);
        return false;
    }
    const size_t length = (size_t)measured;
    const bool read_ok =
        fread(save_mutation, 1, length, file) == length;
    const bool closed = fclose(file) == 0;
    if (!read_ok || !closed ||
        read_u32_le(save_mutation + 8u) != PAL_SAVE_VERSION) {
        return false;
    }

    memcpy(legacy_save, save_mutation, 24u);
    LegacyWire wire = {.read = 24u,
                       .write = 24u,
                       .length = length,
                       .ok = true};
    const size_t common_without_vigor =
        6u * sizeof(uint64_t) + 6u * sizeof(uint32_t) + sizeof(uint8_t);
    (void)legacy_copy(&wire, common_without_vigor);
    (void)legacy_skip(&wire, sizeof(uint32_t));

    const uint8_t prototype_count = legacy_take_u8(&wire);
    (void)legacy_put_u8(&wire, prototype_count);
    for (uint8_t index = 0; index < prototype_count && wire.ok; ++index) {
        (void)legacy_copy(&wire, sizeof(uint8_t));
        (void)legacy_copy_string(&wire);
    }

    wire.descendant_count = legacy_take_u16(&wire);
    if (wire.descendant_count > WORLD_MAX_ENTITIES) {
        wire.ok = false;
    }
    for (uint16_t index = 0;
         index < wire.descendant_count && wire.ok; ++index) {
        wire.descendant_ids[index] = legacy_take_u64(&wire);
        (void)legacy_skip(&wire, sizeof(uint64_t) + sizeof(uint32_t) +
                                    sizeof(uint8_t));
    }

    const uint8_t lineage_count = legacy_take_u8(&wire);
    for (uint8_t index = 0; index < lineage_count && wire.ok; ++index) {
        (void)legacy_skip(&wire, 2u * sizeof(uint64_t));
        (void)legacy_skip_string(&wire);
        (void)legacy_skip(&wire, sizeof(uint32_t) + 2u * sizeof(uint8_t));
    }

    const uint8_t local_count = legacy_take_u8(&wire);
    const size_t local_count_offset = wire.write;
    (void)legacy_put_u8(&wire, 0);
    uint8_t retained_locals = 0;
    for (uint8_t index = 0; index < local_count && wire.ok; ++index) {
        const size_t record_start = wire.write;
        const uint64_t entity_id = legacy_take_u64(&wire);
        (void)legacy_put_u64(&wire, entity_id);
        const uint8_t value_count = legacy_take_u8(&wire);
        (void)legacy_put_u8(&wire, value_count);
        for (uint8_t value = 0; value < value_count && wire.ok; ++value) {
            (void)legacy_copy(&wire, sizeof(uint16_t));
            (void)legacy_copy_value(&wire);
            (void)legacy_skip(&wire, sizeof(uint8_t) + sizeof(uint64_t));
        }
        const uint8_t has_behavior = legacy_take_u8(&wire);
        if (has_behavior > 1u) {
            wire.ok = false;
            break;
        }
        if (version >= 4u) {
            (void)legacy_put_u8(&wire, has_behavior);
        }
        if (has_behavior != 0u) {
            if (version >= 4u) {
                (void)legacy_copy_string(&wire);
            } else {
                (void)legacy_skip_string(&wire);
            }
            (void)legacy_skip(&wire, sizeof(uint8_t) + sizeof(uint64_t));
        }
        if (!legacy_is_descendant(&wire, entity_id) &&
            (version >= 4u || value_count != 0u)) {
            retained_locals++;
        } else {
            wire.write = record_start;
        }
    }
    if (wire.ok) {
        legacy_save[local_count_offset] = retained_locals;
    }

    const uint16_t dirty_count = legacy_take_u16(&wire);
    const size_t dirty_count_offset = wire.write;
    (void)legacy_put_u16(&wire, 0);
    uint16_t retained_dirty = 0;
    for (uint16_t index = 0; index < dirty_count && wire.ok; ++index) {
        const size_t record_start = wire.write;
        const uint64_t entity_id = legacy_take_u64(&wire);
        (void)legacy_put_u64(&wire, entity_id);
        (void)legacy_copy(&wire, sizeof(uint8_t) +
                                    4u * sizeof(uint32_t) +
                                    sizeof(uint64_t) + sizeof(uint16_t));
        (void)legacy_skip(&wire, sizeof(uint32_t) + sizeof(uint16_t));
        const uint8_t state_count = legacy_take_u8(&wire);
        (void)legacy_put_u8(&wire, state_count);
        for (uint8_t state = 0; state < state_count && wire.ok; ++state) {
            (void)legacy_copy_string(&wire);
            (void)legacy_copy_value(&wire);
        }
        if (!legacy_is_descendant(&wire, entity_id)) {
            retained_dirty++;
        } else {
            wire.write = record_start;
        }
    }
    if (wire.ok) {
        write_u16_le(legacy_save + dirty_count_offset, retained_dirty);
    }
    (void)legacy_copy_string(&wire);
    for (ConceptId id = CONCEPT_NONE; id < CONCEPT_COUNT && wire.ok; ++id) {
        if (version >= 3u && id < (ConceptId)LEGACY_OBSERVATION_COUNT) {
            (void)legacy_copy(&wire, sizeof(uint32_t));
        } else {
            (void)legacy_skip(&wire, sizeof(uint32_t));
        }
    }
    if (!wire.ok || wire.read != wire.length || wire.write < 24u ||
        wire.write - 24u > UINT32_MAX) {
        return false;
    }

    write_u32_le(legacy_save + 8u, version);
    write_u32_le(legacy_save + 12u, (uint32_t)(wire.write - 24u));
    uint64_t hash = UINT64_C(1469598103934665603);
    for (size_t index = 24u; index < wire.write; ++index) {
        hash ^= legacy_save[index];
        hash *= UINT64_C(1099511628211);
    }
    for (int index = 0; index < 8; ++index) {
        legacy_save[16u + (size_t)index] =
            (uint8_t)(hash >> (index * 8));
    }
    file = fopen(path, "wb");
    if (file == NULL) {
        return false;
    }
    const bool written =
        fwrite(legacy_save, 1, wire.write, file) == wire.write;
    return fclose(file) == 0 && written;
}

static int first_entity(const World *world, PrototypeId prototype) {
    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        if (world->universe.entities[index].prototype == (uint8_t)prototype &&
            world->universe.entities[index].active) {
            return (int)index;
        }
    }
    return -1;
}

static int next_entity(const World *world, PrototypeId prototype,
                       int after_index) {
    for (uint16_t index = (uint16_t)(after_index + 1);
         index < world->universe.entity_count; ++index) {
        if (world->universe.entities[index].prototype == (uint8_t)prototype &&
            world->universe.entities[index].active) {
            return (int)index;
        }
    }
    return -1;
}

static uint16_t active_child_count(const World *world, uint64_t parent_id) {
    uint16_t count = 0;
    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        const Entity *entity = &world->universe.entities[index];
        if (entity->active && entity->parent_id == parent_id) {
            count++;
        }
    }
    return count;
}

static uint8_t active_lineage_count(const World *world) {
    uint8_t count = 0;
    for (int index = 0; index < WORLD_MAX_LINEAGES; ++index) {
        if (world->universe.lineages[index].active) {
            count++;
        }
    }
    return count;
}

static bool same_behavior_draft(UseBehaviorDraft left,
                                UseBehaviorDraft right) {
    return left.hunger == right.hunger && left.voice == right.voice &&
           left.fate == right.fate &&
           left.aftertaste == right.aftertaste;
}

static bool dummy_get(void *user, PaliTarget target, const char *name,
                      PaliValue *out, PaliError *error) {
    (void)user;
    (void)target;
    (void)name;
    (void)error;
    *out = pali_number(50.0);
    return true;
}

static bool dummy_set(void *user, PaliTarget target, const char *name,
                      PaliValue value, PaliError *error) {
    (void)user;
    (void)target;
    (void)name;
    (void)value;
    (void)error;
    return true;
}

static bool dummy_call(void *user, PaliHostCall call,
                       const PaliValue *argument, PaliError *error) {
    (void)user;
    (void)call;
    (void)argument;
    (void)error;
    return true;
}

static bool rejecting_call(void *user, PaliHostCall call,
                           const PaliValue *argument, PaliError *error) {
    (void)user;
    (void)call;
    (void)argument;
    if (error != NULL) {
        (void)snprintf(error->message, sizeof(error->message),
                       "host rejected the call");
    }
    return false;
}

static bool programs_semantically_equal(const PaliProgram *left,
                                        const PaliProgram *right) {
    if (strcmp(left->prototype_name, right->prototype_name) != 0 ||
        left->property_count != right->property_count ||
        left->constant_count != right->constant_count ||
        left->code_count != right->code_count ||
        left->has_use != right->has_use) {
        return false;
    }
    for (uint16_t index = 0; index < left->property_count; ++index) {
        if (strcmp(left->properties[index].name,
                   right->properties[index].name) != 0 ||
            !pali_value_equal(left->properties[index].value,
                              right->properties[index].value)) {
            return false;
        }
    }
    for (uint16_t index = 0; index < left->constant_count; ++index) {
        if (!pali_value_equal(left->constants[index],
                              right->constants[index])) {
            return false;
        }
    }
    for (uint16_t index = 0; index < left->code_count; ++index) {
        if (left->code[index].op != right->code[index].op ||
            left->code[index].operand != right->code[index].operand) {
            return false;
        }
    }
    return true;
}

static void test_language(void) {
    static const char *valid_source =
        "prototype ration\n"
        "    nutrition = 12\n"
        "    edible = true\n"
        "    label = \"small proof\"\n"
        "    on use(actor)\n"
        "        actor.hunger = max(0, actor.hunger - self.nutrition * 2)\n"
        "        message(self.label)\n"
        "        destroy(self)\n"
        "    end\n"
        "end\n";
    PaliProgram program;
    PaliError error;
    CHECK(pali_compile(valid_source, &program, &error), error.message);
    CHECK(program.property_count == 3, "compiler retained typed properties");
    CHECK(program.has_use, "compiler retained use handler");
    CHECK(pali_program_property(&program, "edible") != NULL &&
              pali_program_property(&program, "edible")->type ==
                  PALI_VALUE_BOOL,
          "Boolean property has Boolean type");
    const ConceptDefinition *nutrition =
        lexicon_find_by_id(CONCEPT_NUTRITION);
    CHECK(nutrition != NULL && strcmp(nutrition->name, "nutrition") == 0 &&
              nutrition->facet == FACET_VITAL &&
              lexicon_find_by_name("nutrition") == nutrition,
          "Lexicon gives nourishment stable typed identity");
    CHECK(lexicon_value_is_valid(nutrition, pali_number(20.0)) &&
              !lexicon_value_is_valid(nutrition, pali_text("twenty")),
          "Lexicon validates typed semantic bounds");
    CHECK(lexicon_value_is_valid(lexicon_find_by_id(CONCEPT_MASS),
                                 pali_number(24000.0)),
          "Lexicon admits the mass of every shipped Prototype");

    PaliDocument document;
    PaliDocument reparsed;
    PaliProgram normalized_program;
    char normalized[PALI_SOURCE_CAP];
    char normalized_again[PALI_SOURCE_CAP];
    CHECK(pali_parse_document(valid_source, &document, &error), error.message);
    CHECK(document.property_count == 3 && document.statement_count == 3,
          "typed document retains properties and Behavior statements");
    CHECK(pali_format_document(&document, normalized, sizeof(normalized),
                               &error),
          error.message);
    CHECK(pali_parse_document(normalized, &reparsed, &error), error.message);
    CHECK(pali_format_document(&reparsed, normalized_again,
                               sizeof(normalized_again), &error),
          error.message);
    CHECK(strcmp(normalized, normalized_again) == 0,
          "normalized PALI formatting is deterministic");
    CHECK(pali_compile_document(&reparsed, &normalized_program, &error),
          error.message);
    CHECK(programs_semantically_equal(&program, &normalized_program),
          "document round-trip preserves executable bytecode");

    static const char *duplicate_source =
        "prototype ration\n"
        "    nutrition = 12\n"
        "    nutrition = 13\n"
        "end\n";
    CHECK(!pali_parse_document(duplicate_source, &reparsed, &error),
          "duplicate semantic properties are rejected");

    char unterminated_source[PALI_SOURCE_CAP];
    memset(unterminated_source, 'x', sizeof(unterminated_source));
    CHECK(!pali_parse_document(unterminated_source, &reparsed, &error),
          "source limit check never scans beyond its fixed buffer");

    char deeply_nested[PALI_SOURCE_CAP];
    int nested_length = snprintf(
        deeply_nested, sizeof(deeply_nested),
        "prototype deep\n    on use(actor)\n        message(");
    CHECK(nested_length > 0, "deep-expression fixture has a valid prefix");
    size_t nested_position = (size_t)nested_length;
    const size_t nesting_count = PALI_MAX_EXPRESSIONS + 8u;
    memset(deeply_nested + nested_position, '(', nesting_count);
    nested_position += nesting_count;
    deeply_nested[nested_position++] = '1';
    memset(deeply_nested + nested_position, ')', nesting_count);
    nested_position += nesting_count;
    (void)snprintf(deeply_nested + nested_position,
                   sizeof(deeply_nested) - nested_position,
                   ")\n    end\nend\n");
    CHECK(!pali_compile(deeply_nested, &program, &error) &&
              strstr(error.message, "nesting") != NULL,
          "recursive syntax is bounded before it can exhaust the C stack");

    PaliDocument malformed = document;
    memset(malformed.prototype_name, 'x',
           sizeof(malformed.prototype_name));
    CHECK(!pali_compile_document(&malformed, &normalized_program, &error),
          "unterminated document names are rejected");
    malformed = document;
    malformed.properties[0].value.type = PALI_VALUE_TEXT;
    memset(malformed.properties[0].value.as.text, 'x',
           sizeof(malformed.properties[0].value.as.text));
    CHECK(!pali_compile_document(&malformed, &normalized_program, &error),
          "unterminated document values are rejected");
    malformed = document;
    const uint16_t orphan_constant = malformed.constant_count++;
    malformed.constants[orphan_constant] = pali_number(99.0);
    PaliExpression *orphan =
        &malformed.expressions[malformed.expression_count++];
    memset(orphan, 0, sizeof(*orphan));
    orphan->kind = (uint8_t)PALI_EXPRESSION_LITERAL;
    orphan->left = PALI_NODE_NONE;
    orphan->right = PALI_NODE_NONE;
    orphan->operand = (uint8_t)orphan_constant;
    CHECK(!pali_compile_document(&malformed, &normalized_program, &error),
          "orphan expression nodes are rejected");

    static const char *precision_source =
        "prototype precision\n"
        "    value = 1\n"
        "end\n";
    PaliDocument precision_document;
    PaliDocument precision_reparsed;
    CHECK(pali_parse_document(precision_source, &precision_document, &error),
          error.message);
    precision_document.properties[0].value =
        pali_number(nextafter(1.0, 2.0));
    bool edge_ok =
        pali_format_document(&precision_document, normalized,
                             sizeof(normalized), &error) &&
        pali_parse_document(normalized, &precision_reparsed, &error);
    CHECK(edge_ok &&
              pali_value_equal(precision_document.properties[0].value,
                               precision_reparsed.properties[0].value),
          "normalization preserves all significant double digits");
    precision_document.properties[0].value = pali_number(1.0e-200);
    edge_ok = pali_format_document(&precision_document, normalized,
                                   sizeof(normalized), &error) &&
              pali_parse_document(normalized, &precision_reparsed, &error);
    CHECK(edge_ok &&
              pali_value_equal(precision_document.properties[0].value,
                               precision_reparsed.properties[0].value),
          "formatter exponent notation reparses exactly");

    PaliDocument oversized;
    memset(&oversized, 0, sizeof(oversized));
    (void)snprintf(oversized.prototype_name,
                   sizeof(oversized.prototype_name), "limit");
    oversized.property_count = PALI_MAX_PROPERTIES;
    oversized.constant_count = PALI_MAX_STATEMENTS;
    oversized.expression_count = PALI_MAX_STATEMENTS;
    oversized.statement_count = PALI_MAX_STATEMENTS;
    oversized.has_use = true;
    char escaped_text[PALI_TEXT_CAP];
    memset(escaped_text, '\\', sizeof(escaped_text) - 1u);
    escaped_text[sizeof(escaped_text) - 1u] = '\0';
    for (uint16_t index = 0; index < PALI_MAX_STATEMENTS; ++index) {
        (void)snprintf(oversized.properties[index].name,
                       sizeof(oversized.properties[index].name), "p%u",
                       (unsigned int)index);
        oversized.properties[index].value = pali_text(escaped_text);
        oversized.constants[index] = pali_text(escaped_text);
        oversized.expressions[index].kind =
            (uint8_t)PALI_EXPRESSION_LITERAL;
        oversized.expressions[index].left = PALI_NODE_NONE;
        oversized.expressions[index].right = PALI_NODE_NONE;
        oversized.expressions[index].operand = (uint8_t)index;
        oversized.statements[index].kind =
            (uint8_t)PALI_STATEMENT_MESSAGE;
        oversized.statements[index].name = PALI_NODE_NONE;
        oversized.statements[index].expression = (uint8_t)index;
    }
    char oversized_source[7000];
    CHECK(!pali_format_document(&oversized, oversized_source,
                                sizeof(oversized_source), &error),
          "normalized source cannot exceed the parseable source contract");
    char one_byte[1];
    CHECK(!pali_format_document(&document, one_byte, sizeof(one_byte),
                                &error) &&
              one_byte[0] == '\0',
          "formatter fails safely for a one-byte destination");

    static const char *invalid_source =
        "prototype ration\n"
        "    nutrition =\n"
        "end\n";
    CHECK(!pali_compile(invalid_source, &program, &error),
          "invalid source is rejected");
    CHECK(error.line == 2 && error.column > 0,
          "compile error reports useful line and column");

    CHECK(pali_compile(valid_source, &program, &error), error.message);
    PaliHost host = {NULL, dummy_get, dummy_set, dummy_call};
    CHECK(!pali_run_use(&program, &host, 1, &error),
          "tiny execution budget stops a valid program");
    CHECK(strstr(error.message, "budget") != NULL,
          "budget error explains the failure");

    PaliProgram malformed_program = program;
    malformed_program.code_count = PALI_MAX_CODE + 1u;
    CHECK(!pali_run_use(&malformed_program, &host, PALI_DEFAULT_BUDGET,
                        &error),
          "VM rejects a public program whose counts exceed fixed storage");

    static const char *bad_call_source =
        "prototype bad_call\n"
        "    on use(actor)\n"
        "        message(123)\n"
        "    end\n"
        "end\n";
    CHECK(pali_compile(bad_call_source, &program, &error), error.message);
    PaliHost rejecting_host = {NULL, dummy_get, dummy_set, rejecting_call};
    CHECK(!pali_run_use(&program, &rejecting_host, PALI_DEFAULT_BUDGET,
                        &error) &&
              error.line == 3,
          "host-call Anomalies retain their source line");
}

static void test_generation(void) {
    PaliError error;
    CHECK(world_init(&world_a, UINT64_C(0x123456789abcdef0),
                     PAL_TEST_ASSET_ROOT, &error),
          error.message);
    CHECK(world_init(&world_b, UINT64_C(0x123456789abcdef0),
                     PAL_TEST_ASSET_ROOT, &error),
          error.message);
    CHECK(world_init(&world_c, UINT64_C(0x123456789abcdef1),
                     PAL_TEST_ASSET_ROOT, &error),
          error.message);
    CHECK(world_genesis_fingerprint(&world_a) ==
              world_genesis_fingerprint(&world_b),
          "identical seeds produce identical genesis");
    CHECK(world_genesis_fingerprint(&world_a) !=
              world_genesis_fingerprint(&world_c),
          "different seeds produce visibly distinct genesis data");
    CHECK(world_a.universe.entity_count >= 20,
          "clearing contains several inspectable objects");
    CHECK(world_concept_access(&world_a, CONCEPT_NUTRITION) ==
              CONCEPT_ACCESS_PATCHABLE &&
              world_concept_access(&world_a, CONCEPT_MASS) ==
                  CONCEPT_ACCESS_VEILED &&
              world_concept_access(&world_a, CONCEPT_HEAT) ==
                  CONCEPT_ACCESS_UNPERCEIVED,
          "normal Knowledge projects patchable, veiled, and hidden concepts");
    CHECK(world_has_reach(&world_a, PATCH_REACH_ENTITY) &&
              !world_has_reach(&world_a, PATCH_REACH_PROTOTYPE),
          "normal Knowledge begins with Entity Reach only");

    const float safe_x = world_c.embodiment.x;
    const float safe_y = world_c.embodiment.y;
    world_step(&world_c, (WorldInput){NAN, INFINITY});
    CHECK(world_c.embodiment.x == safe_x &&
              world_c.embodiment.y == safe_y,
          "non-finite external movement input is ignored safely");

    static const char *extreme_fire =
        "prototype fire\n"
        "    tag = \"warmth\"\n"
        "    mass = 0\n"
        "    heat = 18\n"
        "    color = \"ef8b45\"\n"
        "    alive = true\n"
        "    on use(actor)\n"
        "        self.heat = 1e308\n"
        "    end\n"
        "end\n";
    const int fire_index = first_entity(&world_c, PROTOTYPE_FIRE);
    CHECK(world_apply_prototype_source(&world_c, PROTOTYPE_FIRE,
                                       extreme_fire, &error),
          error.message);
    CHECK(fire_index >= 0 &&
              !world_use_entity(&world_c, fire_index, &error) &&
              world_c.universe.entities[fire_index].state_count == 0 &&
              error.line == 8,
          "Behavior cannot inject out-of-domain physics through self state");
}

static void test_knowledge_revelation(void) {
    PaliError error;
    CHECK(world_init(&knowledge_world, UINT64_C(0x0b5e77ed5ca1e),
                     PAL_TEST_ASSET_ROOT, &error),
          error.message);
    CHECK(world_concept_access(&knowledge_world, CONCEPT_HEAT) ==
                  CONCEPT_ACCESS_UNPERCEIVED &&
              world_concept_access(&knowledge_world, CONCEPT_MASS) ==
                  CONCEPT_ACCESS_VEILED &&
              world_concept_access(&knowledge_world, CONCEPT_COLOR) ==
                  CONCEPT_ACCESS_READABLE &&
              world_concept_access(&knowledge_world, CONCEPT_NUTRITION) ==
                  CONCEPT_ACCESS_PATCHABLE,
          "Genesis Knowledge proves all four Concept Access states");
    CHECK(world_knows_exact_notation(&knowledge_world,
                                     CONCEPT_NUTRITION) &&
              !world_knows_exact_notation(&knowledge_world, CONCEPT_MASS),
          "exact notation is concept-specific rather than numeric-global");

    const int apple_index = first_entity(&knowledge_world, PROTOTYPE_APPLE);
    const int second_apple_index =
        next_entity(&knowledge_world, PROTOTYPE_APPLE, apple_index);
    const int stone_index = first_entity(&knowledge_world, PROTOTYPE_STONE);
    const int tree_index = first_entity(&knowledge_world, PROTOTYPE_TREE);
    CHECK(apple_index >= 0 && second_apple_index >= 0 && stone_index >= 0 &&
              tree_index >= 0,
          "comparison proof has three distinct material kinds");
    if (apple_index < 0 || second_apple_index < 0 || stone_index < 0 ||
        tree_index < 0) {
        return;
    }

    universe_snapshot = knowledge_world.universe;
    const EmbodimentState embodiment_snapshot = knowledge_world.embodiment;
    char message_snapshot[WORLD_MESSAGE_CAP];
    (void)snprintf(message_snapshot, sizeof(message_snapshot), "%s",
                   knowledge_world.message);

    const Entity *apple = &knowledge_world.universe.entities[apple_index];
    const Entity *second_apple =
        &knowledge_world.universe.entities[second_apple_index];
    const Entity *stone = &knowledge_world.universe.entities[stone_index];
    const Entity *tree = &knowledge_world.universe.entities[tree_index];
    CHECK(world_observe_entity_concept(&knowledge_world, apple->id,
                                       CONCEPT_MASS) ==
                  OBSERVATION_RECORDED &&
              world_concept_observation_count(&knowledge_world,
                                              CONCEPT_MASS) == 1 &&
              world_concept_access(&knowledge_world, CONCEPT_MASS) ==
                  CONCEPT_ACCESS_VEILED,
          "one kind leaves mass Veiled while retaining an Observation");

    CHECK(platform_ensure_directory(PAL_TEST_TMP_ROOT, &error),
          error.message);
    char knowledge_save[PLATFORM_PATH_CAP];
    (void)snprintf(knowledge_save, sizeof(knowledge_save),
                   "%s/knowledge-v3.pal", PAL_TEST_TMP_ROOT);
    CHECK(save_write_atomic(&knowledge_world, knowledge_save, &error) &&
              save_load(&loaded_world, knowledge_save, PAL_TEST_ASSET_ROOT,
                        &error) &&
              world_concept_observation_count(&loaded_world,
                                              CONCEPT_MASS) == 1 &&
              !world_knows_exact_notation(&loaded_world, CONCEPT_MASS),
          "save v5 restores partial Observation progress exactly");
    CHECK(downgrade_save_to_version(knowledge_save, 3u) &&
              save_validate_file(knowledge_save, &error) &&
              save_load(&loaded_world, knowledge_save, PAL_TEST_ASSET_ROOT,
                         &error) &&
              world_concept_observation_count(&loaded_world,
                                              CONCEPT_MASS) == 1 &&
              loaded_world.universe.entity_count ==
                  knowledge_world.universe.entity_count &&
              active_lineage_count(&loaded_world) == 0u,
          "save v3 migrates with its Observation ledger and no Behavior Scar");
    const uint8_t no_old_notations[4] = {0u, 0u, 0u, 0u};
    CHECK(save_write_atomic(&knowledge_world, knowledge_save, &error) &&
              rewrite_save_bytes(knowledge_save, 64u, no_old_notations,
                                 sizeof(no_old_notations)) &&
              downgrade_save_to_version(knowledge_save, 2u) &&
              save_validate_file(knowledge_save, &error) &&
              save_load(&loaded_world, knowledge_save, PAL_TEST_ASSET_ROOT,
                        &error) &&
              world_concept_observation_count(&loaded_world,
                                              CONCEPT_MASS) == 0 &&
              world_knows_exact_notation(&loaded_world,
                                         CONCEPT_NUTRITION) &&
              !world_knows_exact_notation(&loaded_world, CONCEPT_MASS) &&
              loaded_world.universe.entity_count ==
                  knowledge_world.universe.entity_count &&
              active_lineage_count(&loaded_world) == 0u,
          "save v2 migrates with no invented Observations and exact nutrition");
    CHECK(world_observe_entity_concept(&knowledge_world, second_apple->id,
                                       CONCEPT_MASS) ==
                  OBSERVATION_REPEATED &&
              world_concept_observation_count(&knowledge_world,
                                              CONCEPT_MASS) == 1,
          "another Entity of the same Prototype cannot advance Knowledge");
    CHECK(world_observe_entity_concept(&knowledge_world, apple->id,
                                       CONCEPT_HEAT) ==
                  OBSERVATION_REJECTED &&
              world_concept_observation_count(&knowledge_world,
                                              CONCEPT_HEAT) == 0,
          "an Unperceived concept cannot be observed or leaked");
    CHECK(world_observe_entity_concept(&knowledge_world, stone->id,
                                       CONCEPT_MASS) ==
                  OBSERVATION_REVELATION &&
              world_concept_observation_count(&knowledge_world,
                                              CONCEPT_MASS) == 2 &&
              world_concept_access(&knowledge_world, CONCEPT_MASS) ==
                  CONCEPT_ACCESS_READABLE &&
              !world_knows_exact_notation(&knowledge_world, CONCEPT_MASS),
          "a second kind reveals qualitative mass but not its notation");
    (void)snprintf(knowledge_save, sizeof(knowledge_save),
                   "%s/knowledge-readable.pal", PAL_TEST_TMP_ROOT);
    CHECK(save_write_atomic(&knowledge_world, knowledge_save, &error) &&
              save_load(&loaded_world, knowledge_save, PAL_TEST_ASSET_ROOT,
                        &error) &&
              world_concept_observation_count(&loaded_world,
                                              CONCEPT_MASS) == 2 &&
              world_concept_access(&loaded_world, CONCEPT_MASS) ==
                  CONCEPT_ACCESS_READABLE &&
              !world_knows_exact_notation(&loaded_world, CONCEPT_MASS),
          "save v4 preserves the qualitative Revelation boundary");
    CHECK(world_observe_entity_concept(&knowledge_world, tree->id,
                                       CONCEPT_MASS) ==
                  OBSERVATION_NOTATION &&
              world_concept_observation_count(&knowledge_world,
                                              CONCEPT_MASS) == 3 &&
              world_knows_exact_notation(&knowledge_world, CONCEPT_MASS) &&
              world_concept_access(&knowledge_world, CONCEPT_MASS) ==
                  CONCEPT_ACCESS_READABLE,
          "a third kind grants exact notation without granting Patch access");
    (void)snprintf(knowledge_save, sizeof(knowledge_save),
                   "%s/knowledge-exact.pal", PAL_TEST_TMP_ROOT);
    CHECK(save_write_atomic(&knowledge_world, knowledge_save, &error) &&
              save_load(&loaded_world, knowledge_save, PAL_TEST_ASSET_ROOT,
                        &error) &&
              world_concept_observation_count(&loaded_world,
                                              CONCEPT_MASS) == 3 &&
              world_knows_exact_notation(&loaded_world, CONCEPT_MASS),
          "save v4 preserves exact notation independently of Concept Access");
    FILE *exact_save = fopen(knowledge_save, "rb");
    long exact_save_length = -1;
    if (exact_save != NULL && fseek(exact_save, 0, SEEK_END) == 0) {
        exact_save_length = ftell(exact_save);
    }
    if (exact_save != NULL) {
        (void)fclose(exact_save);
    }
    const size_t observation_tail =
        (size_t)CONCEPT_COUNT * sizeof(uint32_t);
    CHECK(exact_save_length >= 0 &&
              (size_t)exact_save_length >= 24u + observation_tail,
          "save v4 retains the bounded Observation tail");
    if (exact_save_length >= 0 &&
        (size_t)exact_save_length >= 24u + observation_tail) {
        const size_t mass_mask_offset =
            (size_t)exact_save_length - observation_tail +
            (size_t)CONCEPT_MASS * sizeof(uint32_t);
        const uint8_t invalid_prototype_mask[4] = {0u, 0u, 0u, 0x80u};
        const uint64_t before_invalid_load =
            world_state_fingerprint(&loaded_world);
        CHECK(rewrite_save_bytes(knowledge_save, mass_mask_offset,
                                 invalid_prototype_mask,
                                 sizeof(invalid_prototype_mask)) &&
                  !save_load(&loaded_world, knowledge_save,
                             PAL_TEST_ASSET_ROOT, &error) &&
                  world_state_fingerprint(&loaded_world) ==
                      before_invalid_load,
              "invalid Observation masks are rejected transactionally");
    }
    CHECK(world_observe_entity_concept(&knowledge_world, tree->id,
                                       CONCEPT_MASS) ==
                  OBSERVATION_REPEATED &&
              world_concept_observation_count(&knowledge_world,
                                              CONCEPT_MASS) == 3,
          "Observation is monotonic and idempotent");

    CHECK(memcmp(&knowledge_world.universe, &universe_snapshot,
                 sizeof(universe_snapshot)) == 0 &&
              memcmp(&knowledge_world.embodiment, &embodiment_snapshot,
                     sizeof(embodiment_snapshot)) == 0 &&
              strcmp(knowledge_world.message, message_snapshot) == 0,
          "learning changes only Knowledge, never Universe or Embodiment");

    world_grant_developer_knowledge(&knowledge_world);
    CHECK(world_knows_exact_notation(&knowledge_world, CONCEPT_MASS) &&
              world_knows_exact_notation(&knowledge_world, CONCEPT_COLOR),
          "developer Knowledge includes every exact notation");

    CHECK(world_init(&knowledge_world, UINT64_C(0x0b5e77ed5ca1e),
                     PAL_TEST_ASSET_ROOT, &error),
          error.message);
    const int inquiry_apple =
        first_entity(&knowledge_world, PROTOTYPE_APPLE);
    const int inquiry_stone =
        first_entity(&knowledge_world, PROTOTYPE_STONE);
    CHECK(inquiry_apple >= 0 && inquiry_stone >= 0,
          "Inquiry composition proof has an apple and stone");
    if (inquiry_apple >= 0 && inquiry_stone >= 0) {
        const uint64_t apple_id =
            knowledge_world.universe.entities[inquiry_apple].id;
        const uint64_t stone_id =
            knowledge_world.universe.entities[inquiry_stone].id;
        CHECK(world_apply_entity_value_patch(
                  &knowledge_world, apple_id, CONCEPT_NUTRITION,
                  pali_number(19.0), &error) &&
                  world_use_entity(&knowledge_world, inquiry_apple, &error) &&
                  world_observe_entity_concept(
                      &knowledge_world, stone_id, CONCEPT_MASS) ==
                      OBSERVATION_RECORDED,
              "a completed first Scar composes with the next Observation");
        (void)snprintf(knowledge_save, sizeof(knowledge_save),
                       "%s/milestone-0.3-open.pal", PAL_TEST_TMP_ROOT);
        CHECK(save_write_atomic(&knowledge_world, knowledge_save, &error),
              "the composed Inquiry state is persistable");
    }
}

static void test_patch_gameplay_and_save(void) {
    static const char *apple_patch =
        "prototype apple\n"
        "    tag = \"food\"\n"
        "    mass = 140\n"
        "    nutrition = 37\n"
        "    color = \"74d4ff\"\n"
        "    ripe = true\n"
        "    on use(actor)\n"
        "        actor.hunger = max(0, actor.hunger - self.nutrition)\n"
        "        message(\"Patched fruit consumed.\")\n"
        "        destroy(self)\n"
        "    end\n"
        "end\n";
    static const char *bad_patch =
        "prototype apple\n"
        "    nutrition =\n"
        "end\n";
    static const char *conflicting_patch =
        "prototype apple\n"
        "    tag = \"food\"\n"
        "    mass = 140\n"
        "    color = \"c94f45\"\n"
        "    ripe = true\n"
        "    on use(actor)\n"
        "        message(\"This should never become real.\")\n"
        "    end\n"
        "end\n";
    PaliError error;
    const int apple_index = first_entity(&world_a, PROTOTYPE_APPLE);
    const int second_apple_index =
        next_entity(&world_a, PROTOTYPE_APPLE, apple_index);
    CHECK(apple_index >= 0 && second_apple_index >= 0,
          "generated world contains two apples");
    Entity *apple = &world_a.universe.entities[apple_index];
    Entity *second_apple = &world_a.universe.entities[second_apple_index];

    const uint64_t before_denied_patch = world_state_fingerprint(&world_a);
    CHECK(!world_apply_prototype_value_patch(
              &world_a, PROTOTYPE_APPLE, CONCEPT_NUTRITION,
              pali_number(37.0), &error),
          "normal Knowledge cannot Patch every apple");
    CHECK(world_state_fingerprint(&world_a) == before_denied_patch,
          "denied Prototype Reach leaves the Universe unchanged");

    CHECK(world_apply_entity_value_patch(
              &world_a, apple->id, CONCEPT_NUTRITION, pali_number(5.0),
              &error),
          error.message);
    PaliValue first_nutrition;
    PaliValue second_nutrition;
    CHECK(world_get_entity_concept(&world_a, apple, CONCEPT_NUTRITION,
                                   &first_nutrition) &&
              first_nutrition.type == PALI_VALUE_NUMBER &&
              first_nutrition.as.number == 5.0,
          "Entity Patch changes one apple's nourishment");
    CHECK(world_get_entity_concept(&world_a, second_apple,
                                   CONCEPT_NUTRITION,
                                   &second_nutrition) &&
              second_nutrition.as.number == 20.0,
          "another apple retains Prototype nourishment");

    CHECK(world_apply_entity_value_patch(
              &world_a, second_apple->id, CONCEPT_NUTRITION,
              pali_number(9.0), &error) &&
              world_clear_entity_value_patch(
                  &world_a, second_apple->id, CONCEPT_NUTRITION, &error) &&
              world_get_entity_concept(&world_a, second_apple,
                                       CONCEPT_NUTRITION,
                                       &second_nutrition) &&
              second_nutrition.as.number == 20.0,
          "discarding an Entity Patch reveals the inherited value again");

    const uint64_t before_bad_value = world_state_fingerprint(&world_a);
    CHECK(!world_apply_entity_value_patch(
              &world_a, apple->id, CONCEPT_NUTRITION,
              pali_text("bottomless"), &error),
          "wrong-type Entity Patch is rejected");
    CHECK(world_state_fingerprint(&world_a) == before_bad_value,
          "rejected Entity Patch is transactional");
    CHECK(!world_apply_entity_value_patch(
              &world_a, apple->id, CONCEPT_NUTRITION,
              pali_number(101.0), &error) &&
              world_state_fingerprint(&world_a) == before_bad_value,
          "out-of-range Entity Patch is rejected transactionally");

    world_grant_developer_knowledge(&world_a);
    const uint64_t before_conflict = world_state_fingerprint(&world_a);
    CHECK(!world_apply_prototype_source(&world_a, PROTOTYPE_APPLE,
                                        conflicting_patch, &error) &&
              world_state_fingerprint(&world_a) == before_conflict,
          "Prototype cannot remove a node carrying a narrower Entity Patch");
    CHECK(world_apply_prototype_value_patch(
              &world_a, PROTOTYPE_APPLE, CONCEPT_NUTRITION,
              pali_number(37.0), &error),
          error.message);
    CHECK(world_apply_prototype_source(&world_a, PROTOTYPE_APPLE, apple_patch,
                                       &error),
          error.message);
    CHECK(strstr(world_prototype_source(&world_a, PROTOTYPE_APPLE),
                 "nutrition = 37") != NULL,
          "valid prototype patch becomes active source");
    CHECK(!world_apply_prototype_source(&world_a, PROTOTYPE_APPLE, bad_patch,
                                        &error),
          "invalid prototype patch is rejected");
    CHECK(error.line == 2 &&
              strstr(world_prototype_source(&world_a, PROTOTYPE_APPLE),
                     "nutrition = 37") != NULL,
          "invalid patch preserves last valid program and source");

    CHECK(world_get_entity_concept(&world_a, apple, CONCEPT_NUTRITION,
                                   &first_nutrition) &&
              first_nutrition.as.number == 5.0 &&
              world_get_entity_concept(&world_a, second_apple,
                                       CONCEPT_NUTRITION,
                                       &second_nutrition) &&
              second_nutrition.as.number == 37.0,
          "narrow nourishment Scar survives broader Prototype change");
    PaliValue inherited_color;
    CHECK(world_get_entity_concept(&world_a, apple, CONCEPT_COLOR,
                                   &inherited_color) &&
              strcmp(inherited_color.as.text, "74d4ff") == 0,
          "locally Patched apple inherits unrelated Prototype properties");

    world_a.embodiment.hunger = 60.0f;
    CHECK(world_use_entity(&world_a, apple_index, &error), error.message);
    CHECK(fabsf(world_a.embodiment.hunger - 55.0f) < 0.001f,
          "existing VM reads sparse Entity nourishment Patch");
    CHECK(strcmp(world_a.message, "Patched fruit consumed.") == 0,
          "locally Patched apple inherits new Prototype Behavior");
    CHECK(!world_a.universe.entities[apple_index].active,
          "edited apple handler destroys the used entity");

    WorldInput input = {1.0f, 0.5f};
    for (int tick = 0; tick < 180; ++tick) {
        world_step(&world_a, input);
    }

    CHECK(platform_ensure_directory(PAL_TEST_TMP_ROOT, &error), error.message);
    char save_path[PLATFORM_PATH_CAP];
    (void)snprintf(save_path, sizeof(save_path), "%s/save-roundtrip.pal",
                   PAL_TEST_TMP_ROOT);
    const uint16_t valid_entity_count = world_c.universe.entity_count;
    world_c.universe.entity_count = WORLD_MAX_ENTITIES + 1u;
    CHECK(!save_write_atomic(&world_c, save_path, &error),
          "serializer rejects public World counts beyond fixed storage");
    world_c.universe.entity_count = valid_entity_count;
    char valid_message[WORLD_MESSAGE_CAP];
    (void)snprintf(valid_message, sizeof(valid_message), "%s",
                   world_c.message);
    memset(world_c.message, 'x', sizeof(world_c.message));
    CHECK(!save_write_atomic(&world_c, save_path, &error),
          "serializer rejects unterminated public World strings");
    (void)snprintf(world_c.message, sizeof(world_c.message), "%s",
                   valid_message);
    const uint64_t before = world_state_fingerprint(&world_a);
    CHECK(save_write_atomic(&world_a, save_path, &error), error.message);
    CHECK(save_validate_file(save_path, &error), error.message);
    CHECK(save_load(&loaded_world, save_path, PAL_TEST_ASSET_ROOT, &error),
          error.message);
    CHECK(world_state_fingerprint(&loaded_world) == before,
          "save round-trip restores exact playable state and patches");
    const Entity *loaded_first =
        world_entity_by_id_const(&loaded_world, apple->id);
    const Entity *loaded_second =
        world_entity_by_id_const(&loaded_world, second_apple->id);
    CHECK(world_get_entity_concept(&loaded_world, loaded_first,
                                   CONCEPT_NUTRITION, &first_nutrition) &&
              first_nutrition.as.number == 5.0 &&
              world_get_entity_concept(&loaded_world, loaded_second,
                                       CONCEPT_NUTRITION,
                                       &second_nutrition) &&
              second_nutrition.as.number == 37.0,
          "save restores sparse and broader Patch resolution");

    FILE *file = fopen(save_path, "rb");
    CHECK(file != NULL, "round-trip save can be measured");
    if (file != NULL) {
        (void)fseek(file, 0, SEEK_END);
        const long size = ftell(file);
        (void)fclose(file);
        CHECK(size > 0 && size < 16384,
              "recipe-and-scars save remains sparse and compact");
    }

    CHECK(!save_validate_file(NULL, &error),
          "save validation rejects a null path safely");
    const uint64_t before_semantic_rejection =
        world_state_fingerprint(&world_c);
    const uint8_t quiet_nan[4] = {0x00u, 0x00u, 0xc0u, 0x7fu};
    CHECK(rewrite_save_bytes(save_path, 81u, quiet_nan,
                             sizeof(quiet_nan)) &&
              save_validate_file(save_path, &error) &&
              !save_load(&world_c, save_path, PAL_TEST_ASSET_ROOT, &error) &&
              world_state_fingerprint(&world_c) ==
                  before_semantic_rejection,
          "checksummed non-finite embodiment state is rejected transactionally");

    CHECK(save_write_atomic(&world_a, save_path, &error), error.message);
    const uint8_t impossible_source_length[2] = {0xffu, 0xffu};
    CHECK(rewrite_save_bytes(save_path, 99u, impossible_source_length,
                             sizeof(impossible_source_length)) &&
              save_validate_file(save_path, &error) &&
              !save_load(&world_c, save_path, PAL_TEST_ASSET_ROOT, &error) &&
              world_state_fingerprint(&world_c) ==
                  before_semantic_rejection,
          "malformed Prototype source records never reach the parser");

    const int hole_first_index = first_entity(&world_b, PROTOTYPE_APPLE);
    const int hole_second_index =
        next_entity(&world_b, PROTOTYPE_APPLE, hole_first_index);
    Entity *hole_first = &world_b.universe.entities[hole_first_index];
    Entity *hole_second = &world_b.universe.entities[hole_second_index];
    CHECK(world_apply_entity_value_patch(
              &world_b, hole_first->id, CONCEPT_NUTRITION,
              pali_number(7.0), &error) &&
              world_apply_entity_value_patch(
                  &world_b, hole_second->id, CONCEPT_NUTRITION,
                  pali_number(8.0), &error) &&
              world_clear_entity_value_patch(
                  &world_b, hole_first->id, CONCEPT_NUTRITION, &error),
          "sparse allocator can contain a harmless empty slot");
    (void)snprintf(save_path, sizeof(save_path),
                   "%s/save-sparse-hole.pal", PAL_TEST_TMP_ROOT);
    const uint64_t before_hole_save = world_state_fingerprint(&world_b);
    CHECK(save_write_atomic(&world_b, save_path, &error) &&
              save_load(&loaded_world, save_path, PAL_TEST_ASSET_ROOT,
                        &error) &&
              world_state_fingerprint(&loaded_world) == before_hole_save,
          "save identity follows semantic Scars, not allocator slot numbers");

    (void)snprintf(save_path, sizeof(save_path),
                   "%s/milestone-0.3-complete.pal", PAL_TEST_TMP_ROOT);
    CHECK(save_write_atomic(&world_a, save_path, &error),
          "completed first Scar and Knowledge remain capturable together");
}

static void test_inquiry_and_behavior_grammar(void) {
    static const char *over_budget_handler =
        "prototype apple\n"
        "    on use(actor)\n"
        "        message(\"one\")\n"
        "        message(\"two\")\n"
        "        message(\"three\")\n"
        "        message(\"four\")\n"
        "        message(\"five\")\n"
        "        message(\"six\")\n"
        "        message(\"seven\")\n"
        "        message(\"eight\")\n"
        "        message(\"nine\")\n"
        "        message(\"ten\")\n"
        "        message(\"eleven\")\n"
        "        message(\"twelve\")\n"
        "        message(\"thirteen\")\n"
        "    end\n"
        "end\n";
    static const char *wrong_type_handler =
        "prototype apple\n"
        "    on use(actor)\n"
        "        message(1)\n"
        "    end\n"
        "end\n";
    static const char *foreign_handler =
        "prototype apple\n"
        "    on use(actor)\n"
        "        message(\"foreign sentence\")\n"
        "    end\n"
        "end\n";
    PaliError error;
    CHECK(world_init(&behavior_world, UINT64_C(0x0b4a7105),
                     PAL_TEST_ASSET_ROOT, &error),
          error.message);
    const InquiryProgress genesis =
        world_inquiry_progress(&behavior_world, INQUIRY_FIRST_SCAR);
    CHECK(world_active_inquiry(&behavior_world) == INQUIRY_FIRST_SCAR &&
              genesis.completed_steps == 0 && genesis.step_count == 2 &&
              behavior_world.knowledge.access_depth == ACCESS_DEPTH_STATE,
          "Genesis begins with one derived Inquiry at State depth");

    const int first_apple = first_entity(&behavior_world, PROTOTYPE_APPLE);
    const int second_apple =
        next_entity(&behavior_world, PROTOTYPE_APPLE, first_apple);
    const int third_apple =
        next_entity(&behavior_world, PROTOTYPE_APPLE, second_apple);
    CHECK(first_apple >= 0 && second_apple >= 0 && third_apple >= 0,
          "Behavior proof has three distinct apples");
    if (first_apple < 0 || second_apple < 0 || third_apple < 0) {
        return;
    }
    const uint64_t first_id =
        behavior_world.universe.entities[first_apple].id;
    CHECK(world_apply_entity_value_patch(
              &behavior_world, first_id, CONCEPT_NUTRITION,
              pali_number(19.0), &error) &&
              world_inquiry_progress(&behavior_world,
                                     INQUIRY_FIRST_SCAR)
                      .completed_steps == 1 &&
              world_use_entity(&behavior_world, first_apple, &error),
          "the First Scar proof is derived from Patch plus invocation");
    CHECK(world_reconcile_inquiry_knowledge(&behavior_world) ==
                  KNOWLEDGE_GRANT_BEHAVIOR_DEPTH &&
              behavior_world.knowledge.access_depth ==
                  ACCESS_DEPTH_BEHAVIOR &&
              world_concept_access(&behavior_world, CONCEPT_HUNGER) ==
                  CONCEPT_ACCESS_READABLE &&
              world_active_inquiry(&behavior_world) ==
                  INQUIRY_WEIGHT_OF_THINGS &&
              world_reconcile_inquiry_knowledge(&behavior_world) ==
                  KNOWLEDGE_GRANT_NONE,
          "First Scar grants Behavior Knowledge once and yields to Weight");

    const int stone = first_entity(&behavior_world, PROTOTYPE_STONE);
    const int tree = first_entity(&behavior_world, PROTOTYPE_TREE);
    const int fire = first_entity(&behavior_world, PROTOTYPE_FIRE);
    CHECK(stone >= 0 && tree >= 0 && fire >= 0,
          "Behavior grammar proof has three material kinds");
    if (stone < 0 || tree < 0 || fire < 0) {
        return;
    }
    CHECK(world_observe_entity_concept(
              &behavior_world, behavior_world.universe.entities[stone].id,
              CONCEPT_MASS) == OBSERVATION_RECORDED &&
              world_observe_entity_concept(
                  &behavior_world,
                  behavior_world.universe.entities[tree].id,
                  CONCEPT_MASS) == OBSERVATION_REVELATION &&
              world_observe_entity_concept(
                  &behavior_world,
                  behavior_world.universe.entities[fire].id,
                  CONCEPT_MASS) == OBSERVATION_NOTATION &&
              world_active_inquiry(&behavior_world) ==
                  INQUIRY_SENTENCE_INSIDE,
          "exact mass Notation clears Weight and opens the Behavior Inquiry");

    CHECK(platform_ensure_directory(PAL_TEST_TMP_ROOT, &error), error.message);
    char capture_save[PLATFORM_PATH_CAP];
    (void)snprintf(capture_save, sizeof(capture_save),
                   "%s/milestone-0.4-open.pal", PAL_TEST_TMP_ROOT);
    CHECK(save_write_atomic(&behavior_world, capture_save, &error),
          "the open Behavior Inquiry is capturable");

    Entity *scarred = &behavior_world.universe.entities[second_apple];
    Entity *inherited = &behavior_world.universe.entities[third_apple];
    UseBehaviorDraft default_draft;
    CHECK(world_get_entity_use_behavior_draft(&behavior_world, scarred,
                                              &default_draft) &&
              default_draft.hunger == BEHAVIOR_HUNGER_SOOTHE &&
              default_draft.voice == BEHAVIOR_VOICE_FADE &&
              default_draft.fate == BEHAVIOR_FATE_CEASE &&
              world_behavior_is_patchable(&behavior_world, scarred),
          "the existing apple sentence projects into typed Clause choices");

    PaliDocument rejected;
    CHECK(pali_parse_document(over_budget_handler, &rejected, &error),
          error.message);
    const uint64_t before_budget_rejection =
        world_state_fingerprint(&behavior_world);
    CHECK(!world_apply_entity_behavior_patch(
              &behavior_world, scarred->id, &rejected, &error) &&
              strstr(error.message, "budget") != NULL &&
              world_state_fingerprint(&behavior_world) ==
                  before_budget_rejection,
          "over-budget Behavior is rejected before it can become a Patch");
    CHECK(pali_parse_document(wrong_type_handler, &rejected, &error),
          error.message);
    CHECK(!world_apply_entity_behavior_patch(
              &behavior_world, scarred->id, &rejected, &error) &&
              world_state_fingerprint(&behavior_world) ==
                  before_budget_rejection,
          "wrong-type Clause sockets reject transactionally");
    CHECK(pali_parse_document(foreign_handler, &rejected, &error),
          error.message);
    CHECK(!world_apply_entity_behavior_patch(
              &behavior_world, scarred->id, &rejected, &error) &&
              strstr(error.message, "apple grammar") != NULL &&
              world_state_fingerprint(&behavior_world) ==
                  before_budget_rejection,
          "non-generated apple Behavior rejects transactionally");

    const UseBehaviorDraft changed = {
        .hunger = BEHAVIOR_HUNGER_SHARPEN,
        .voice = BEHAVIOR_VOICE_REMEMBER,
        .fate = BEHAVIOR_FATE_REMAIN};
    PaliDocument changed_handler;
    CHECK(world_build_use_behavior_document(
              &behavior_world, scarred, changed, &changed_handler, &error) &&
              world_apply_entity_behavior_patch(
                  &behavior_world, scarred->id, &changed_handler, &error) &&
              world_apply_entity_value_patch(
                  &behavior_world, scarred->id, CONCEPT_NUTRITION,
                  pali_number(7.0), &error) &&
              world_entity_has_behavior_patch(&behavior_world, scarred) &&
              !world_entity_has_behavior_patch(&behavior_world, inherited) &&
              world_active_inquiry(&behavior_world) ==
                  INQUIRY_FRUIT_REMEMBERS,
          "mouse grammar model gives one apple a sparse local Behavior Scar");

    behavior_world.embodiment.hunger = 60.0f;
    CHECK(world_use_entity(&behavior_world, second_apple, &error) &&
              fabsf(behavior_world.embodiment.hunger - 67.0f) < 0.001f &&
              scarred->active &&
              strcmp(behavior_world.message,
                     "The apple remembers being eaten.") == 0,
          "local Behavior executes changed effect, voice, and fate");
    CHECK(world_use_entity(&behavior_world, third_apple, &error) &&
              fabsf(behavior_world.embodiment.hunger - 47.0f) < 0.001f &&
              !inherited->active,
          "another apple still executes inherited Prototype Behavior");

    char save_path[PLATFORM_PATH_CAP];
    (void)snprintf(save_path, sizeof(save_path),
                   "%s/milestone-0.4-complete.pal", PAL_TEST_TMP_ROOT);
    const uint64_t before_save = world_state_fingerprint(&behavior_world);
    CHECK(save_write_atomic(&behavior_world, save_path, &error) &&
              save_load(&loaded_world, save_path, PAL_TEST_ASSET_ROOT,
                         &error) &&
              world_state_fingerprint(&loaded_world) == before_save,
          "save v5 restores the complete sparse Behavior Patch");
    CHECK(downgrade_save_to_version(save_path, 4u) &&
              save_validate_file(save_path, &error) &&
              save_load(&loaded_world, save_path, PAL_TEST_ASSET_ROOT,
                        &error) &&
              world_state_fingerprint(&loaded_world) == before_save &&
              active_lineage_count(&loaded_world) == 0u,
          "genuine save v4 restores its sparse Behavior Patch");
    char normalized_behavior_source[PALI_SOURCE_CAP];
    size_t handler_offset = 0;
    CHECK(pali_format_document(&changed_handler,
                               normalized_behavior_source,
                               sizeof(normalized_behavior_source), &error) &&
              find_save_bytes(save_path, normalized_behavior_source,
                              &handler_offset),
          "save v4 regenerates the normalized Behavior source from its Draft");
    static const uint8_t wrong_target_name[5] = {'s', 't', 'o', 'n', 'e'};
    const uint64_t before_wrong_target_load =
        world_state_fingerprint(&loaded_world);
    CHECK(handler_offset >= 24u &&
              rewrite_save_bytes(
                  save_path, handler_offset + strlen("prototype "),
                  wrong_target_name, sizeof(wrong_target_name)) &&
              save_validate_file(save_path, &error) &&
              !save_load(&loaded_world, save_path, PAL_TEST_ASSET_ROOT,
                         &error) &&
              world_state_fingerprint(&loaded_world) ==
                  before_wrong_target_load,
          "checksummed wrong-target Behavior source rejects load transactionally");
    CHECK(save_write_atomic(&behavior_world, save_path, &error),
          "valid compact Behavior Draft rewrites the v5 source record");
    const int scarred_slot = scarred->local_override;
    CHECK(scarred_slot >= 0 && scarred_slot < WORLD_MAX_LOCAL_OVERRIDES,
          "Behavior Scar retains a local Patch slot");
    if (scarred_slot >= 0 && scarred_slot < WORLD_MAX_LOCAL_OVERRIDES) {
        LocalOverride *override =
            &behavior_world.universe.local_overrides[scarred_slot];
        const UseBehaviorDraft retained_draft = override->behavior;
        override->behavior.hunger =
            (BehaviorHungerClause)BEHAVIOR_HUNGER_COUNT;
        char semantic_rejection_save[PLATFORM_PATH_CAP];
        (void)snprintf(semantic_rejection_save,
                       sizeof(semantic_rejection_save),
                       "%s/milestone-0.4-invalid-behavior.pal",
                       PAL_TEST_TMP_ROOT);
        CHECK(!save_write_atomic(&behavior_world, semantic_rejection_save,
                                 &error),
              "serializer rejects an invalid compact Behavior Draft");
        override->behavior = retained_draft;
    }
    Entity *loaded_scarred =
        world_entity_by_id(&loaded_world, scarred->id);
    loaded_world.embodiment.hunger = 30.0f;
    CHECK(loaded_scarred != NULL &&
              world_entity_has_behavior_patch(&loaded_world,
                                               loaded_scarred) &&
              world_use_entity(
                  &loaded_world,
                  (int)(loaded_scarred - loaded_world.universe.entities),
                  &error) &&
              fabsf(loaded_world.embodiment.hunger - 37.0f) < 0.001f &&
              loaded_scarred->active,
          "restored local Behavior composes with its sparse value Scar");

    if (scarred_slot >= 0 && scarred_slot < WORLD_MAX_LOCAL_OVERRIDES) {
        CHECK(world_clear_entity_value_patch(&behavior_world, scarred->id,
                                             CONCEPT_NUTRITION, &error),
              error.message);
        char empty_record_save[PLATFORM_PATH_CAP];
        (void)snprintf(empty_record_save, sizeof(empty_record_save),
                       "%s/milestone-0.4-empty-local.pal",
                       PAL_TEST_TMP_ROOT);
        LocalOverride *empty_override =
            &behavior_world.universe.local_overrides[scarred_slot];
        empty_override->has_behavior = false;
        CHECK(!save_write_atomic(&behavior_world, empty_record_save,
                                 &error),
              "serializer rejects an active but empty Entity Patch binding");
        empty_override->has_behavior = true;
        size_t empty_handler_offset = 0;
        const uint8_t no_behavior = 0u;
        const uint64_t before_empty_load =
            world_state_fingerprint(&loaded_world);
        PaliDocument retained_handler;
        char retained_source[PALI_SOURCE_CAP];
        CHECK(world_build_use_behavior_document(
                  &behavior_world, scarred, empty_override->behavior,
                  &retained_handler, &error) &&
                  pali_format_document(&retained_handler, retained_source,
                                       sizeof(retained_source), &error),
              error.message);
        const size_t behavior_source_length = strlen(retained_source);
        CHECK(save_write_atomic(&behavior_world, empty_record_save, &error) &&
                  find_save_bytes(empty_record_save,
                                  "prototype apple\n    on use(actor)",
                                  &empty_handler_offset) &&
                  empty_handler_offset >= 3u &&
                  rewrite_save_bytes(empty_record_save,
                                     empty_handler_offset - 3u,
                                     &no_behavior, 1u) &&
                  remove_save_bytes(
                      empty_record_save, empty_handler_offset - 2u,
                      sizeof(uint16_t) + behavior_source_length) &&
                  save_validate_file(empty_record_save, &error) &&
                  !save_load(&loaded_world, empty_record_save,
                             PAL_TEST_ASSET_ROOT, &error) &&
                  strstr(error.message, "empty") != NULL &&
                  world_state_fingerprint(&loaded_world) ==
                      before_empty_load,
              "empty v4 Entity Patch records reject transactionally");
    }

    const char *handler_prefix = "prototype apple\n    on use(actor)";
    const uint8_t malformed_token = (uint8_t)'?';
    const uint64_t before_malformed_load =
        world_state_fingerprint(&loaded_world);
    CHECK(find_save_bytes(save_path, handler_prefix, &handler_offset) &&
              rewrite_save_bytes(
                  save_path,
                  handler_offset + strlen("prototype apple\n    on "),
                  &malformed_token, 1) &&
              save_validate_file(save_path, &error) &&
              !save_load(&loaded_world, save_path, PAL_TEST_ASSET_ROOT,
                         &error) &&
              world_state_fingerprint(&loaded_world) ==
                  before_malformed_load,
          "checksummed malformed Behavior source rejects load transactionally");
}

static void test_lineage_inheritance_and_save(void) {
    PaliError error;
    const int tree_index = first_entity(&behavior_world, PROTOTYPE_TREE);
    const int unrelated_tree_index =
        next_entity(&behavior_world, PROTOTYPE_TREE, tree_index);
    CHECK(tree_index >= 0 && unrelated_tree_index >= 0,
          "Lineage test has two deterministic trees");
    if (tree_index < 0 || unrelated_tree_index < 0) {
        return;
    }
    Entity *tree = &behavior_world.universe.entities[tree_index];
    Entity *unrelated_tree =
        &behavior_world.universe.entities[unrelated_tree_index];
    const uint64_t tree_id = tree->id;
    const uint64_t unrelated_tree_id = unrelated_tree->id;
    WorldInput idle = {0};
    for (int tick = 0; tick < 180; ++tick) {
        world_step(&behavior_world, idle);
    }
    bool every_tree_has_one_child = true;
    for (uint16_t index = 0;
         index < behavior_world.universe.entity_count; ++index) {
        const Entity *candidate =
            &behavior_world.universe.entities[index];
        if (candidate->active && candidate->prototype == PROTOTYPE_TREE &&
            active_child_count(&behavior_world, candidate->id) != 1u) {
            every_tree_has_one_child = false;
        }
    }
    CHECK(every_tree_has_one_child &&
              active_child_count(&behavior_world, tree_id) == 1u,
          "each tree retains exactly one current child");

    const Entity *current_child =
        world_tree_current_fruit(&behavior_world, tree);
    const Entity *unrelated_child =
        world_tree_current_fruit(&behavior_world, unrelated_tree);
    CHECK(current_child != NULL && unrelated_child != NULL,
          "deterministic tree timers materialize current fruit");
    if (current_child == NULL || unrelated_child == NULL) {
        return;
    }
    const uint64_t current_child_id = current_child->id;
    const uint64_t unrelated_child_id = unrelated_child->id;
    CHECK(current_child->parent_id == tree_id &&
              current_child->birth_ordinal == 1u &&
              current_child_id == world_descendant_id(
                                      &behavior_world, tree_id, 1u,
                                      PROTOTYPE_APPLE),
          "descendant identity is stable across Parentage and birth ordinal");
    const InquiryProgress born = world_inquiry_progress(
        &behavior_world, INQUIRY_FRUIT_REMEMBERS);
    CHECK(born.completed_steps == 1u && born.step_count == 3u &&
              world_reconcile_inquiry_knowledge(&behavior_world) ==
                  KNOWLEDGE_GRANT_LINEAGE_DEPTH &&
              world_reconcile_inquiry_knowledge(&behavior_world) ==
                  KNOWLEDGE_GRANT_NONE &&
              behavior_world.knowledge.access_depth ==
                  (uint8_t)ACCESS_DEPTH_LINEAGE &&
              world_has_reach(&behavior_world, PATCH_REACH_LINEAGE) &&
              world_concept_access(&behavior_world, CONCEPT_PARENTAGE) ==
                  CONCEPT_ACCESS_READABLE &&
              world_concept_access(&behavior_world, CONCEPT_VIGOR) ==
                  CONCEPT_ACCESS_READABLE &&
              world_concept_access(&behavior_world, CONCEPT_WARMTH) ==
                  CONCEPT_ACCESS_READABLE,
          "a materialized child grants Lineage Knowledge exactly once");

    FruitLineageDraft inherited;
    CHECK(world_get_tree_lineage_draft(&behavior_world, tree, &inherited),
          "tree exposes its inherited future-fruit Draft");
    FruitLineageDraft kindle = inherited;
    kindle.behavior.aftertaste = BEHAVIOR_AFTERTASTE_KINDLE;
    CHECK(world_apply_tree_lineage_draft(&behavior_world, tree_id, kindle,
                                         &error) &&
              world_clear_tree_lineage_patch(&behavior_world, tree_id,
                                              &error) &&
              world_tree_lineage(&behavior_world, tree) == NULL,
          "normal Lineage Knowledge accepts and clears a KINDLE Draft");

    PaliValue current_nutrition;
    UseBehaviorDraft current_behavior;
    uint64_t current_nutrition_origin = 0;
    uint64_t current_behavior_origin = 0;
    const PatchReach current_nutrition_reach =
        world_entity_concept_provenance(
            &behavior_world, current_child, CONCEPT_NUTRITION,
            &current_nutrition_origin);
    const PatchReach current_behavior_reach =
        world_entity_behavior_provenance(
            &behavior_world, current_child, &current_behavior_origin);
    CHECK(world_get_entity_concept(&behavior_world, current_child,
                                   CONCEPT_NUTRITION,
                                   &current_nutrition) &&
              current_nutrition.type == PALI_VALUE_NUMBER &&
              current_nutrition.as.number == 18.0 &&
              world_get_entity_use_behavior_draft(
                  &behavior_world, current_child, &current_behavior),
          "the current child captures its deterministic first-birth meaning");

    const double unrelated_preview = world_tree_next_fruit_nutrition(
        &behavior_world, unrelated_tree);
    FruitLineageDraft changed = inherited;
    changed.nutrition = 31.0;
    changed.behavior.hunger = BEHAVIOR_HUNGER_SHARPEN;
    changed.behavior.voice = BEHAVIOR_VOICE_REMEMBER;
    changed.behavior.fate = BEHAVIOR_FATE_REMAIN;
    changed.behavior.aftertaste = BEHAVIOR_AFTERTASTE_QUICKEN;
    const double expected_nutrition = world_tree_preview_fruit_nutrition(
        &behavior_world, tree, changed);
    CHECK(expected_nutrition == 29.0 &&
              world_apply_tree_lineage_draft(
                  &behavior_world, tree_id, changed, &error),
          "next-fruit preview and application share one deterministic result");
    const LineageDefinition *lineage =
        world_tree_lineage(&behavior_world, tree);
    CHECK(lineage != NULL && lineage->has_nutrition_patch &&
              lineage->has_behavior_patch &&
              lineage->inherited_births == 0u &&
              world_tree_next_fruit_nutrition(&behavior_world, tree) ==
                  expected_nutrition &&
              world_inquiry_progress(&behavior_world,
                                     INQUIRY_FRUIT_REMEMBERS)
                      .completed_steps == 2u,
          "one tree records a sparse future-fruit Scar at Inquiry step two");

    PaliValue unchanged_nutrition;
    UseBehaviorDraft unchanged_behavior;
    uint64_t unchanged_nutrition_origin = 0;
    uint64_t unchanged_behavior_origin = 0;
    const Entity *same_current =
        world_tree_current_fruit(&behavior_world, tree);
    const Entity *same_unrelated =
        world_tree_current_fruit(&behavior_world, unrelated_tree);
    CHECK(same_current != NULL && same_current->id == current_child_id &&
              world_get_entity_concept(&behavior_world, same_current,
                                       CONCEPT_NUTRITION,
                                       &unchanged_nutrition) &&
              unchanged_nutrition.as.number ==
                  current_nutrition.as.number &&
              world_get_entity_use_behavior_draft(
                  &behavior_world, same_current, &unchanged_behavior) &&
              same_behavior_draft(unchanged_behavior, current_behavior) &&
              world_entity_concept_provenance(
                  &behavior_world, same_current, CONCEPT_NUTRITION,
                  &unchanged_nutrition_origin) ==
                  current_nutrition_reach &&
              unchanged_nutrition_origin == current_nutrition_origin &&
              world_entity_behavior_provenance(
                  &behavior_world, same_current,
                  &unchanged_behavior_origin) == current_behavior_reach &&
              unchanged_behavior_origin == current_behavior_origin &&
              same_unrelated != NULL &&
              same_unrelated->id == unrelated_child_id &&
              world_tree_lineage(&behavior_world, unrelated_tree) == NULL &&
              world_tree_next_fruit_nutrition(
                  &behavior_world, unrelated_tree) == unrelated_preview,
          "Lineage edits leave the current child and unrelated tree unchanged");

    CHECK(current_behavior.fate == BEHAVIOR_FATE_CEASE &&
              world_use_entity(
                  &behavior_world,
                  (int)(same_current - behavior_world.universe.entities),
                  &error) &&
              world_tree_current_fruit(&behavior_world, tree) == NULL &&
              tree->fruit_ticks == WORLD_FRUIT_REGROW_TICKS,
          "consuming the current child arms the bounded regrow timer");
    for (int tick = 0; tick < WORLD_FRUIT_REGROW_TICKS - 1; ++tick) {
        world_step(&behavior_world, idle);
    }
    CHECK(world_tree_current_fruit(&behavior_world, tree) == NULL &&
              tree->fruit_ticks == 1u,
          "regrowth waits for all 300 deterministic ticks");
    world_step(&behavior_world, idle);

    Entity *new_child = (Entity *)world_tree_current_fruit(
        &behavior_world, tree);
    CHECK(new_child != NULL && new_child->parent_id == tree_id &&
              new_child->birth_ordinal == 2u &&
              new_child->id == world_descendant_id(
                                   &behavior_world, tree_id, 2u,
                                   PROTOTYPE_APPLE),
          "the regrown child has the next stable descendant identity");
    if (new_child == NULL) {
        return;
    }
    const uint64_t new_child_id = new_child->id;
    PaliValue inherited_nutrition;
    UseBehaviorDraft inherited_behavior;
    uint64_t nutrition_origin = 0;
    uint64_t behavior_origin = 0;
    uint64_t parentage_origin = 0;
    uint64_t color_origin = 0;
    CHECK(world_get_entity_concept(&behavior_world, new_child,
                                   CONCEPT_NUTRITION,
                                   &inherited_nutrition) &&
              inherited_nutrition.type == PALI_VALUE_NUMBER &&
              inherited_nutrition.as.number == expected_nutrition &&
              world_get_entity_use_behavior_draft(
                  &behavior_world, new_child, &inherited_behavior) &&
              same_behavior_draft(inherited_behavior, changed.behavior) &&
              world_entity_concept_provenance(
                  &behavior_world, new_child, CONCEPT_NUTRITION,
                  &nutrition_origin) == PATCH_REACH_LINEAGE &&
              nutrition_origin == tree_id &&
              world_entity_behavior_provenance(
                  &behavior_world, new_child,
                  &behavior_origin) == PATCH_REACH_LINEAGE &&
              behavior_origin == tree_id &&
              world_entity_concept_provenance(
                  &behavior_world, new_child, CONCEPT_PARENTAGE,
                  &parentage_origin) == PATCH_REACH_LINEAGE &&
              parentage_origin == tree_id &&
              world_entity_concept_provenance(
                  &behavior_world, new_child, CONCEPT_COLOR,
                  &color_origin) == PATCH_REACH_PROTOTYPE &&
              color_origin == PROTOTYPE_APPLE,
          "new fruit captures only addressed nodes with Lineage provenance");
    lineage = world_tree_lineage(&behavior_world, tree);
    const InquiryProgress inherited_progress = world_inquiry_progress(
        &behavior_world, INQUIRY_FRUIT_REMEMBERS);
    CHECK(lineage != NULL && lineage->inherited_births == 1u &&
              inherited_progress.completed_steps == 3u &&
              inherited_progress.step_count == 3u &&
              world_active_inquiry(&behavior_world) == INQUIRY_NONE,
          "an inherited child completes The Fruit Remembers");

    behavior_world.embodiment.hunger = 10.0f;
    behavior_world.embodiment.vigor = 5.0f;
    CHECK(world_use_entity(
              &behavior_world,
              (int)(new_child - behavior_world.universe.entities),
              &error) &&
              fabsf(behavior_world.embodiment.hunger - 39.0f) < 0.001f &&
              fabsf(behavior_world.embodiment.vigor - 34.0f) < 0.001f &&
              new_child->active &&
              strcmp(behavior_world.message,
                     "The apple remembers being eaten.") == 0,
          "inherited SHARPEN and QUICKEN apply exactly while REMAIN preserves fruit");

    FruitLineageDraft revised = changed;
    revised.nutrition = 47.0;
    revised.behavior.hunger = BEHAVIOR_HUNGER_SOOTHE;
    revised.behavior.voice = BEHAVIOR_VOICE_SILENT;
    revised.behavior.fate = BEHAVIOR_FATE_CEASE;
    revised.behavior.aftertaste = BEHAVIOR_AFTERTASTE_KINDLE;
    CHECK(world_apply_tree_lineage_draft(&behavior_world, tree_id, revised,
                                         &error) &&
              world_get_entity_concept(&behavior_world, new_child,
                                       CONCEPT_NUTRITION,
                                       &inherited_nutrition) &&
              inherited_nutrition.as.number == expected_nutrition &&
              world_get_entity_use_behavior_draft(
                  &behavior_world, new_child, &inherited_behavior) &&
              same_behavior_draft(inherited_behavior, changed.behavior) &&
              world_tree_current_fruit(&behavior_world, tree)->id ==
                  new_child_id,
          "later tree edits cannot retroactively alter a materialized child");

    unrelated_child =
        world_tree_current_fruit(&behavior_world, unrelated_tree);
    CHECK(unrelated_child != NULL &&
              world_use_entity(
                  &behavior_world,
                  (int)(unrelated_child -
                        behavior_world.universe.entities),
                  &error) &&
              unrelated_tree->fruit_ticks == WORLD_FRUIT_REGROW_TICKS,
          "an unrelated tree exposes a nonzero timer for persistence");

    char save_path[PLATFORM_PATH_CAP];
    (void)snprintf(save_path, sizeof(save_path),
                   "%s/milestone-0.5-complete.pal", PAL_TEST_TMP_ROOT);
    const uint64_t before_save =
        world_state_fingerprint(&behavior_world);
    const float saved_vigor = behavior_world.embodiment.vigor;
    const uint32_t saved_births = tree->descendants_born;
    const uint16_t saved_timer = unrelated_tree->fruit_ticks;
    CHECK(save_write_atomic(&behavior_world, save_path, &error) &&
              save_load(&loaded_world, save_path, PAL_TEST_ASSET_ROOT,
                        &error) &&
              world_state_fingerprint(&loaded_world) == before_save,
          "save v5 restores the complete Lineage world exactly");
    const Entity *loaded_tree =
        world_entity_by_id_const(&loaded_world, tree_id);
    const Entity *loaded_unrelated_tree =
        world_entity_by_id_const(&loaded_world, unrelated_tree_id);
    const Entity *loaded_child =
        world_entity_by_id_const(&loaded_world, new_child_id);
    const LineageDefinition *loaded_lineage =
        world_tree_lineage(&loaded_world, loaded_tree);
    uint64_t loaded_parentage_origin = 0;
    uint64_t loaded_nutrition_origin = 0;
    uint64_t loaded_behavior_origin = 0;
    UseBehaviorDraft loaded_child_behavior;
    CHECK(loaded_tree != NULL && loaded_unrelated_tree != NULL &&
              loaded_child != NULL && loaded_child->active &&
              loaded_child->parent_id == tree_id &&
              loaded_child->birth_ordinal == 2u &&
              loaded_tree->descendants_born == saved_births &&
              loaded_unrelated_tree->fruit_ticks == saved_timer &&
              loaded_world.embodiment.vigor == saved_vigor &&
              loaded_lineage != NULL &&
              loaded_lineage->inherited_births == 1u &&
              loaded_lineage->has_nutrition_patch &&
              loaded_lineage->has_behavior_patch &&
              loaded_lineage->draft.nutrition == revised.nutrition &&
              same_behavior_draft(loaded_lineage->draft.behavior,
                                  revised.behavior) &&
              world_get_entity_use_behavior_draft(
                  &loaded_world, loaded_child, &loaded_child_behavior) &&
              same_behavior_draft(loaded_child_behavior,
                                  changed.behavior) &&
              world_entity_concept_provenance(
                  &loaded_world, loaded_child, CONCEPT_PARENTAGE,
                  &loaded_parentage_origin) == PATCH_REACH_LINEAGE &&
              loaded_parentage_origin == tree_id &&
              world_entity_concept_provenance(
                  &loaded_world, loaded_child, CONCEPT_NUTRITION,
                  &loaded_nutrition_origin) == PATCH_REACH_LINEAGE &&
              loaded_nutrition_origin == tree_id &&
              world_entity_behavior_provenance(
                  &loaded_world, loaded_child,
                  &loaded_behavior_origin) == PATCH_REACH_LINEAGE &&
              loaded_behavior_origin == tree_id,
          "v5 preserves counters, timers, drafts, provenance, and vigor");

    world_c = loaded_world;
    Entity *duplicate_tree = world_entity_by_id(&world_c, tree_id);
    const uint32_t duplicate_ordinal = duplicate_tree->descendants_born + 1u;
    const uint64_t duplicate_id = world_descendant_id(
        &world_c, tree_id, duplicate_ordinal, PROTOTYPE_APPLE);
    CHECK(world_restore_descendant(&world_c, duplicate_id, tree_id,
                                   duplicate_ordinal, PROTOTYPE_APPLE,
                                   &error),
          "test fixture can construct a second valid current child");
    duplicate_tree = world_entity_by_id(&world_c, tree_id);
    duplicate_tree->descendants_born = duplicate_ordinal;
    char invalid_path[PLATFORM_PATH_CAP];
    (void)snprintf(invalid_path, sizeof(invalid_path),
                   "%s/milestone-0.5-duplicate-child.pal",
                   PAL_TEST_TMP_ROOT);
    CHECK(!save_write_atomic(&world_c, invalid_path, &error),
          "serializer rejects two active current children transactionally");
}

static void test_recycled_override_capacity(void) {
    const LineageDefinition *fixture_lineage = NULL;
    for (int index = 0; index < WORLD_MAX_LINEAGES; ++index) {
        if (loaded_world.universe.lineages[index].active) {
            fixture_lineage = &loaded_world.universe.lineages[index];
            break;
        }
    }
    CHECK(fixture_lineage != NULL,
          "capacity regression reuses the saved Lineage fixture");
    if (fixture_lineage == NULL) {
        return;
    }

    world_c = loaded_world;
    Entity *tree = world_entity_by_id(
        &world_c, fixture_lineage->progenitor_id);
    Entity *recycled = tree != NULL
                           ? (Entity *)world_tree_current_fruit(&world_c, tree)
                           : NULL;
    CHECK(tree != NULL && recycled != NULL && recycled->local_override >= 0,
          "fixture has a bound descendant override to recycle");
    if (tree == NULL || recycled == NULL || recycled->local_override < 0) {
        return;
    }
    const uint64_t recycled_id = recycled->id;
    const int recycled_slot = recycled->local_override;
    const uint32_t next_ordinal = tree->descendants_born + 1u;
    const uint64_t next_id = world_descendant_id(
        &world_c, tree->id, next_ordinal, PROTOTYPE_APPLE);
    const LineageDefinition *lineage = world_tree_lineage(&world_c, tree);
    const uint32_t inherited_before =
        lineage != NULL ? lineage->inherited_births : 0u;

    for (uint16_t index = 0; index < world_c.universe.entity_count; ++index) {
        Entity *entity = &world_c.universe.entities[index];
        if (entity->parent_id != 0 && entity->id != recycled_id) {
            entity->active = true;
        }
    }
    recycled->active = false;
    recycled->dirty = true;
    tree->fruit_ticks = 1u;
    tree->dirty = true;
    for (int index = 0; index < WORLD_MAX_LOCAL_OVERRIDES; ++index) {
        LocalOverride *override = &world_c.universe.local_overrides[index];
        if (override->active) {
            continue;
        }
        memset(override, 0, sizeof(*override));
        override->active = true;
        override->entity_id = UINT64_C(0xf000000000000000) +
                              (uint64_t)(unsigned int)index;
        override->value_count = 1u;
        override->values[0].concept = CONCEPT_NUTRITION;
        override->values[0].value = pali_number(20.0);
        override->values[0].provenance_reach =
            (uint8_t)PATCH_REACH_ENTITY;
        override->values[0].provenance_id = override->entity_id;
    }
    uint8_t active_overrides = 0;
    for (int index = 0; index < WORLD_MAX_LOCAL_OVERRIDES; ++index) {
        if (world_c.universe.local_overrides[index].active) {
            active_overrides++;
        }
    }
    const LocalOverride *retained =
        &world_c.universe.local_overrides[recycled_slot];
    CHECK(active_overrides == WORLD_MAX_LOCAL_OVERRIDES &&
              retained->active && retained->entity_id == recycled_id &&
              world_tree_current_fruit(&world_c, tree) == NULL &&
              tree->fruit_ticks == 1u && lineage != NULL &&
              (lineage->has_nutrition_patch ||
               lineage->has_behavior_patch),
          "full override storage retains one recyclable bound Scar");

    WorldInput idle = {0};
    world_step(&world_c, idle);
    const Entity *born = world_tree_current_fruit(&world_c, tree);
    lineage = world_tree_lineage(&world_c, tree);
    active_overrides = 0;
    for (int index = 0; index < WORLD_MAX_LOCAL_OVERRIDES; ++index) {
        if (world_c.universe.local_overrides[index].active) {
            active_overrides++;
        }
    }
    CHECK(born != NULL && born->id == next_id && born->active &&
              born->birth_ordinal == next_ordinal &&
              born->local_override == recycled_slot &&
              world_entity_by_id_const(&world_c, recycled_id) == NULL &&
              world_c.universe.local_overrides[recycled_slot].entity_id ==
                  next_id &&
              active_overrides == WORLD_MAX_LOCAL_OVERRIDES &&
              tree->descendants_born == next_ordinal &&
              tree->fruit_ticks == 0u && lineage != NULL &&
              lineage->inherited_births == inherited_before + 1u,
          "one tick recycles the bound override and materializes next fruit");
}

int main(void) {
    test_language();
    test_generation();
    test_knowledge_revelation();
    test_patch_gameplay_and_save();
    test_inquiry_and_behavior_grammar();
    test_lineage_inheritance_and_save();
    test_recycled_override_capacity();
    if (failures != 0) {
        (void)fprintf(stderr, "%d test failure(s)\n", failures);
        return 1;
    }
    (void)printf("PALIMPSEST core checks passed\n");
    return 0;
}
