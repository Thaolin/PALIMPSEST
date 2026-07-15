#include "save.h"

#include "platform.h"

#include <math.h>
#include <stdio.h>
#include <string.h>

#define SAVE_BUFFER_CAPACITY (128u * 1024u)
#define SAVE_HEADER_SIZE 24u
#define SAVE_LEGACY_VERSION 2u

static const uint8_t SAVE_MAGIC[8] = {'P', 'A', 'L', 'S', 'A', 'V', 'E', '2'};

typedef struct SaveBuffer {
    uint8_t *data;
    size_t length;
    size_t position;
    bool ok;
    PaliError *error;
} SaveBuffer;

/* ponytail: single-threaded game owns these buffers; no heap or allocator API. */
static uint8_t write_storage[SAVE_BUFFER_CAPACITY];
static uint8_t read_storage[SAVE_BUFFER_CAPACITY];
static World load_candidate;

_Static_assert(CONCEPT_COUNT <= 32,
               "known notation masks require at most 32 concepts");
_Static_assert(PROTOTYPE_COUNT <= 32,
               "observation masks require at most 32 prototypes");

static bool save_error(PaliError *error, const char *message) {
    if (error != NULL) {
        error->line = 0;
        error->column = 0;
        (void)snprintf(error->message, sizeof(error->message), "%s", message);
    }
    return false;
}

static void buffer_fail(SaveBuffer *buffer, const char *message) {
    if (!buffer->ok) {
        return;
    }
    buffer->ok = false;
    (void)save_error(buffer->error, message);
}

static void put_bytes(SaveBuffer *buffer, const void *source, size_t count) {
    if (!buffer->ok || count > SAVE_BUFFER_CAPACITY - buffer->length) {
        buffer_fail(buffer, "save exceeds its 128 KiB memory contract");
        return;
    }
    memcpy(buffer->data + buffer->length, source, count);
    buffer->length += count;
}

static void put_u8(SaveBuffer *buffer, uint8_t value) {
    put_bytes(buffer, &value, 1);
}

static void put_u16(SaveBuffer *buffer, uint16_t value) {
    uint8_t bytes[2] = {(uint8_t)(value & UINT16_C(0xff)),
                        (uint8_t)(value >> 8)};
    put_bytes(buffer, bytes, sizeof(bytes));
}

static void put_u32(SaveBuffer *buffer, uint32_t value) {
    uint8_t bytes[4];
    for (int index = 0; index < 4; ++index) {
        bytes[index] = (uint8_t)(value >> (index * 8));
    }
    put_bytes(buffer, bytes, sizeof(bytes));
}

static void put_u64(SaveBuffer *buffer, uint64_t value) {
    uint8_t bytes[8];
    for (int index = 0; index < 8; ++index) {
        bytes[index] = (uint8_t)(value >> (index * 8));
    }
    put_bytes(buffer, bytes, sizeof(bytes));
}

static void put_float(SaveBuffer *buffer, float value) {
    if (!isfinite(value)) {
        buffer_fail(buffer, "save contains a non-finite physical value");
        return;
    }
    uint32_t bits = 0;
    memcpy(&bits, &value, sizeof(bits));
    put_u32(buffer, bits);
}

static void put_double(SaveBuffer *buffer, double value) {
    if (!isfinite(value)) {
        buffer_fail(buffer, "save contains a non-finite numeric value");
        return;
    }
    uint64_t bits = 0;
    memcpy(&bits, &value, sizeof(bits));
    put_u64(buffer, bits);
}

static void put_string(SaveBuffer *buffer, const char *text,
                       size_t capacity) {
    const char *terminator =
        text != NULL ? memchr(text, '\0', capacity) : NULL;
    if (terminator == NULL) {
        buffer_fail(buffer, "save string is not terminated within its bound");
        return;
    }
    const size_t length = (size_t)(terminator - text);
    put_u16(buffer, (uint16_t)length);
    put_bytes(buffer, text, length);
}

static void put_value(SaveBuffer *buffer, PaliValue value) {
    put_u8(buffer, (uint8_t)value.type);
    switch (value.type) {
        case PALI_VALUE_NIL:
            break;
        case PALI_VALUE_NUMBER:
            put_double(buffer, value.as.number);
            break;
        case PALI_VALUE_BOOL:
            put_u8(buffer, value.as.boolean ? 1u : 0u);
            break;
        case PALI_VALUE_TEXT:
            put_string(buffer, value.as.text, sizeof(value.as.text));
            break;
        default:
            buffer_fail(buffer, "unknown PALI value in entity state");
            break;
    }
}

