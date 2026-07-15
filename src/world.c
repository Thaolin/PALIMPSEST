#include "world.h"

#include <math.h>
#include <stdio.h>
#include <string.h>

typedef struct DeterministicRng {
    uint64_t state;
} DeterministicRng;

typedef struct HostContext {
    World *world;
    Entity *self;
    const PaliProgram *program;
} HostContext;

static const char *const PROTOTYPE_NAMES[PROTOTYPE_COUNT] = {
    "stone", "tree", "apple", "fire", "moth"};

static uint64_t mix64(uint64_t value) {
    value += UINT64_C(0x9e3779b97f4a7c15);
    value = (value ^ (value >> 30)) * UINT64_C(0xbf58476d1ce4e5b9);
    value = (value ^ (value >> 27)) * UINT64_C(0x94d049bb133111eb);
    return value ^ (value >> 31);
}

static DeterministicRng rng_stream(uint64_t root_seed, uint64_t stream_tag) {
    DeterministicRng rng;
    rng.state = mix64(root_seed ^ mix64(stream_tag));
    return rng;
}

static uint64_t rng_next(DeterministicRng *rng) {
    rng->state += UINT64_C(0x9e3779b97f4a7c15);
    return mix64(rng->state);
}

static int rng_range(DeterministicRng *rng, int upper_exclusive) {
    return (int)(rng_next(rng) % (uint64_t)upper_exclusive);
}

static void set_error(PaliError *error, int line, int column,
                      const char *message) {
    if (error == NULL) {
        return;
    }
    error->line = line;
    error->column = column;
    (void)snprintf(error->message, sizeof(error->message), "%s", message);
}

const char *world_prototype_name(PrototypeId prototype) {
    if (prototype < 0 || prototype >= PROTOTYPE_COUNT) {
        return "unknown";
    }
    return PROTOTYPE_NAMES[prototype];
}

static bool read_source_file(const char *root, const char *name, char *out,
                             PaliError *error) {
    if (root == NULL || root[0] == '\0') {
        set_error(error, 0, 0, "PALI asset root was not provided");
        return false;
    }
    char path[512];
    const int length = snprintf(path, sizeof(path), "%s/%s.pali", root, name);
    if (length < 0 || (size_t)length >= sizeof(path)) {
        set_error(error, 0, 0, "PALI asset path is too long");
        return false;
    }
    FILE *file = fopen(path, "rb");
    if (file == NULL) {
        char message[PALI_ERROR_CAP];
        (void)snprintf(message, sizeof(message), "could not open %.100s", path);
        set_error(error, 0, 0, message);
        return false;
    }
    const size_t bytes = fread(out, 1, PALI_SOURCE_CAP - 1, file);
    bool io_failed = ferror(file) != 0;
    int extra = EOF;
    if (!io_failed) {
        extra = fgetc(file);
        io_failed = ferror(file) != 0;
    }
    (void)fclose(file);
    if (io_failed || extra != EOF) {
        set_error(error, 0, 0,
                  "PALI source could not be read or exceeds its fixed buffer");
        return false;
    }
    out[bytes] = '\0';
    return true;
}

static bool load_prototypes(World *world, const char *asset_root,
                            PaliError *error) {
    for (int index = 0; index < PROTOTYPE_COUNT; ++index) {
        PrototypeDefinition *definition = &world->universe.prototypes[index];
        (void)snprintf(definition->name, sizeof(definition->name), "%s",
                       PROTOTYPE_NAMES[index]);
        if (!read_source_file(asset_root, PROTOTYPE_NAMES[index],
                              definition->default_source, error)) {
            return false;
        }
        if (!pali_parse_document(definition->default_source,
                                 &definition->document, error) ||
            !pali_compile_document(&definition->document,
                                   &definition->program, error)) {
            return false;
        }
        if (strcmp(definition->document.prototype_name,
                   PROTOTYPE_NAMES[index]) != 0) {
            set_error(error, 1, 1,
                      "PALI filename and prototype declaration disagree");
            return false;
        }
        if (!pali_format_document(&definition->document,
                                  definition->default_source,
                                  sizeof(definition->default_source), error)) {
            return false;
        }
        (void)snprintf(definition->current_source,
                       sizeof(definition->current_source), "%s",
                       definition->default_source);
    }
    return true;
}

bool world_tile_is_blocking(uint8_t tile) {
    return tile == TILE_THICKET || tile == TILE_WATER;
}

static void generate_tiles(World *world) {
    DeterministicRng terrain =
        rng_stream(world->universe.root_seed, UINT64_C(0x5445525241494e));
    const int center_x = WORLD_MAP_WIDTH / 2;
    const int center_y = WORLD_MAP_HEIGHT / 2;
    for (int y = 0; y < WORLD_MAP_HEIGHT; ++y) {
        for (int x = 0; x < WORLD_MAP_WIDTH; ++x) {
            const bool border = x < 2 || y < 2 || x >= WORLD_MAP_WIDTH - 2 ||
                                y >= WORLD_MAP_HEIGHT - 2;
            const bool clearing = abs(x - center_x) <= 5 &&
                                  abs(y - center_y) <= 4;
            const int roll = rng_range(&terrain, 100);
            uint8_t tile = TILE_GRASS_DARK;
            if (border) {
                tile = TILE_THICKET;
            } else if (!clearing && roll < 5) {
                tile = TILE_WATER;
            } else if (!clearing && roll < 10) {
                tile = TILE_THICKET;
            } else if (roll < 20) {
                tile = TILE_FLOWERS;
            } else if (roll < 60) {
                tile = TILE_GRASS_LIGHT;
            }
            world->universe.tiles[y][x] = tile;
        }
    }
}

