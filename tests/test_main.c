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
static uint8_t save_mutation[16384];

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
}

int main(void) {
    test_language();
    test_generation();
    test_patch_gameplay_and_save();
    if (failures != 0) {
        (void)fprintf(stderr, "%d test failure(s)\n", failures);
        return 1;
    }
    (void)printf("PALIMPSEST core checks passed\n");
    return 0;
}