static uint64_t checksum(const uint8_t *data, size_t length) {
    uint64_t hash = UINT64_C(1469598103934665603);
    for (size_t index = 0; index < length; ++index) {
        hash ^= data[index];
        hash *= UINT64_C(1099511628211);
    }
    return hash;
}

static bool knowledge_is_valid(const KnowledgeState *knowledge) {
    if (knowledge->observed_prototypes[CONCEPT_NONE] != 0) {
        return false;
    }
    uint64_t valid_concepts = 0;
    uint32_t valid_notations = 0;
    for (ConceptId id = CONCEPT_TAG; id < CONCEPT_COUNT; ++id) {
        valid_concepts |= concept_bit(id);
        valid_notations |= UINT32_C(1) << (unsigned int)id;
    }
    uint32_t valid_reach = 0;
    for (int reach = 0; reach < PATCH_REACH_COUNT; ++reach) {
        valid_reach |= patch_reach_bit((PatchReach)reach);
    }
    uint32_t valid_prototypes = 0;
    for (int prototype = 0; prototype < PROTOTYPE_COUNT; ++prototype) {
        valid_prototypes |= UINT32_C(1) << (unsigned int)prototype;
    }
    for (ConceptId id = CONCEPT_TAG; id < CONCEPT_COUNT; ++id) {
        if ((knowledge->observed_prototypes[id] & ~valid_prototypes) != 0) {
            return false;
        }
    }
    return (knowledge->perceived_concepts & ~valid_concepts) == 0 &&
           (knowledge->readable_concepts &
            ~knowledge->perceived_concepts) == 0 &&
           (knowledge->patchable_concepts &
            ~knowledge->readable_concepts) == 0 &&
           (knowledge->known_notations & ~valid_notations) == 0 &&
           (knowledge->reach_mask & ~valid_reach) == 0 &&
           knowledge->access_depth <= (uint8_t)ACCESS_DEPTH_LAW;
}

static void patch_u32(uint8_t *data, size_t offset, uint32_t value) {
    for (int index = 0; index < 4; ++index) {
        data[offset + (size_t)index] = (uint8_t)(value >> (index * 8));
    }
}

static void patch_u64(uint8_t *data, size_t offset, uint64_t value) {
    for (int index = 0; index < 8; ++index) {
        data[offset + (size_t)index] = (uint8_t)(value >> (index * 8));
    }
}