static bool entity_is_blocking(const Entity *entity) {
    return entity->active &&
           (entity->prototype == PROTOTYPE_STONE ||
            entity->prototype == PROTOTYPE_TREE);
}

static bool position_has_entity(const World *world, float x, float y,
                                float distance) {
    const float limit = distance * distance;
    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        const Entity *entity = &world->universe.entities[index];
        const float dx = entity->x - x;
        const float dy = entity->y - y;
        if (entity->active && dx * dx + dy * dy < limit) {
            return true;
        }
    }
    return false;
}

static uint64_t entity_id(uint64_t seed, uint16_t serial,
                          PrototypeId prototype) {
    uint64_t id = mix64(seed ^ ((uint64_t)serial << 16) ^
                        ((uint64_t)prototype << 48) ^
                        UINT64_C(0x454e54495459));
    return id == 0 ? 1 : id;
}

static Entity *add_entity(World *world, PrototypeId prototype, float x,
                          float y) {
    if (world->universe.entity_count >= WORLD_MAX_ENTITIES) {
        return NULL;
    }
    const uint16_t serial = world->universe.entity_count;
    Entity *entity = &world->universe.entities[serial];
    memset(entity, 0, sizeof(*entity));
    entity->id = entity_id(world->universe.root_seed, serial, prototype);
    entity->prototype = (uint8_t)prototype;
    entity->x = x;
    entity->y = y;
    entity->active = true;
    entity->local_override = -1;
    entity->rng_state = mix64(entity->id ^ UINT64_C(0x4352454154555245));
    world->universe.entity_count++;
    return entity;
}

static bool find_spawn_position(World *world, DeterministicRng *rng,
                                bool keep_from_center, float *out_x,
                                float *out_y) {
    const float center_x = (float)(WORLD_MAP_WIDTH * WORLD_TILE_SIZE) * 0.5f;
    const float center_y = (float)(WORLD_MAP_HEIGHT * WORLD_TILE_SIZE) * 0.5f;
    for (int attempt = 0; attempt < 256; ++attempt) {
        const int tile_x = 2 + rng_range(rng, WORLD_MAP_WIDTH - 4);
        const int tile_y = 2 + rng_range(rng, WORLD_MAP_HEIGHT - 4);
        const float x = ((float)tile_x + 0.5f) * (float)WORLD_TILE_SIZE;
        const float y = ((float)tile_y + 0.5f) * (float)WORLD_TILE_SIZE;
        const float dx = x - center_x;
        const float dy = y - center_y;
        if (!world_tile_is_blocking(world->universe.tiles[tile_y][tile_x]) &&
            (!keep_from_center || dx * dx + dy * dy > 34.0f * 34.0f) &&
            !position_has_entity(world, x, y, 7.0f)) {
            *out_x = x;
            *out_y = y;
            return true;
        }
    }
    return false;
}

static bool spawn_group(World *world, DeterministicRng *rng,
                        PrototypeId prototype, int count,
                        bool keep_from_center) {
    for (int index = 0; index < count; ++index) {
        float x = 0.0f;
        float y = 0.0f;
        if (!find_spawn_position(world, rng, keep_from_center, &x, &y) ||
            add_entity(world, prototype, x, y) == NULL) {
            return false;
        }
    }
    return true;
}

static bool generate_entities(World *world) {
    DeterministicRng objects =
        rng_stream(world->universe.root_seed, UINT64_C(0x4f424a45435453));
    const float center_x = (float)(WORLD_MAP_WIDTH * WORLD_TILE_SIZE) * 0.5f;
    const float center_y = (float)(WORLD_MAP_HEIGHT * WORLD_TILE_SIZE) * 0.5f;

    if (add_entity(world, PROTOTYPE_APPLE, center_x + 16.0f, center_y) == NULL ||
        add_entity(world, PROTOTYPE_FIRE, center_x - 24.0f, center_y + 8.0f) ==
            NULL ||
        add_entity(world, PROTOTYPE_MOTH, center_x, center_y - 18.0f) == NULL) {
        return false;
    }
    return spawn_group(world, &objects, PROTOTYPE_TREE, 12, true) &&
           spawn_group(world, &objects, PROTOTYPE_STONE, 8, true) &&
           spawn_group(world, &objects, PROTOTYPE_APPLE, 6, false) &&
           spawn_group(world, &objects, PROTOTYPE_FIRE, 2, false);
}

bool world_init(World *world, uint64_t seed, const char *pali_asset_root,
                PaliError *error) {
    if (world == NULL) {
        set_error(error, 0, 0, "world storage is null");
        return false;
    }
    memset(world, 0, sizeof(*world));
    if (error != NULL) {
        memset(error, 0, sizeof(*error));
    }
    world->universe.root_seed = seed;
    if (!load_prototypes(world, pali_asset_root, error)) {
        return false;
    }
    generate_tiles(world);
    if (!generate_entities(world)) {
        set_error(error, 0, 0, "deterministic entity capacity exhausted");
        return false;
    }

    world->knowledge.perceived_concepts =
        concept_bit(CONCEPT_TAG) | concept_bit(CONCEPT_MASS) |
        concept_bit(CONCEPT_NUTRITION) | concept_bit(CONCEPT_COLOR) |
        concept_bit(CONCEPT_RIPE);
    world->knowledge.readable_concepts =
        concept_bit(CONCEPT_TAG) | concept_bit(CONCEPT_NUTRITION) |
        concept_bit(CONCEPT_COLOR) | concept_bit(CONCEPT_RIPE);
    world->knowledge.patchable_concepts = concept_bit(CONCEPT_NUTRITION);
    world->knowledge.known_notations = 0;
    world->knowledge.reach_mask = patch_reach_bit(PATCH_REACH_ENTITY);
    world->knowledge.access_depth = (uint8_t)ACCESS_DEPTH_STATE;
    world->embodiment.entity_id =
        mix64(seed ^ UINT64_C(0x454d424f44494d45));
    world->embodiment.x =
        (float)(WORLD_MAP_WIDTH * WORLD_TILE_SIZE) * 0.5f;
    world->embodiment.y =
        (float)(WORLD_MAP_HEIGHT * WORLD_TILE_SIZE) * 0.5f;
    world->embodiment.hunger = 36.0f;
    world->embodiment.warmth = 72.0f;
    (void)snprintf(world->message, sizeof(world->message),
                   "Click an entity or press E to open its Lens. F invokes use.");
    return true;
}

static bool point_hits_blocking_tile(const World *world, float x, float y) {
    const int tile_x = (int)floorf(x / (float)WORLD_TILE_SIZE);
    const int tile_y = (int)floorf(y / (float)WORLD_TILE_SIZE);
    if (tile_x < 0 || tile_y < 0 || tile_x >= WORLD_MAP_WIDTH ||
        tile_y >= WORLD_MAP_HEIGHT) {
        return true;
    }
    return world_tile_is_blocking(world->universe.tiles[tile_y][tile_x]);
}

static bool position_blocked(const World *world, float x, float y,
                             float radius, uint64_t ignore_id) {
    if (point_hits_blocking_tile(world, x - radius, y - radius) ||
        point_hits_blocking_tile(world, x + radius, y - radius) ||
        point_hits_blocking_tile(world, x - radius, y + radius) ||
        point_hits_blocking_tile(world, x + radius, y + radius)) {
        return true;
    }
    const float collision = radius + 3.0f;
    const float limit = collision * collision;
    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        const Entity *entity = &world->universe.entities[index];
        if (entity->id == ignore_id || !entity_is_blocking(entity)) {
            continue;
        }
        const float dx = entity->x - x;
        const float dy = entity->y - y;
        if (dx * dx + dy * dy < limit) {
            return true;
        }
    }
    return false;
}

static void move_with_collision(World *world, float *x, float *y, float dx,
                                float dy, float radius, uint64_t ignore_id) {
    if (!position_blocked(world, *x + dx, *y, radius, ignore_id)) {
        *x += dx;
    }
    if (!position_blocked(world, *x, *y + dy, radius, ignore_id)) {
        *y += dy;
    }
}

const PaliProgram *world_entity_program(const World *world,
                                        const Entity *entity) {
    if (world == NULL || entity == NULL ||
        entity->prototype >= PROTOTYPE_COUNT) {
        return NULL;
    }
    return &world->universe.prototypes[entity->prototype].program;
}

static const LocalOverride *entity_override_const(const World *world,
                                                  const Entity *entity) {
    if (world == NULL || entity == NULL || entity->local_override < 0 ||
        entity->local_override >= WORLD_MAX_LOCAL_OVERRIDES) {
        return NULL;
    }
    const LocalOverride *override =
        &world->universe.local_overrides[entity->local_override];
    if (!override->active || override->entity_id != entity->id) {
        return NULL;
    }
    return override;
}

static const LocalPatchValue *override_value(const LocalOverride *override,
                                             ConceptId concept) {
    if (override == NULL) {
        return NULL;
    }
    for (uint8_t index = 0; index < override->value_count; ++index) {
        if (override->values[index].concept == concept) {
            return &override->values[index];
        }
    }
    return NULL;
}

bool world_get_entity_property(const World *world, const Entity *entity,
                               const char *name, PaliValue *out) {
    if (world == NULL || entity == NULL || name == NULL || out == NULL) {
        return false;
    }
    for (uint8_t index = 0; index < entity->state_count; ++index) {
        if (strcmp(entity->state[index].name, name) == 0) {
            *out = entity->state[index].value;
            return true;
        }
    }
    const ConceptDefinition *concept = lexicon_find_by_name(name);
    if (concept != NULL) {
        const LocalPatchValue *local =
            override_value(entity_override_const(world, entity), concept->id);
        if (local != NULL) {
            *out = local->value;
            return true;
        }
    }
    const PaliProgram *program = world_entity_program(world, entity);
    const PaliValue *value = pali_program_property(program, name);
    if (value == NULL) {
        return false;
    }
    *out = *value;
    return true;
}

bool world_get_entity_concept(const World *world, const Entity *entity,
                              ConceptId concept, PaliValue *out) {
    const ConceptDefinition *definition = lexicon_find_by_id(concept);
    return definition != NULL &&
           world_get_entity_property(world, entity, definition->name, out);
}