static void serialize_world(SaveBuffer *buffer, const World *world) {
    if (!knowledge_is_valid(&world->knowledge)) {
        buffer_fail(buffer, "save contains invalid Knowledge state");
        return;
    }
    if (world->universe.entity_count > WORLD_MAX_ENTITIES) {
        buffer_fail(buffer, "World entity count exceeds fixed storage");
        return;
    }
    for (int index = 0; index < WORLD_MAX_LOCAL_OVERRIDES; ++index) {
        if (world->universe.local_overrides[index].value_count >
            WORLD_LOCAL_PATCH_VALUES) {
            buffer_fail(buffer, "Entity Patch exceeds fixed storage");
            return;
        }
    }
    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        if (world->universe.entities[index].state_count >
            WORLD_INSTANCE_PROPERTIES) {
            buffer_fail(buffer, "Entity state exceeds fixed storage");
            return;
        }
    }
    put_bytes(buffer, SAVE_MAGIC, sizeof(SAVE_MAGIC));
    put_u32(buffer, PAL_SAVE_VERSION);
    put_u32(buffer, 0);
    put_u64(buffer, 0);

    put_u64(buffer, world->universe.root_seed);
    put_u64(buffer, world->universe.tick);
    put_u64(buffer, world->knowledge.perceived_concepts);
    put_u64(buffer, world->knowledge.readable_concepts);
    put_u64(buffer, world->knowledge.patchable_concepts);
    put_u32(buffer, world->knowledge.known_notations);
    put_u32(buffer, world->knowledge.reach_mask);
    put_u8(buffer, world->knowledge.access_depth);
    put_u64(buffer, world->embodiment.entity_id);
    put_float(buffer, world->embodiment.x);
    put_float(buffer, world->embodiment.y);
    put_float(buffer, world->embodiment.hunger);
    put_float(buffer, world->embodiment.warmth);

    uint8_t patch_count = 0;
    for (int index = 0; index < PROTOTYPE_COUNT; ++index) {
        if (world->universe.prototypes[index].patched) {
            patch_count++;
        }
    }
    put_u8(buffer, patch_count);
    for (int index = 0; index < PROTOTYPE_COUNT; ++index) {
        const PrototypeDefinition *definition =
            &world->universe.prototypes[index];
        if (definition->patched) {
            put_u8(buffer, (uint8_t)index);
            put_string(buffer, definition->current_source,
                       sizeof(definition->current_source));
        }
    }

    uint8_t local_count = 0;
    for (int index = 0; index < WORLD_MAX_LOCAL_OVERRIDES; ++index) {
        if (world->universe.local_overrides[index].active) {
            local_count++;
        }
    }
    put_u8(buffer, local_count);
    for (int index = 0; index < WORLD_MAX_LOCAL_OVERRIDES; ++index) {
        const LocalOverride *override =
            &world->universe.local_overrides[index];
        if (override->active) {
            put_u64(buffer, override->entity_id);
            put_u8(buffer, override->value_count);
            for (uint8_t value = 0; value < override->value_count; ++value) {
                put_u16(buffer, override->values[value].concept);
                put_value(buffer, override->values[value].value);
            }
        }
    }

    uint16_t dirty_count = 0;
    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        if (world->universe.entities[index].dirty) {
            dirty_count++;
        }
    }
    put_u16(buffer, dirty_count);
    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        const Entity *entity = &world->universe.entities[index];
        if (!entity->dirty) {
            continue;
        }
        put_u64(buffer, entity->id);
        put_u8(buffer, entity->active ? 1u : 0u);
        put_float(buffer, entity->x);
        put_float(buffer, entity->y);
        put_float(buffer, entity->move_x);
        put_float(buffer, entity->move_y);
        put_u64(buffer, entity->rng_state);
        put_u16(buffer, entity->direction_ticks);
        put_u8(buffer, entity->state_count);
        for (uint8_t property = 0; property < entity->state_count; ++property) {
            put_string(buffer, entity->state[property].name,
                       sizeof(entity->state[property].name));
            put_value(buffer, entity->state[property].value);
        }
    }
    put_string(buffer, world->message, sizeof(world->message));
    for (ConceptId id = CONCEPT_NONE; id < CONCEPT_COUNT; ++id) {
        put_u32(buffer, world->knowledge.observed_prototypes[id]);
    }

    if (buffer->ok) {
        const size_t payload_size = buffer->length - SAVE_HEADER_SIZE;
        if (payload_size > UINT32_MAX) {
            buffer_fail(buffer, "save payload length overflow");
            return;
        }
        patch_u32(buffer->data, 12, (uint32_t)payload_size);
        patch_u64(buffer->data, 16,
                  checksum(buffer->data + SAVE_HEADER_SIZE, payload_size));
    }
}

static bool load_file_bytes(const char *path, uint8_t *storage,
                            size_t *out_length, PaliError *error) {
    FILE *file = fopen(path, "rb");
    if (file == NULL) {
        return save_error(error, "save file does not exist or cannot be read");
    }
    const size_t bytes = fread(storage, 1, SAVE_BUFFER_CAPACITY, file);
    bool failed = ferror(file) != 0;
    int extra = EOF;
    if (!failed) {
        extra = fgetc(file);
        failed = ferror(file) != 0;
    }
    (void)fclose(file);
    if (failed || extra != EOF) {
        return save_error(error, "save exceeds its 128 KiB memory contract");
    }
    *out_length = bytes;
    return true;
}

static uint32_t raw_u32(const uint8_t *data) {
    uint32_t value = 0;
    for (int index = 0; index < 4; ++index) {
        value |= (uint32_t)data[index] << (index * 8);
    }
    return value;
}

static uint64_t raw_u64(const uint8_t *data) {
    uint64_t value = 0;
    for (int index = 0; index < 8; ++index) {
        value |= (uint64_t)data[index] << (index * 8);
    }
    return value;
}

static bool validate_bytes(const uint8_t *data, size_t length,
                           PaliError *error) {
    if (length < SAVE_HEADER_SIZE ||
        memcmp(data, SAVE_MAGIC, sizeof(SAVE_MAGIC)) != 0) {
        return save_error(error, "save header is not PALIMPSEST format");
    }
    const uint32_t version = raw_u32(data + 8);
    if (version != SAVE_LEGACY_VERSION && version != PAL_SAVE_VERSION) {
        return save_error(error, "save format version is unsupported");
    }
    const uint32_t payload_size = raw_u32(data + 12);
    if ((size_t)payload_size != length - SAVE_HEADER_SIZE) {
        return save_error(error, "save payload length is invalid");
    }
    const uint64_t expected = raw_u64(data + 16);
    const uint64_t actual =
        checksum(data + SAVE_HEADER_SIZE, (size_t)payload_size);
    if (expected != actual) {
        return save_error(error, "save checksum does not match");
    }
    return true;
}