ConceptAccess world_concept_access(const World *world, ConceptId concept) {
    if (world == NULL || lexicon_find_by_id(concept) == NULL) {
        return CONCEPT_ACCESS_UNPERCEIVED;
    }
    const uint64_t bit = concept_bit(concept);
    if ((world->knowledge.perceived_concepts & bit) == 0) {
        return CONCEPT_ACCESS_UNPERCEIVED;
    }
    if ((world->knowledge.readable_concepts & bit) == 0) {
        return CONCEPT_ACCESS_VEILED;
    }
    if ((world->knowledge.patchable_concepts & bit) == 0) {
        return CONCEPT_ACCESS_READABLE;
    }
    return CONCEPT_ACCESS_PATCHABLE;
}

bool world_has_reach(const World *world, PatchReach reach) {
    return world != NULL &&
           (world->knowledge.reach_mask & patch_reach_bit(reach)) != 0;
}

void world_grant_developer_knowledge(World *world) {
    if (world == NULL) {
        return;
    }
    uint64_t all_concepts = 0;
    for (ConceptId concept = CONCEPT_TAG; concept < CONCEPT_COUNT; ++concept) {
        all_concepts |= concept_bit(concept);
    }
    world->knowledge.perceived_concepts = all_concepts;
    world->knowledge.readable_concepts = all_concepts;
    world->knowledge.patchable_concepts = all_concepts;
    world->knowledge.access_depth = (uint8_t)ACCESS_DEPTH_LAW;
    world->knowledge.reach_mask = 0;
    for (int reach = 0; reach < PATCH_REACH_COUNT; ++reach) {
        world->knowledge.reach_mask |= patch_reach_bit((PatchReach)reach);
    }
}

static double entity_numeric_property(const World *world, const Entity *entity,
                                      const char *name, double fallback) {
    PaliValue value;
    if (world_get_entity_property(world, entity, name, &value) &&
        value.type == PALI_VALUE_NUMBER) {
        return value.as.number;
    }
    return fallback;
}

static uint64_t entity_random(Entity *entity) {
    entity->rng_state += UINT64_C(0x9e3779b97f4a7c15);
    return mix64(entity->rng_state);
}

static void step_moth(World *world, Entity *entity) {
    if (!entity->active || entity->prototype != PROTOTYPE_MOTH) {
        return;
    }
    if (entity->direction_ticks == 0) {
        static const float DIRECTIONS[8][2] = {
            {1.0f, 0.0f},  {0.7f, 0.7f},  {0.0f, 1.0f}, {-0.7f, 0.7f},
            {-1.0f, 0.0f}, {-0.7f, -0.7f}, {0.0f, -1.0f}, {0.7f, -0.7f}};
        const int direction = (int)(entity_random(entity) % UINT64_C(8));
        entity->move_x = DIRECTIONS[direction][0];
        entity->move_y = DIRECTIONS[direction][1];
        entity->direction_ticks =
            (uint16_t)(35 + (entity_random(entity) % UINT64_C(70)));
    } else {
        entity->direction_ticks--;
    }
    const float previous_x = entity->x;
    const float previous_y = entity->y;
    move_with_collision(world, &entity->x, &entity->y,
                        entity->move_x * 0.18f, entity->move_y * 0.18f, 2.0f,
                        entity->id);
    if (entity->x == previous_x && entity->y == previous_y) {
        entity->direction_ticks = 0;
    }
    entity->dirty = true;
}

static float clamp_stat(float value) {
    if (value < 0.0f) {
        return 0.0f;
    }
    if (value > 100.0f) {
        return 100.0f;
    }
    return value;
}

static float clamp_number_to_stat(double value) {
    if (value <= 0.0) {
        return 0.0f;
    }
    if (value >= 100.0) {
        return 100.0f;
    }
    return (float)value;
}

void world_step(World *world, WorldInput input) {
    if (world == NULL) {
        return;
    }
    if (!isfinite(input.move_x) || !isfinite(input.move_y)) {
        input.move_x = 0.0f;
        input.move_y = 0.0f;
    }
    float length = sqrtf(input.move_x * input.move_x +
                         input.move_y * input.move_y);
    if (length > 1.0f) {
        input.move_x /= length;
        input.move_y /= length;
    }
    move_with_collision(world, &world->embodiment.x, &world->embodiment.y,
                        input.move_x * 0.62f, input.move_y * 0.62f, 2.5f, 0);

    world->embodiment.hunger =
        clamp_stat(world->embodiment.hunger + 0.0025f);
    world->embodiment.warmth =
        clamp_stat(world->embodiment.warmth - 0.0030f);

    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        Entity *entity = &world->universe.entities[index];
        if (entity->active && entity->prototype == PROTOTYPE_FIRE) {
            const float dx = entity->x - world->embodiment.x;
            const float dy = entity->y - world->embodiment.y;
            const double heat = entity_numeric_property(world, entity, "heat", 0.0);
            const double bounded_heat = fmax(0.0, fmin(100.0, heat));
            const float radius = (float)(bounded_heat + 10.0);
            if (dx * dx + dy * dy <= radius * radius) {
                world->embodiment.warmth = clamp_stat(
                    world->embodiment.warmth +
                    (float)bounded_heat * 0.00020f);
            }
        }
        step_moth(world, entity);
    }
    world->universe.tick++;
}