bool save_validate_file(const char *path, PaliError *error) {
    if (path == NULL || path[0] == '\0') {
        return save_error(error, "save target is invalid");
    }
    size_t length = 0;
    if (!load_file_bytes(path, read_storage, &length, error)) {
        return false;
    }
    return validate_bytes(read_storage, length, error);
}

bool save_write_atomic(const World *world, const char *path, PaliError *error) {
    if (world == NULL || path == NULL || path[0] == '\0') {
        return save_error(error, "save target is invalid");
    }
    if (error != NULL) {
        memset(error, 0, sizeof(*error));
    }
    SaveBuffer buffer = {write_storage, 0, 0, true, error};
    serialize_world(&buffer, world);
    if (!buffer.ok) {
        return false;
    }
    char temporary_path[PLATFORM_PATH_CAP];
    const int length =
        snprintf(temporary_path, sizeof(temporary_path), "%s.tmp", path);
    if (length < 0 || (size_t)length >= sizeof(temporary_path)) {
        return save_error(error, "temporary save path is too long");
    }
    FILE *file = fopen(temporary_path, "wb");
    if (file == NULL) {
        return save_error(error, "could not open temporary save");
    }
    const bool written = fwrite(buffer.data, 1, buffer.length, file) ==
                         buffer.length;
    const bool flushed = written && platform_flush_file(file, error);
    const bool closed = fclose(file) == 0;
    if (!written || !flushed || !closed) {
        return save_error(error, "could not complete temporary save");
    }
    if (!save_validate_file(temporary_path, error)) {
        return false;
    }
    return platform_atomic_replace(temporary_path, path, error);
}

static bool take_bytes(SaveBuffer *buffer, void *out, size_t count) {
    if (!buffer->ok || buffer->position > buffer->length ||
        count > buffer->length - buffer->position) {
        buffer_fail(buffer, "save payload ended unexpectedly");
        return false;
    }
    memcpy(out, buffer->data + buffer->position, count);
    buffer->position += count;
    return true;
}

static uint8_t take_u8(SaveBuffer *buffer) {
    uint8_t value = 0;
    (void)take_bytes(buffer, &value, 1);
    return value;
}

static uint16_t take_u16(SaveBuffer *buffer) {
    uint8_t bytes[2] = {0, 0};
    (void)take_bytes(buffer, bytes, sizeof(bytes));
    return (uint16_t)((uint16_t)bytes[0] | ((uint16_t)bytes[1] << 8));
}

static uint32_t take_u32(SaveBuffer *buffer) {
    uint8_t bytes[4] = {0, 0, 0, 0};
    (void)take_bytes(buffer, bytes, sizeof(bytes));
    return raw_u32(bytes);
}

static uint64_t take_u64(SaveBuffer *buffer) {
    uint8_t bytes[8] = {0, 0, 0, 0, 0, 0, 0, 0};
    (void)take_bytes(buffer, bytes, sizeof(bytes));
    return raw_u64(bytes);
}

static float take_float(SaveBuffer *buffer) {
    const uint32_t bits = take_u32(buffer);
    float value = 0.0f;
    memcpy(&value, &bits, sizeof(value));
    return value;
}

static double take_double(SaveBuffer *buffer) {
    const uint64_t bits = take_u64(buffer);
    double value = 0.0;
    memcpy(&value, &bits, sizeof(value));
    return value;
}

static bool take_string(SaveBuffer *buffer, char *out, size_t capacity) {
    const uint16_t length = take_u16(buffer);
    if (!buffer->ok || (size_t)length >= capacity) {
        buffer_fail(buffer, "save string exceeds destination capacity");
        return false;
    }
    if (!take_bytes(buffer, out, length)) {
        return false;
    }
    out[length] = '\0';
    return true;
}