int world_nearest_entity(const World *world, float maximum_distance) {
    if (world == NULL) {
        return -1;
    }
    const float limit = maximum_distance * maximum_distance;
    float best = limit;
    int result = -1;
    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        const Entity *entity = &world->universe.entities[index];
        if (!entity->active) {
            continue;
        }
        const float dx = entity->x - world->embodiment.x;
        const float dy = entity->y - world->embodiment.y;
        const float distance = dx * dx + dy * dy;
        if (distance < best ||
            (distance == best && result >= 0 &&
             entity->id < world->universe.entities[result].id)) {
            best = distance;
            result = (int)index;
        }
    }
    return result;
}

Entity *world_entity_by_id(World *world, uint64_t id) {
    if (world == NULL) {
        return NULL;
    }
    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        if (world->universe.entities[index].id == id) {
            return &world->universe.entities[index];
        }
    }
    return NULL;
}

const Entity *world_entity_by_id_const(const World *world, uint64_t id) {
    return world_entity_by_id((World *)(uintptr_t)world, id);
}

static bool host_error(PaliError *error, const char *message) {
    set_error(error, 0, 0, message);
    return false;
}

static bool host_get(void *user, PaliTarget target, const char *name,
                     PaliValue *out, PaliError *error) {
    HostContext *context = user;
    if (target == PALI_TARGET_SELF) {
        if (world_get_entity_property(context->world, context->self, name, out)) {
            return true;
        }
        return host_error(error, "self property does not exist");
    }
    if (strcmp(name, "hunger") == 0) {
        *out = pali_number((double)context->world->embodiment.hunger);
        return true;
    }
    if (strcmp(name, "warmth") == 0) {
        *out = pali_number((double)context->world->embodiment.warmth);
        return true;
    }
    if (strcmp(name, "x") == 0) {
        *out = pali_number((double)context->world->embodiment.x);
        return true;
    }
    if (strcmp(name, "y") == 0) {
        *out = pali_number((double)context->world->embodiment.y);
        return true;
    }
    return host_error(error, "actor property is not exposed");
}

static bool set_instance_property(Entity *entity, const char *name,
                                  PaliValue value, PaliError *error) {
    const ConceptDefinition *concept = lexicon_find_by_name(name);
    if (concept != NULL && !lexicon_value_is_valid(concept, value)) {
        return host_error(error,
                          "self property violates its semantic contract");
    }
    for (uint8_t index = 0; index < entity->state_count; ++index) {
        if (strcmp(entity->state[index].name, name) == 0) {
            entity->state[index].value = value;
            entity->dirty = true;
            return true;
        }
    }
    if (entity->state_count >= WORLD_INSTANCE_PROPERTIES) {
        return host_error(error, "self has no remaining state property slots");
    }
    PaliProperty *property = &entity->state[entity->state_count++];
    (void)snprintf(property->name, sizeof(property->name), "%s", name);
    property->value = value;
    entity->dirty = true;
    return true;
}

static bool host_set(void *user, PaliTarget target, const char *name,
                     PaliValue value, PaliError *error) {
    HostContext *context = user;
    if (target == PALI_TARGET_SELF) {
        return set_instance_property(context->self, name, value, error);
    }
    if (value.type != PALI_VALUE_NUMBER || !isfinite(value.as.number)) {
        return host_error(error, "actor physical properties require a number");
    }
    if (strcmp(name, "hunger") == 0) {
        context->world->embodiment.hunger =
            clamp_number_to_stat(value.as.number);
        return true;
    }
    if (strcmp(name, "warmth") == 0) {
        context->world->embodiment.warmth =
            clamp_number_to_stat(value.as.number);
        return true;
    }
    return host_error(error, "actor property is read-only or unknown");
}

static bool host_call(void *user, PaliHostCall call,
                      const PaliValue *argument, PaliError *error) {
    HostContext *context = user;
    if (call == PALI_HOST_DESTROY) {
        context->self->active = false;
        context->self->dirty = true;
        return true;
    }
    if (call == PALI_HOST_MESSAGE) {
        if (argument == NULL || argument->type != PALI_VALUE_TEXT) {
            return host_error(error, "message(...) requires text");
        }
        (void)snprintf(context->world->message,
                       sizeof(context->world->message), "%s",
                       argument->as.text);
        return true;
    }
    return host_error(error, "host call is not whitelisted");
}

bool world_use_entity(World *world, int entity_index, PaliError *error) {
    if (world == NULL || entity_index < 0 ||
        entity_index >= (int)world->universe.entity_count) {
        set_error(error, 0, 0, "no usable entity selected");
        return false;
    }
    Entity *entity = &world->universe.entities[entity_index];
    if (!entity->active) {
        set_error(error, 0, 0, "entity no longer exists");
        return false;
    }
    HostContext context;
    context.world = world;
    context.self = entity;
    context.program = world_entity_program(world, entity);
    PaliHost host;
    host.user = &context;
    host.get_property = host_get;
    host.set_property = host_set;
    host.call = host_call;
    if (!pali_run_use(context.program, &host, PALI_DEFAULT_BUDGET, error)) {
        char detail[WORLD_MESSAGE_CAP];
        (void)snprintf(detail, sizeof(detail), "Anomaly L%d: %.120s",
                       error != NULL ? error->line : 0,
                       error != NULL ? error->message : "runtime failure");
        (void)snprintf(world->message, sizeof(world->message), "%s", detail);
        return false;
    }
    return true;
}

bool world_apply_prototype_source(World *world, PrototypeId prototype,
                                  const char *source, PaliError *error) {
    if (world == NULL || prototype < 0 || prototype >= PROTOTYPE_COUNT) {
        set_error(error, 0, 0, "invalid prototype target");
        return false;
    }
    PaliDocument candidate_document;
    PaliProgram candidate;
    char normalized[PALI_SOURCE_CAP];
    if (!pali_parse_document(source, &candidate_document, error) ||
        !pali_compile_document(&candidate_document, &candidate, error) ||
        !pali_format_document(&candidate_document, normalized,
                              sizeof(normalized), error)) {
        return false;
    }
    PrototypeDefinition *definition = &world->universe.prototypes[prototype];
    if (strcmp(candidate.prototype_name, definition->name) != 0) {
        set_error(error, 1, 1,
                  "patch cannot rename the selected prototype");
        return false;
    }
    for (uint16_t property = 0;
         property < candidate_document.property_count; ++property) {
        const PaliProperty *candidate_property =
            &candidate_document.properties[property];
        const ConceptDefinition *concept =
            lexicon_find_by_name(candidate_property->name);
        if (concept != NULL &&
            !lexicon_value_is_valid(concept, candidate_property->value)) {
            set_error(error, 1, 1,
                      "prototype property violates its semantic contract");
            return false;
        }
    }
    for (int slot = 0; slot < WORLD_MAX_LOCAL_OVERRIDES; ++slot) {
        const LocalOverride *override =
            &world->universe.local_overrides[slot];
        if (!override->active) {
            continue;
        }
        const Entity *entity =
            world_entity_by_id_const(world, override->entity_id);
        if (entity == NULL || entity->prototype != (uint8_t)prototype) {
            continue;
        }
        for (uint8_t index = 0; index < override->value_count; ++index) {
            const ConceptDefinition *concept =
                lexicon_find_by_id(override->values[index].concept);
            const PaliValue *broader =
                concept != NULL
                    ? pali_document_property(&candidate_document, concept->name)
                    : NULL;
            if (concept == NULL || broader == NULL ||
                broader->type != override->values[index].value.type) {
                set_error(error, 1, 1,
                          "prototype patch conflicts with a narrower Entity Patch");
                return false;
            }
        }
    }
    definition->document = candidate_document;
    definition->program = candidate;
    (void)snprintf(definition->current_source,
                   sizeof(definition->current_source), "%s", normalized);
    definition->patched =
        strcmp(definition->current_source, definition->default_source) != 0;
    (void)snprintf(world->message, sizeof(world->message),
                   "Shared prototype '%s' compiled.", definition->name);
    return true;
}

static int available_override_slot(const World *world) {
    for (int index = 0; index < WORLD_MAX_LOCAL_OVERRIDES; ++index) {
        if (!world->universe.local_overrides[index].active) {
            return index;
        }
    }
    return -1;
}

static int local_value_index(const LocalOverride *override,
                             ConceptId concept) {
    if (override == NULL) {
        return -1;
    }
    for (uint8_t index = 0; index < override->value_count; ++index) {
        if (override->values[index].concept == concept) {
            return (int)index;
        }
    }
    return -1;
}

static PaliProperty *document_property(PaliDocument *document,
                                       const char *name) {
    if (document == NULL || name == NULL) {
        return NULL;
    }
    for (uint16_t index = 0; index < document->property_count; ++index) {
        if (strcmp(document->properties[index].name, name) == 0) {
            return &document->properties[index];
        }
    }
    return NULL;
}

static bool patch_permission(const World *world, ConceptId concept,
                             PatchReach reach, PaliError *error) {
    if (world_concept_access(world, concept) != CONCEPT_ACCESS_PATCHABLE) {
        set_error(error, 0, 0, "Knowledge cannot Patch this concept");
        return false;
    }
    if (!world_has_reach(world, reach)) {
        set_error(error, 0, 0, "Knowledge does not have the required Reach");
        return false;
    }
    return true;
}

bool world_clear_entity_value_patch(World *world, uint64_t entity_id,
                                    ConceptId concept, PaliError *error) {
    Entity *entity = world_entity_by_id(world, entity_id);
    if (entity == NULL) {
        set_error(error, 0, 0, "Entity Patch target does not exist");
        return false;
    }
    if (!patch_permission(world, concept, PATCH_REACH_ENTITY, error)) {
        return false;
    }
    if (entity->local_override < 0 ||
        entity->local_override >= WORLD_MAX_LOCAL_OVERRIDES) {
        return true;
    }
    LocalOverride *override =
        &world->universe.local_overrides[entity->local_override];
    if (!override->active || override->entity_id != entity_id) {
        set_error(error, 0, 0, "Entity Patch provenance is inconsistent");
        return false;
    }
    const int value_index = local_value_index(override, concept);
    if (value_index < 0) {
        return true;
    }
    for (uint8_t index = (uint8_t)value_index;
         index + 1u < override->value_count; ++index) {
        override->values[index] = override->values[index + 1u];
    }
    override->value_count--;
    memset(&override->values[override->value_count], 0,
           sizeof(override->values[override->value_count]));
    if (override->value_count == 0) {
        memset(override, 0, sizeof(*override));
        entity->local_override = -1;
    }
    entity->dirty = true;
    (void)snprintf(world->message, sizeof(world->message),
                   "Entity Patch removed.");
    return true;
}