static PaliValue take_value(SaveBuffer *buffer) {
    PaliValue value;
    memset(&value, 0, sizeof(value));
    value.type = (PaliValueType)take_u8(buffer);
    switch (value.type) {
        case PALI_VALUE_NIL:
            break;
        case PALI_VALUE_NUMBER:
            value.as.number = take_double(buffer);
            if (buffer->ok && !isfinite(value.as.number)) {
                buffer_fail(buffer,
                            "save contains a non-finite numeric value");
            }
            break;
        case PALI_VALUE_BOOL: {
            const uint8_t boolean = take_u8(buffer);
            if (buffer->ok && boolean > 1u) {
                buffer_fail(buffer, "save contains an invalid Boolean value");
            }
            value.as.boolean = boolean != 0;
            break;
        }
        case PALI_VALUE_TEXT:
            (void)take_string(buffer, value.as.text, sizeof(value.as.text));
            break;
        default:
            buffer_fail(buffer, "save contains an unknown PALI value type");
            break;
    }
    return value;
}

static bool loaded_position_is_valid(float x, float y) {
    return isfinite(x) && isfinite(y) && x >= 0.0f && y >= 0.0f &&
           x < (float)(WORLD_MAP_WIDTH * WORLD_TILE_SIZE) &&
           y < (float)(WORLD_MAP_HEIGHT * WORLD_TILE_SIZE);
}

static bool loaded_motion_is_valid(float x, float y) {
    const float limit =
        (float)(WORLD_MAP_WIDTH * WORLD_TILE_SIZE);
    return isfinite(x) && isfinite(y) && fabsf(x) <= limit &&
           fabsf(y) <= limit;
}

bool save_load(World *world, const char *path, const char *pali_asset_root,
               PaliError *error) {
    if (world == NULL || path == NULL || path[0] == '\0' ||
        pali_asset_root == NULL || pali_asset_root[0] == '\0') {
        return save_error(error, "load target is invalid");
    }
    size_t length = 0;
    if (!load_file_bytes(path, read_storage, &length, error) ||
        !validate_bytes(read_storage, length, error)) {
        return false;
    }
    const uint32_t version = raw_u32(read_storage + 8);
    SaveBuffer buffer = {read_storage, length, SAVE_HEADER_SIZE, true, error};
    const uint64_t seed = take_u64(&buffer);
    if (!buffer.ok || !world_init(&load_candidate, seed, pali_asset_root, error)) {
        return false;
    }
    const uint64_t genesis_embodiment_id =
        load_candidate.embodiment.entity_id;
    load_candidate.universe.tick = take_u64(&buffer);
    load_candidate.knowledge.perceived_concepts = take_u64(&buffer);
    load_candidate.knowledge.readable_concepts = take_u64(&buffer);
    load_candidate.knowledge.patchable_concepts = take_u64(&buffer);
    load_candidate.knowledge.known_notations = take_u32(&buffer);
    memset(load_candidate.knowledge.observed_prototypes, 0,
           sizeof(load_candidate.knowledge.observed_prototypes));
    if (version == SAVE_LEGACY_VERSION) {
        load_candidate.knowledge.known_notations |=
            (uint32_t)concept_bit(CONCEPT_NUTRITION);
    }
    load_candidate.knowledge.reach_mask = take_u32(&buffer);
    load_candidate.knowledge.access_depth = take_u8(&buffer);
    load_candidate.embodiment.entity_id = take_u64(&buffer);
    load_candidate.embodiment.x = take_float(&buffer);
    load_candidate.embodiment.y = take_float(&buffer);
    load_candidate.embodiment.hunger = take_float(&buffer);
    load_candidate.embodiment.warmth = take_float(&buffer);
    if (buffer.ok &&
        (!knowledge_is_valid(&load_candidate.knowledge) ||
         load_candidate.embodiment.entity_id != genesis_embodiment_id ||
         !loaded_position_is_valid(load_candidate.embodiment.x,
                                   load_candidate.embodiment.y) ||
         !isfinite(load_candidate.embodiment.hunger) ||
         load_candidate.embodiment.hunger < 0.0f ||
         load_candidate.embodiment.hunger > 100.0f ||
         !isfinite(load_candidate.embodiment.warmth) ||
         load_candidate.embodiment.warmth < 0.0f ||
         load_candidate.embodiment.warmth > 100.0f)) {
        buffer_fail(&buffer, "save contains invalid embodied or Knowledge state");
    }

    const uint8_t patch_count = take_u8(&buffer);
    if (patch_count > PROTOTYPE_COUNT) {
        buffer_fail(&buffer, "save has too many prototype patches");
    }
    char source[PALI_SOURCE_CAP];
    for (uint8_t index = 0; index < patch_count && buffer.ok; ++index) {
        const uint8_t prototype = take_u8(&buffer);
        if (prototype >= PROTOTYPE_COUNT) {
            buffer_fail(&buffer,
                        "save references an unknown Prototype Patch");
            break;
        }
        if (!take_string(&buffer, source, sizeof(source))) {
            break;
        }
        if (!world_apply_prototype_source(&load_candidate,
                                          (PrototypeId)prototype, source,
                                          error)) {
            buffer.ok = false;
        }
    }

    const uint8_t local_count = take_u8(&buffer);
    if (local_count > WORLD_MAX_LOCAL_OVERRIDES) {
        buffer_fail(&buffer, "save has too many local overrides");
    }
    for (uint8_t index = 0; index < local_count && buffer.ok; ++index) {
        const uint64_t entity_id = take_u64(&buffer);
        const uint8_t value_count = take_u8(&buffer);
        if (value_count > WORLD_LOCAL_PATCH_VALUES) {
            buffer_fail(&buffer, "Entity Patch exceeds value capacity");
            break;
        }
        for (uint8_t value = 0; value < value_count && buffer.ok; ++value) {
            const ConceptId concept = take_u16(&buffer);
            const PaliValue patched_value = take_value(&buffer);
            if (!buffer.ok) {
                break;
            }
            if (lexicon_find_by_id(concept) == NULL ||
                !world_apply_entity_value_patch(&load_candidate, entity_id,
                                                concept, patched_value,
                                                error)) {
                buffer.ok = false;
            }
        }
    }

    const uint16_t dirty_count = take_u16(&buffer);
    if (dirty_count > load_candidate.universe.entity_count) {
        buffer_fail(&buffer, "save has too many changed entities");
    }
    for (uint16_t index = 0; index < dirty_count && buffer.ok; ++index) {
        const uint64_t id = take_u64(&buffer);
        Entity *entity = world_entity_by_id(&load_candidate, id);
        if (entity == NULL) {
            buffer_fail(&buffer, "save references an entity outside genesis");
            break;
        }
        const uint8_t active = take_u8(&buffer);
        const float x = take_float(&buffer);
        const float y = take_float(&buffer);
        const float move_x = take_float(&buffer);
        const float move_y = take_float(&buffer);
        entity->rng_state = take_u64(&buffer);
        entity->direction_ticks = take_u16(&buffer);
        entity->state_count = take_u8(&buffer);
        if (buffer.ok &&
            (active > 1u || !loaded_position_is_valid(x, y) ||
             !loaded_motion_is_valid(move_x, move_y))) {
            buffer_fail(&buffer, "save entity has invalid physical state");
            break;
        }
        if (entity->state_count > WORLD_INSTANCE_PROPERTIES) {
            buffer_fail(&buffer, "save entity state exceeds property capacity");
            break;
        }
        entity->active = active != 0;
        entity->x = x;
        entity->y = y;
        entity->move_x = move_x;
        entity->move_y = move_y;
        for (uint8_t property = 0;
             property < entity->state_count && buffer.ok; ++property) {
            if (!take_string(&buffer, entity->state[property].name,
                             sizeof(entity->state[property].name))) {
                break;
            }
            entity->state[property].value = take_value(&buffer);
            const ConceptDefinition *concept = lexicon_find_by_name(
                entity->state[property].name);
            if (buffer.ok && concept != NULL &&
                !lexicon_value_is_valid(
                    concept, entity->state[property].value)) {
                buffer_fail(&buffer,
                            "save entity state violates a semantic contract");
            }
        }
        entity->dirty = true;
    }
    if (buffer.ok &&
        !take_string(&buffer, load_candidate.message,
                     sizeof(load_candidate.message))) {
        return false;
    }
    if (version == PAL_SAVE_VERSION) {
        for (ConceptId id = CONCEPT_NONE; id < CONCEPT_COUNT; ++id) {
            load_candidate.knowledge.observed_prototypes[id] =
                take_u32(&buffer);
        }
    }
    if (buffer.ok && !knowledge_is_valid(&load_candidate.knowledge)) {
        buffer_fail(&buffer, "save contains invalid Knowledge state");
    }
    if (!buffer.ok || buffer.position != buffer.length) {
        if (buffer.ok) {
            buffer_fail(&buffer, "save payload contains trailing bytes");
        }
        return false;
    }
    *world = load_candidate;
    return true;
}

_Static_assert(SAVE_BUFFER_CAPACITY >=
                   PROTOTYPE_COUNT * PALI_SOURCE_CAP +
                       WORLD_MAX_LOCAL_OVERRIDES * WORLD_LOCAL_PATCH_VALUES *
                           sizeof(LocalPatchValue),
               "save buffer cannot hold maximum source patches");