bool world_apply_entity_value_patch(World *world, uint64_t entity_id,
                                    ConceptId concept_id, PaliValue value,
                                    PaliError *error) {
    Entity *entity = world_entity_by_id(world, entity_id);
    const ConceptDefinition *concept = lexicon_find_by_id(concept_id);
    if (entity == NULL || !entity->active) {
        set_error(error, 0, 0, "Entity Patch target does not exist");
        return false;
    }
    if (concept == NULL ||
        (concept->operation_flags & CONCEPT_OP_REPLACE) == 0 ||
        !lexicon_value_is_valid(concept, value)) {
        set_error(error, 0, 0,
                  "Patch value violates the concept's semantic contract");
        return false;
    }
    if (!patch_permission(world, concept_id, PATCH_REACH_ENTITY, error)) {
        return false;
    }
    PrototypeDefinition *prototype =
        &world->universe.prototypes[entity->prototype];
    PaliProperty *broader = document_property(&prototype->document,
                                              concept->name);
    if (broader == NULL || broader->value.type != value.type) {
        set_error(error, 0, 0,
                  "Entity does not expose this concept at the required type");
        return false;
    }
    if (pali_value_equal(broader->value, value)) {
        return world_clear_entity_value_patch(world, entity_id, concept_id,
                                              error);
    }

    PaliDocument resolved = prototype->document;
    PaliProperty *resolved_property = document_property(&resolved,
                                                        concept->name);
    resolved_property->value = value;
    PaliProgram validation;
    if (!pali_compile_document(&resolved, &validation, error)) {
        return false;
    }

    int slot = entity->local_override;
    if (slot < 0) {
        slot = available_override_slot(world);
    }
    if (slot < 0 || slot >= WORLD_MAX_LOCAL_OVERRIDES) {
        set_error(error, 0, 0, "Entity Patch capacity reached");
        return false;
    }
    LocalOverride candidate;
    if (entity->local_override >= 0) {
        candidate = world->universe.local_overrides[slot];
        if (!candidate.active || candidate.entity_id != entity_id) {
            set_error(error, 0, 0, "Entity Patch provenance is inconsistent");
            return false;
        }
    } else {
        memset(&candidate, 0, sizeof(candidate));
        candidate.active = true;
        candidate.entity_id = entity_id;
    }
    int value_index = local_value_index(&candidate, concept_id);
    if (value_index < 0) {
        if (candidate.value_count >= WORLD_LOCAL_PATCH_VALUES) {
            set_error(error, 0, 0, "Entity has no remaining Patch value slots");
            return false;
        }
        value_index = (int)candidate.value_count++;
    }
    candidate.values[value_index].concept = concept_id;
    candidate.values[value_index].value = value;
    world->universe.local_overrides[slot] = candidate;
    entity->local_override = (int8_t)slot;
    entity->dirty = true;
    (void)snprintf(world->message, sizeof(world->message),
                   "This %s now carries a local %s Scar.",
                   world_prototype_name((PrototypeId)entity->prototype),
                   concept->name);
    return true;
}

bool world_apply_prototype_value_patch(World *world, PrototypeId prototype,
                                       ConceptId concept_id, PaliValue value,
                                       PaliError *error) {
    if (world == NULL || prototype < 0 || prototype >= PROTOTYPE_COUNT) {
        set_error(error, 0, 0, "invalid Prototype Patch target");
        return false;
    }
    const ConceptDefinition *concept = lexicon_find_by_id(concept_id);
    if (concept == NULL ||
        (concept->operation_flags & CONCEPT_OP_REPLACE) == 0 ||
        !lexicon_value_is_valid(concept, value)) {
        set_error(error, 0, 0,
                  "Patch value violates the concept's semantic contract");
        return false;
    }
    if (!patch_permission(world, concept_id, PATCH_REACH_PROTOTYPE, error)) {
        return false;
    }
    PaliDocument candidate = world->universe.prototypes[prototype].document;
    PaliProperty *property = document_property(&candidate, concept->name);
    if (property == NULL || property->value.type != value.type) {
        set_error(error, 0, 0,
                  "Prototype does not expose this concept at the required type");
        return false;
    }
    property->value = value;
    char normalized[PALI_SOURCE_CAP];
    if (!pali_format_document(&candidate, normalized, sizeof(normalized),
                              error)) {
        return false;
    }
    return world_apply_prototype_source(world, prototype, normalized, error);
}

const char *world_prototype_source(const World *world, PrototypeId prototype) {
    if (world == NULL || prototype < 0 || prototype >= PROTOTYPE_COUNT) {
        return "";
    }
    return world->universe.prototypes[prototype].current_source;
}

const PaliDocument *world_prototype_document(const World *world,
                                             PrototypeId prototype) {
    if (world == NULL || prototype < 0 || prototype >= PROTOTYPE_COUNT) {
        return NULL;
    }
    return &world->universe.prototypes[prototype].document;
}

static uint64_t hash_bytes(uint64_t hash, const void *data, size_t length) {
    const unsigned char *bytes = data;
    for (size_t index = 0; index < length; ++index) {
        hash ^= bytes[index];
        hash *= UINT64_C(1099511628211);
    }
    return hash;
}

static uint64_t hash_float(uint64_t hash, float value) {
    uint32_t bits = 0;
    memcpy(&bits, &value, sizeof(bits));
    return hash_bytes(hash, &bits, sizeof(bits));
}

static uint64_t hash_value(uint64_t hash, PaliValue value) {
    const uint8_t type = (uint8_t)value.type;
    hash = hash_bytes(hash, &type, sizeof(type));
    switch (value.type) {
        case PALI_VALUE_NIL:
            break;
        case PALI_VALUE_NUMBER: {
            uint64_t bits = 0;
            memcpy(&bits, &value.as.number, sizeof(bits));
            hash = hash_bytes(hash, &bits, sizeof(bits));
            break;
        }
        case PALI_VALUE_BOOL: {
            const uint8_t boolean = value.as.boolean ? 1u : 0u;
            hash = hash_bytes(hash, &boolean, sizeof(boolean));
            break;
        }
        case PALI_VALUE_TEXT:
            hash = hash_bytes(hash, value.as.text, strlen(value.as.text));
            break;
        default:
            break;
    }
    return hash;
}

uint64_t world_genesis_fingerprint(const World *world) {
    if (world == NULL) {
        return 0;
    }
    uint64_t hash = UINT64_C(1469598103934665603);
    hash = hash_bytes(hash, &world->universe.root_seed,
                      sizeof(world->universe.root_seed));
    hash = hash_bytes(hash, world->universe.tiles,
                      sizeof(world->universe.tiles));
    hash = hash_bytes(hash, &world->universe.entity_count,
                      sizeof(world->universe.entity_count));
    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        const Entity *entity = &world->universe.entities[index];
        hash = hash_bytes(hash, &entity->id, sizeof(entity->id));
        hash = hash_bytes(hash, &entity->prototype, sizeof(entity->prototype));
        hash = hash_float(hash, entity->x);
        hash = hash_float(hash, entity->y);
    }
    return hash;
}

uint64_t world_state_fingerprint(const World *world) {
    if (world == NULL) {
        return 0;
    }
    uint64_t hash = world_genesis_fingerprint(world);
    hash = hash_bytes(hash, &world->universe.tick, sizeof(world->universe.tick));
    hash = hash_bytes(hash, &world->knowledge.perceived_concepts,
                      sizeof(world->knowledge.perceived_concepts));
    hash = hash_bytes(hash, &world->knowledge.readable_concepts,
                      sizeof(world->knowledge.readable_concepts));
    hash = hash_bytes(hash, &world->knowledge.patchable_concepts,
                      sizeof(world->knowledge.patchable_concepts));
    hash = hash_bytes(hash, &world->knowledge.known_notations,
                      sizeof(world->knowledge.known_notations));
    hash = hash_bytes(hash, &world->knowledge.reach_mask,
                      sizeof(world->knowledge.reach_mask));
    hash = hash_bytes(hash, &world->knowledge.access_depth,
                      sizeof(world->knowledge.access_depth));
    hash = hash_bytes(hash, &world->embodiment.entity_id,
                      sizeof(world->embodiment.entity_id));
    hash = hash_float(hash, world->embodiment.x);
    hash = hash_float(hash, world->embodiment.y);
    hash = hash_float(hash, world->embodiment.hunger);
    hash = hash_float(hash, world->embodiment.warmth);
    for (int index = 0; index < PROTOTYPE_COUNT; ++index) {
        hash = hash_bytes(hash,
                          world->universe.prototypes[index].current_source,
                          strlen(world->universe.prototypes[index].current_source));
    }
    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        const Entity *entity = &world->universe.entities[index];
        hash = hash_bytes(hash, &entity->active, sizeof(entity->active));
        hash = hash_bytes(hash, &entity->dirty, sizeof(entity->dirty));
        hash = hash_bytes(hash, &entity->rng_state, sizeof(entity->rng_state));
        hash = hash_float(hash, entity->x);
        hash = hash_float(hash, entity->y);
        hash = hash_float(hash, entity->move_x);
        hash = hash_float(hash, entity->move_y);
        hash = hash_bytes(hash, &entity->direction_ticks,
                          sizeof(entity->direction_ticks));
        hash = hash_bytes(hash, &entity->state_count,
                          sizeof(entity->state_count));
        for (uint8_t property = 0; property < entity->state_count; ++property) {
            hash = hash_bytes(hash, entity->state[property].name,
                              strlen(entity->state[property].name));
            hash = hash_value(hash, entity->state[property].value);
        }
        const LocalOverride *override = entity_override_const(world, entity);
        const uint8_t has_override = override != NULL ? 1u : 0u;
        hash = hash_bytes(hash, &has_override, sizeof(has_override));
        if (override != NULL) {
            hash = hash_bytes(hash, &override->value_count,
                              sizeof(override->value_count));
            for (uint8_t value = 0; value < override->value_count; ++value) {
                hash = hash_bytes(hash, &override->values[value].concept,
                                  sizeof(override->values[value].concept));
                hash = hash_value(hash, override->values[value].value);
            }
        }
    }
    return hash;
}

_Static_assert(sizeof(World) <= 262144,
               "World memory contract exceeds 256 KiB");
