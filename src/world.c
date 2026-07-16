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

static void step_tree_fruit(World *world, Entity *tree);

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

uint64_t world_descendant_id(const World *world, uint64_t parent_id,
                             uint32_t birth_ordinal,
                             PrototypeId prototype) {
    if (world == NULL || parent_id == 0 || birth_ordinal == 0 ||
        prototype < 0 || prototype >= PROTOTYPE_COUNT) {
        return 0;
    }
    uint64_t id = mix64(world->universe.root_seed ^ mix64(parent_id) ^
                        ((uint64_t)birth_ordinal << 17) ^
                        ((uint64_t)prototype << 48) ^
                        UINT64_C(0x44455343454e4453));
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
    if (prototype == PROTOTYPE_TREE) {
        entity->fruit_ticks =
            (uint16_t)(60u + (uint16_t)(entity->id % UINT64_C(120)));
    }
    world->universe.entity_count++;
    return entity;
}

static void release_entity_override(World *world, Entity *entity) {
    if (world == NULL || entity == NULL || entity->local_override < 0 ||
        entity->local_override >= WORLD_MAX_LOCAL_OVERRIDES) {
        return;
    }
    LocalOverride *override =
        &world->universe.local_overrides[entity->local_override];
    if (override->active && override->entity_id == entity->id) {
        memset(override, 0, sizeof(*override));
    }
    entity->local_override = -1;
}

bool world_restore_descendant(World *world, uint64_t id,
                              uint64_t parent_id,
                              uint32_t birth_ordinal,
                              PrototypeId prototype, PaliError *error) {
    if (world == NULL || id == 0 || prototype != PROTOTYPE_APPLE ||
        id != world_descendant_id(world, parent_id, birth_ordinal,
                                  prototype) ||
        world_entity_by_id_const(world, id) != NULL) {
        set_error(error, 0, 0, "descendant identity is not valid");
        return false;
    }
    const Entity *parent = world_entity_by_id_const(world, parent_id);
    if (parent == NULL || parent->prototype != PROTOTYPE_TREE) {
        set_error(error, 0, 0, "descendant Parentage has no tree");
        return false;
    }
    Entity *entity = NULL;
    Entity *fallback = NULL;
    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        Entity *candidate = &world->universe.entities[index];
        if (!candidate->active && candidate->parent_id != 0) {
            if (fallback == NULL) {
                fallback = candidate;
            }
            if (candidate->local_override >= 0 &&
                candidate->local_override < WORLD_MAX_LOCAL_OVERRIDES) {
                const LocalOverride *override =
                    &world->universe
                         .local_overrides[candidate->local_override];
                if (override->active &&
                    override->entity_id == candidate->id) {
                    entity = candidate;
                    break;
                }
            }
        }
    }
    if (entity == NULL) {
        entity = fallback;
    }
    if (entity != NULL) {
        release_entity_override(world, entity);
    }
    if (entity == NULL) {
        if (world->universe.entity_count >= WORLD_MAX_ENTITIES) {
            set_error(error, 0, 0, "descendant capacity is full");
            return false;
        }
        entity =
            &world->universe.entities[world->universe.entity_count++];
    }
    memset(entity, 0, sizeof(*entity));
    entity->id = id;
    entity->parent_id = parent_id;
    entity->birth_ordinal = birth_ordinal;
    entity->prototype = (uint8_t)prototype;
    entity->x = parent->x;
    entity->y = parent->y;
    entity->active = true;
    entity->dirty = true;
    entity->local_override = -1;
    entity->rng_state = mix64(id ^ UINT64_C(0x4352454154555245));
    return true;
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
    world->knowledge.known_notations =
        UINT32_C(1) << CONCEPT_NUTRITION;
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
    world->embodiment.vigor = 0.0f;
    (void)snprintf(world->message, sizeof(world->message),
                   "Click or E opens. Right-click or F invokes nearby Behavior.");
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

bool world_entity_has_behavior_patch(const World *world,
                                     const Entity *entity) {
    const LocalOverride *override = entity_override_const(world, entity);
    return override != NULL && override->has_behavior;
}

PatchReach world_entity_behavior_provenance(const World *world,
                                            const Entity *entity,
                                            uint64_t *out_id) {
    const LocalOverride *override = entity_override_const(world, entity);
    if (override != NULL && override->has_behavior) {
        if (out_id != NULL) {
            *out_id = override->behavior_provenance_id;
        }
        return (PatchReach)override->behavior_provenance_reach;
    }
    if (out_id != NULL) {
        *out_id = entity != NULL ? (uint64_t)entity->prototype : 0;
    }
    return PATCH_REACH_PROTOTYPE;
}

PatchReach world_entity_concept_provenance(const World *world,
                                           const Entity *entity,
                                           ConceptId concept,
                                           uint64_t *out_id) {
    if (world == NULL || entity == NULL) {
        if (out_id != NULL) {
            *out_id = 0;
        }
        return PATCH_REACH_ENTITY;
    }
    if (concept == CONCEPT_PARENTAGE && entity->parent_id != 0) {
        if (out_id != NULL) {
            *out_id = entity->parent_id;
        }
        return PATCH_REACH_LINEAGE;
    }
    const ConceptDefinition *definition = lexicon_find_by_id(concept);
    if (definition != NULL) {
        for (uint8_t index = 0; index < entity->state_count; ++index) {
            if (strcmp(entity->state[index].name, definition->name) == 0) {
                if (out_id != NULL) {
                    *out_id = entity->id;
                }
                return PATCH_REACH_ENTITY;
            }
        }
    }
    const LocalOverride *override = entity_override_const(world, entity);
    if (override != NULL) {
        for (uint8_t index = 0; index < override->value_count; ++index) {
            if (override->values[index].concept == concept) {
                if (out_id != NULL) {
                    *out_id = override->values[index].provenance_id;
                }
                return (PatchReach)override->values[index].provenance_reach;
            }
        }
    }
    if (out_id != NULL) {
        *out_id = (uint64_t)entity->prototype;
    }
    return PATCH_REACH_PROTOTYPE;
}

static int behavior_draft_index(UseBehaviorDraft draft) {
    if (draft.hunger < 0 || draft.hunger >= BEHAVIOR_HUNGER_COUNT ||
        draft.voice < 0 || draft.voice >= BEHAVIOR_VOICE_COUNT ||
        draft.fate < 0 || draft.fate >= BEHAVIOR_FATE_COUNT ||
        draft.aftertaste < 0 ||
        draft.aftertaste >= BEHAVIOR_AFTERTASTE_COUNT) {
        return -1;
    }
    return (((int)draft.hunger * BEHAVIOR_VOICE_COUNT + (int)draft.voice) *
                BEHAVIOR_FATE_COUNT +
            (int)draft.fate) *
               BEHAVIOR_AFTERTASTE_COUNT +
           (int)draft.aftertaste;
}

bool world_entity_behavior_document(const World *world, const Entity *entity,
                                    PaliDocument *out) {
    if (world == NULL || entity == NULL || out == NULL ||
        entity->prototype >= PROTOTYPE_COUNT) {
        return false;
    }
    const LocalOverride *override = entity_override_const(world, entity);
    if (override != NULL && override->has_behavior) {
        PaliError error;
        return world_build_use_behavior_document(world, entity,
                                                 override->behavior, out,
                                                 &error);
    }
    *out = world->universe.prototypes[entity->prototype].document;
    return true;
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
    if (strcmp(name, "parentage") == 0) {
        if (entity->parent_id == 0) {
            return false;
        }
        char parentage[PALI_TEXT_CAP];
        (void)snprintf(parentage, sizeof(parentage), "tree/%016llx",
                       (unsigned long long)entity->parent_id);
        *out = pali_text(parentage);
        return true;
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

bool world_knows_exact_notation(const World *world, ConceptId concept) {
    if (world == NULL || lexicon_find_by_id(concept) == NULL) {
        return false;
    }
    return (world->knowledge.known_notations &
            (UINT32_C(1) << concept)) != 0;
}

uint8_t world_concept_observation_count(const World *world,
                                        ConceptId concept) {
    if (world == NULL || lexicon_find_by_id(concept) == NULL) {
        return 0;
    }
    const uint32_t observed =
        world->knowledge.observed_prototypes[concept];
    uint8_t count = 0;
    for (int prototype = 0; prototype < PROTOTYPE_COUNT; ++prototype) {
        if ((observed & (UINT32_C(1) << prototype)) != 0) {
            count++;
        }
    }
    return count;
}

ObservationResult world_observe_entity_concept(World *world,
                                               uint64_t entity_id,
                                               ConceptId concept) {
    const ConceptDefinition *const definition =
        lexicon_find_by_id(concept);
    if (world == NULL || definition == NULL ||
        world_concept_access(world, concept) ==
            CONCEPT_ACCESS_UNPERCEIVED) {
        return OBSERVATION_REJECTED;
    }

    const Entity *entity = world_entity_by_id_const(world, entity_id);
    PaliValue value;
    if (entity == NULL || !entity->active ||
        entity->prototype >= PROTOTYPE_COUNT ||
        !world_get_entity_concept(world, entity, concept, &value) ||
        !lexicon_value_is_valid(definition, value)) {
        return OBSERVATION_REJECTED;
    }

    const uint32_t prototype_bit = UINT32_C(1) << entity->prototype;
    uint32_t *const observed =
        &world->knowledge.observed_prototypes[concept];
    if ((*observed & prototype_bit) != 0) {
        return OBSERVATION_REPEATED;
    }
    *observed |= prototype_bit;

    const uint8_t observation_count =
        world_concept_observation_count(world, concept);
    const ConceptAccess access = world_concept_access(world, concept);
    if (observation_count >= 2 && access == CONCEPT_ACCESS_VEILED) {
        world->knowledge.readable_concepts |= concept_bit(concept);
        return OBSERVATION_REVELATION;
    }
    if (observation_count >= 3 &&
        (access == CONCEPT_ACCESS_READABLE ||
         access == CONCEPT_ACCESS_PATCHABLE) &&
        !world_knows_exact_notation(world, concept)) {
        world->knowledge.known_notations |= UINT32_C(1) << concept;
        return OBSERVATION_NOTATION;
    }
    return OBSERVATION_RECORDED;
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
    world->knowledge.known_notations = (uint32_t)all_concepts;
    world->knowledge.access_depth = (uint8_t)ACCESS_DEPTH_LAW;
    world->knowledge.reach_mask = 0;
    for (int reach = 0; reach < PATCH_REACH_COUNT; ++reach) {
        world->knowledge.reach_mask |= patch_reach_bit((PatchReach)reach);
    }
}

InquiryProgress world_inquiry_progress(const World *world,
                                       InquiryId inquiry) {
    InquiryProgress progress = {inquiry, 0, 0};
    if (world == NULL) {
        return progress;
    }
    if (inquiry == INQUIRY_FIRST_SCAR) {
        progress.step_count = 2;
        const ConceptAccess hunger_access =
            world_concept_access(world, CONCEPT_HUNGER);
        if (world->knowledge.access_depth >=
                (uint8_t)ACCESS_DEPTH_BEHAVIOR &&
            (hunger_access == CONCEPT_ACCESS_READABLE ||
             hunger_access == CONCEPT_ACCESS_PATCHABLE)) {
            progress.completed_steps = 2;
            return progress;
        }
        for (int slot = 0; slot < WORLD_MAX_LOCAL_OVERRIDES; ++slot) {
            const LocalOverride *override =
                &world->universe.local_overrides[slot];
            if (!override->active) {
                continue;
            }
            bool changes_nutrition = false;
            for (uint8_t value = 0; value < override->value_count; ++value) {
                if (override->values[value].concept == CONCEPT_NUTRITION &&
                    override->values[value].provenance_reach ==
                        (uint8_t)PATCH_REACH_ENTITY) {
                    changes_nutrition = true;
                    break;
                }
            }
            const Entity *entity = changes_nutrition
                                       ? world_entity_by_id_const(
                                             world, override->entity_id)
                                       : NULL;
            if (entity == NULL || entity->prototype != PROTOTYPE_APPLE) {
                continue;
            }
            if (!entity->active) {
                progress.completed_steps = 2;
                return progress;
            }
            progress.completed_steps = 1;
        }
        return progress;
    }
    if (inquiry == INQUIRY_WEIGHT_OF_THINGS) {
        progress.step_count = 3;
        progress.completed_steps =
            world_concept_observation_count(world, CONCEPT_MASS);
        if (progress.completed_steps > 2) {
            progress.completed_steps = 2;
        }
        if (world_knows_exact_notation(world, CONCEPT_MASS)) {
            progress.completed_steps = 3;
        }
        return progress;
    }
    if (inquiry == INQUIRY_SENTENCE_INSIDE) {
        progress.step_count = 1;
        if (world->knowledge.access_depth >=
            (uint8_t)ACCESS_DEPTH_LINEAGE) {
            progress.completed_steps = 1;
            return progress;
        }
        for (int slot = 0; slot < WORLD_MAX_LOCAL_OVERRIDES; ++slot) {
            const LocalOverride *override =
                &world->universe.local_overrides[slot];
            const Entity *entity =
                override->active && override->has_behavior &&
                        override->behavior_provenance_reach ==
                            (uint8_t)PATCH_REACH_ENTITY
                    ? world_entity_by_id_const(world, override->entity_id)
                    : NULL;
            if (entity != NULL && entity->prototype == PROTOTYPE_APPLE) {
                progress.completed_steps = 1;
                break;
            }
        }
        return progress;
    }
    if (inquiry == INQUIRY_FRUIT_REMEMBERS) {
        progress.step_count = 3;
        bool has_descendant =
            world->knowledge.access_depth >= (uint8_t)ACCESS_DEPTH_LINEAGE;
        for (uint16_t index = 0;
             !has_descendant && index < world->universe.entity_count;
             ++index) {
            const Entity *entity = &world->universe.entities[index];
            has_descendant = entity->prototype == PROTOTYPE_TREE &&
                             entity->descendants_born > 0;
        }
        if (has_descendant) {
            progress.completed_steps = 1;
        }
        for (int index = 0; index < WORLD_MAX_LINEAGES; ++index) {
            const LineageDefinition *lineage =
                &world->universe.lineages[index];
            if (!lineage->active) {
                continue;
            }
            if (lineage->has_nutrition_patch ||
                lineage->has_behavior_patch) {
                if (progress.completed_steps < 2) {
                    progress.completed_steps = 2;
                }
            }
            if (lineage->inherited_births > 0) {
                progress.completed_steps = 3;
                break;
            }
        }
    }
    return progress;
}

InquiryId world_active_inquiry(const World *world) {
    const InquiryProgress first =
        world_inquiry_progress(world, INQUIRY_FIRST_SCAR);
    if (first.completed_steps < first.step_count || world == NULL ||
        world->knowledge.access_depth < (uint8_t)ACCESS_DEPTH_BEHAVIOR) {
        return INQUIRY_FIRST_SCAR;
    }
    const InquiryProgress weight =
        world_inquiry_progress(world, INQUIRY_WEIGHT_OF_THINGS);
    if (weight.completed_steps < weight.step_count) {
        return INQUIRY_WEIGHT_OF_THINGS;
    }
    const InquiryProgress sentence =
        world_inquiry_progress(world, INQUIRY_SENTENCE_INSIDE);
    if (sentence.completed_steps < sentence.step_count) {
        return INQUIRY_SENTENCE_INSIDE;
    }
    const InquiryProgress fruit =
        world_inquiry_progress(world, INQUIRY_FRUIT_REMEMBERS);
    if (fruit.completed_steps < fruit.step_count) {
        return INQUIRY_FRUIT_REMEMBERS;
    }
    return INQUIRY_NONE;
}

KnowledgeGrant world_reconcile_inquiry_knowledge(World *world) {
    if (world == NULL) {
        return KNOWLEDGE_GRANT_NONE;
    }
    const InquiryProgress first =
        world_inquiry_progress(world, INQUIRY_FIRST_SCAR);
    const uint64_t hunger = concept_bit(CONCEPT_HUNGER);
    if (first.completed_steps == first.step_count &&
        (world->knowledge.access_depth < (uint8_t)ACCESS_DEPTH_BEHAVIOR ||
         (world->knowledge.readable_concepts & hunger) == 0)) {
        if (world->knowledge.access_depth < (uint8_t)ACCESS_DEPTH_BEHAVIOR) {
            world->knowledge.access_depth = (uint8_t)ACCESS_DEPTH_BEHAVIOR;
        }
        world->knowledge.perceived_concepts |= hunger;
        world->knowledge.readable_concepts |= hunger;
        (void)snprintf(world->message, sizeof(world->message),
                       "Revelation: Behavior opens. Things have sentences inside.");
        return KNOWLEDGE_GRANT_BEHAVIOR_DEPTH;
    }
    const InquiryProgress sentence =
        world_inquiry_progress(world, INQUIRY_SENTENCE_INSIDE);
    bool descendant_exists = false;
    for (uint16_t index = 0;
         !descendant_exists && index < world->universe.entity_count;
         ++index) {
        const Entity *entity = &world->universe.entities[index];
        descendant_exists = entity->prototype == PROTOTYPE_TREE &&
                            entity->descendants_born > 0;
    }
    const uint64_t parentage = concept_bit(CONCEPT_PARENTAGE);
    const uint64_t vigor = concept_bit(CONCEPT_VIGOR);
    const uint64_t warmth = concept_bit(CONCEPT_WARMTH);
    if (sentence.completed_steps == sentence.step_count &&
        descendant_exists &&
        (world->knowledge.access_depth < (uint8_t)ACCESS_DEPTH_LINEAGE ||
         !world_has_reach(world, PATCH_REACH_LINEAGE) ||
         (world->knowledge.readable_concepts & parentage) == 0 ||
         (world->knowledge.readable_concepts & vigor) == 0 ||
         (world->knowledge.readable_concepts & warmth) == 0)) {
        world->knowledge.access_depth = (uint8_t)ACCESS_DEPTH_LINEAGE;
        world->knowledge.perceived_concepts |= parentage | vigor | warmth;
        world->knowledge.readable_concepts |= parentage | vigor | warmth;
        world->knowledge.reach_mask |=
            patch_reach_bit(PATCH_REACH_LINEAGE);
        (void)snprintf(world->message, sizeof(world->message),
                       "Revelation: fruit remembers the tree that imagined it.");
        return KNOWLEDGE_GRANT_LINEAGE_DEPTH;
    }
    return KNOWLEDGE_GRANT_NONE;
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
    const float movement_speed =
        0.62f * (1.0f + world->embodiment.vigor * 0.005f);
    move_with_collision(world, &world->embodiment.x, &world->embodiment.y,
                        input.move_x * movement_speed,
                        input.move_y * movement_speed, 2.5f, 0);

    world->embodiment.hunger =
        clamp_stat(world->embodiment.hunger + 0.0025f);
    world->embodiment.warmth =
        clamp_stat(world->embodiment.warmth - 0.0030f);
    world->embodiment.vigor =
        clamp_stat(world->embodiment.vigor - 0.0500f);

    const uint16_t materialized_count = world->universe.entity_count;
    for (uint16_t index = 0; index < materialized_count; ++index) {
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
        step_tree_fruit(world, entity);
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
    if (strcmp(name, "vigor") == 0) {
        *out = pali_number((double)context->world->embodiment.vigor);
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
    if (strcmp(name, "vigor") == 0) {
        context->world->embodiment.vigor =
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
        if (context->self->parent_id != 0) {
            Entity *parent = world_entity_by_id(
                context->world, context->self->parent_id);
            if (parent != NULL && parent->prototype == PROTOTYPE_TREE) {
                parent->fruit_ticks = WORLD_FRUIT_REGROW_TICKS;
                parent->dirty = true;
            }
        }
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

static void behavior_fragment_from_document(const PaliDocument *source,
                                            PaliDocument *out) {
    memset(out, 0, sizeof(*out));
    if (source == NULL) {
        return;
    }
    (void)snprintf(out->prototype_name, sizeof(out->prototype_name), "%s",
                   source->prototype_name);
    memcpy(out->constants, source->constants, sizeof(out->constants));
    memcpy(out->names, source->names, sizeof(out->names));
    memcpy(out->expressions, source->expressions, sizeof(out->expressions));
    memcpy(out->statements, source->statements, sizeof(out->statements));
    out->constant_count = source->constant_count;
    out->name_count = source->name_count;
    out->expression_count = source->expression_count;
    out->statement_count = source->statement_count;
    out->has_use = source->has_use;
}

static bool merge_behavior_document(const PaliDocument *prototype,
                                    const PaliDocument *handler,
                                    PaliDocument *out, PaliError *error) {
    if (prototype == NULL || handler == NULL || out == NULL ||
        handler->property_count != 0 ||
        strcmp(prototype->prototype_name, handler->prototype_name) != 0) {
        set_error(error, 1, 1,
                  "Behavior Patch must contain only this prototype's use handler");
        return false;
    }
    *out = *prototype;
    memset(out->constants, 0, sizeof(out->constants));
    memset(out->names, 0, sizeof(out->names));
    memset(out->expressions, 0, sizeof(out->expressions));
    memset(out->statements, 0, sizeof(out->statements));
    memcpy(out->constants, handler->constants, sizeof(out->constants));
    memcpy(out->names, handler->names, sizeof(out->names));
    memcpy(out->expressions, handler->expressions, sizeof(out->expressions));
    memcpy(out->statements, handler->statements, sizeof(out->statements));
    out->constant_count = handler->constant_count;
    out->name_count = handler->name_count;
    out->expression_count = handler->expression_count;
    out->statement_count = handler->statement_count;
    out->has_use = handler->has_use;
    return true;
}

static bool behavior_concept_is_readable(const World *world,
                                         ConceptId concept) {
    const ConceptAccess access = world_concept_access(world, concept);
    return access == CONCEPT_ACCESS_READABLE ||
           access == CONCEPT_ACCESS_PATCHABLE;
}

static bool behavior_document_is_valid(const World *world,
                                       const PaliDocument *prototype,
                                       const PaliDocument *handler,
                                       bool require_knowledge,
                                       PaliError *error) {
    PaliValueType expression_types[PALI_MAX_EXPRESSIONS];
    memset(expression_types, 0, sizeof(expression_types));
    for (uint16_t index = 0; index < handler->expression_count; ++index) {
        const PaliExpression *expression = &handler->expressions[index];
        PaliValueType type = PALI_VALUE_NIL;
        switch ((PaliExpressionKind)expression->kind) {
            case PALI_EXPRESSION_LITERAL:
                type = handler->constants[expression->operand].type;
                break;
            case PALI_EXPRESSION_GET_SELF: {
                const char *name = handler->names[expression->operand];
                const ConceptDefinition *concept = lexicon_find_by_name(name);
                const PaliValue *property =
                    pali_document_property(prototype, name);
                if (concept == NULL || property == NULL ||
                    property->type != concept->value_type ||
                    (require_knowledge &&
                     !behavior_concept_is_readable(world, concept->id))) {
                    set_error(error, (int)expression->line, 1,
                              "Behavior refers to an unreadable self concept");
                    return false;
                }
                type = concept->value_type;
                break;
            }
            case PALI_EXPRESSION_GET_ACTOR: {
                const char *name = handler->names[expression->operand];
                const ConceptDefinition *concept = lexicon_find_by_name(name);
                if (concept == NULL ||
                    (concept->id != CONCEPT_HUNGER &&
                     concept->id != CONCEPT_WARMTH &&
                     concept->id != CONCEPT_VIGOR &&
                     concept->id != CONCEPT_X && concept->id != CONCEPT_Y) ||
                    (require_knowledge &&
                     !behavior_concept_is_readable(world, concept->id))) {
                    set_error(error, (int)expression->line, 1,
                              "Behavior refers to an unreadable actor concept");
                    return false;
                }
                type = concept->value_type;
                break;
            }
            case PALI_EXPRESSION_ADD:
            case PALI_EXPRESSION_SUBTRACT:
            case PALI_EXPRESSION_MIN:
            case PALI_EXPRESSION_MAX:
                if (expression_types[expression->left] != PALI_VALUE_NUMBER ||
                    expression_types[expression->right] != PALI_VALUE_NUMBER) {
                    set_error(error, (int)expression->line, 1,
                              "Behavior operator sockets require numbers");
                    return false;
                }
                type = PALI_VALUE_NUMBER;
                break;
            case PALI_EXPRESSION_MULTIPLY:
            case PALI_EXPRESSION_DIVIDE:
            case PALI_EXPRESSION_NEGATE:
            default:
                set_error(error, (int)expression->line, 1,
                          "Knowledge does not contain that Behavior operator");
                return false;
        }
        expression_types[index] = type;
    }

    for (uint16_t index = 0; index < handler->statement_count; ++index) {
        const PaliStatement *statement = &handler->statements[index];
        switch ((PaliStatementKind)statement->kind) {
            case PALI_STATEMENT_SET_ACTOR: {
                const ConceptDefinition *concept =
                    lexicon_find_by_name(handler->names[statement->name]);
                if (concept == NULL ||
                    (concept->id != CONCEPT_HUNGER &&
                     concept->id != CONCEPT_WARMTH &&
                     concept->id != CONCEPT_VIGOR) ||
                    (require_knowledge &&
                     !behavior_concept_is_readable(world, concept->id)) ||
                    expression_types[statement->expression] !=
                        concept->value_type) {
                    set_error(error, (int)statement->line, 1,
                              "Behavior effect does not fit its actor socket");
                    return false;
                }
                break;
            }
            case PALI_STATEMENT_SET_SELF: {
                const char *name = handler->names[statement->name];
                const ConceptDefinition *concept = lexicon_find_by_name(name);
                const PaliValue *property =
                    pali_document_property(prototype, name);
                if (concept == NULL || property == NULL ||
                    (require_knowledge &&
                     world_concept_access(world, concept->id) !=
                         CONCEPT_ACCESS_PATCHABLE) ||
                    expression_types[statement->expression] !=
                        property->type) {
                    set_error(error, (int)statement->line, 1,
                              "Behavior effect does not fit its self socket");
                    return false;
                }
                break;
            }
            case PALI_STATEMENT_MESSAGE:
                if (expression_types[statement->expression] !=
                    PALI_VALUE_TEXT) {
                    set_error(error, (int)statement->line, 1,
                              "reveal Clause requires text");
                    return false;
                }
                break;
            case PALI_STATEMENT_DESTROY_SELF:
                break;
            default:
                set_error(error, (int)statement->line, 1,
                          "Knowledge does not contain that Behavior effect");
                return false;
        }
    }
    return true;
}

static bool resolve_behavior_program(const World *world,
                                     const PaliDocument *prototype,
                                     const PaliDocument *handler,
                                     PaliProgram *out, PaliError *error) {
    PaliProgram handler_validation;
    if (world == NULL || prototype == NULL || handler == NULL || out == NULL ||
        !handler->has_use ||
        !pali_compile_document(handler, &handler_validation, error)) {
        if (error != NULL && error->message[0] == '\0') {
            set_error(error, 1, 1,
                      "Behavior Patch requires a valid on use(actor) trigger");
        }
        return false;
    }
    PaliDocument resolved;
    if (!merge_behavior_document(prototype, handler, &resolved, error) ||
        !behavior_document_is_valid(world, prototype, handler, true, error) ||
        !pali_compile_document(&resolved, out, error)) {
        return false;
    }
    if (out->code_count > WORLD_BEHAVIOR_PATCH_BUDGET) {
        set_error(error, 1, 1,
                  "Behavior candidate exceeds the known Clause budget");
        return false;
    }
    return true;
}

bool world_behavior_is_patchable(const World *world, const Entity *entity) {
    return world != NULL && entity != NULL && entity->active &&
           entity->prototype == PROTOTYPE_APPLE &&
           world->knowledge.access_depth >= (uint8_t)ACCESS_DEPTH_BEHAVIOR &&
           world_knows_exact_notation(world, CONCEPT_MASS) &&
           world_has_reach(world, PATCH_REACH_ENTITY);
}

static bool build_use_behavior_document(const char *prototype_name,
                                        UseBehaviorDraft draft,
                                        PaliDocument *out,
                                        PaliError *error) {
    if (prototype_name == NULL || out == NULL ||
        behavior_draft_index(draft) < 0) {
        set_error(error, 0, 0, "Behavior Draft is not a known apple grammar");
        return false;
    }
    const char *hunger = "";
    if (draft.hunger == BEHAVIOR_HUNGER_SOOTHE) {
        hunger = "        actor.hunger = max(0, actor.hunger - self.nutrition)\n";
    } else if (draft.hunger == BEHAVIOR_HUNGER_SHARPEN) {
        hunger = "        actor.hunger = min(100, actor.hunger + self.nutrition)\n";
    }
    const char *aftertaste = "";
    if (draft.aftertaste == BEHAVIOR_AFTERTASTE_KINDLE) {
        aftertaste =
            "        actor.warmth = min(100, actor.warmth + self.nutrition)\n";
    } else if (draft.aftertaste == BEHAVIOR_AFTERTASTE_QUICKEN) {
        aftertaste =
            "        actor.vigor = min(100, actor.vigor + self.nutrition)\n";
    }
    const char *voice = "";
    if (draft.voice == BEHAVIOR_VOICE_FADE) {
        voice = "        message(\"The apple becomes less real.\")\n";
    } else if (draft.voice == BEHAVIOR_VOICE_REMEMBER) {
        voice = "        message(\"The apple remembers being eaten.\")\n";
    }
    const char *fate = draft.fate == BEHAVIOR_FATE_CEASE
                           ? "        destroy(self)\n"
                           : "";
    char source[PALI_SOURCE_CAP];
    const int written = snprintf(
        source, sizeof(source),
        "prototype %s\n    on use(actor)\n%s%s%s%s    end\nend\n",
        prototype_name, hunger, aftertaste, voice, fate);
    if (written < 0 || (size_t)written >= sizeof(source)) {
        set_error(error, 0, 0, "Behavior Draft exceeds its source bound");
        return false;
    }
    return pali_parse_document(source, out, error);
}

bool world_build_apple_behavior_document(const World *world,
                                         UseBehaviorDraft draft,
                                         PaliDocument *out,
                                         PaliError *error) {
    if (world == NULL) {
        set_error(error, 0, 0, "Behavior Draft has no Universe");
        return false;
    }
    return build_use_behavior_document(
        world_prototype_name(PROTOTYPE_APPLE), draft, out, error);
}

bool world_build_use_behavior_document(const World *world,
                                       const Entity *entity,
                                       UseBehaviorDraft draft,
                                       PaliDocument *out,
                                       PaliError *error) {
    if (world == NULL || entity == NULL ||
        entity->prototype != PROTOTYPE_APPLE) {
        set_error(error, 0, 0, "Behavior Draft is not a known apple grammar");
        return false;
    }
    return build_use_behavior_document(
        world_prototype_name((PrototypeId)entity->prototype), draft, out,
        error);
}

static bool behavior_draft_from_source(const World *world,
                                       const Entity *entity,
                                       const char *source,
                                       UseBehaviorDraft *out,
                                       PaliError *error) {
    for (int hunger = 0; hunger < BEHAVIOR_HUNGER_COUNT; ++hunger) {
        for (int voice = 0; voice < BEHAVIOR_VOICE_COUNT; ++voice) {
            for (int fate = 0; fate < BEHAVIOR_FATE_COUNT; ++fate) {
                for (int aftertaste = 0;
                     aftertaste < BEHAVIOR_AFTERTASTE_COUNT; ++aftertaste) {
                    const UseBehaviorDraft draft = {
                        (BehaviorHungerClause)hunger,
                        (BehaviorVoiceClause)voice,
                        (BehaviorFateClause)fate,
                        (BehaviorAftertasteClause)aftertaste};
                    PaliDocument candidate;
                    char candidate_source[PALI_SOURCE_CAP];
                    if (!world_build_use_behavior_document(
                            world, entity, draft, &candidate, error) ||
                        !pali_format_document(
                            &candidate, candidate_source,
                            sizeof(candidate_source), error)) {
                        return false;
                    }
                    if (strcmp(source, candidate_source) == 0) {
                        if (out != NULL) {
                            *out = draft;
                        }
                        return true;
                    }
                }
            }
        }
    }
    set_error(error, 1, 1, "Behavior Patch is not a known apple grammar");
    return false;
}

bool world_behavior_draft_from_document(const World *world,
                                        const Entity *entity,
                                        const PaliDocument *handler,
                                        UseBehaviorDraft *out,
                                        PaliError *error) {
    if (world == NULL || entity == NULL || handler == NULL || out == NULL ||
        entity->prototype != PROTOTYPE_APPLE) {
        set_error(error, 0, 0, "Behavior Patch has no apple target");
        return false;
    }
    char normalized[PALI_SOURCE_CAP];
    PaliDocument normalized_document;
    PaliProgram validation;
    if (!pali_format_document(handler, normalized, sizeof(normalized),
                              error) ||
        !pali_parse_document(normalized, &normalized_document, error) ||
        !resolve_behavior_program(
            world,
            &world->universe.prototypes[entity->prototype].document,
            &normalized_document, &validation, error)) {
        return false;
    }
    return behavior_draft_from_source(world, entity, normalized, out, error);
}

bool world_get_entity_use_behavior_draft(const World *world,
                                         const Entity *entity,
                                         UseBehaviorDraft *out) {
    if (world == NULL || entity == NULL || out == NULL ||
        entity->prototype != PROTOTYPE_APPLE) {
        return false;
    }
    const LocalOverride *override = entity_override_const(world, entity);
    if (override != NULL && override->has_behavior) {
        if (behavior_draft_index(override->behavior) < 0) {
            return false;
        }
        *out = override->behavior;
        return true;
    }
    PaliDocument fragment;
    behavior_fragment_from_document(
        &world->universe.prototypes[entity->prototype].document, &fragment);
    char effective_source[PALI_SOURCE_CAP];
    PaliError error;
    if (!pali_format_document(&fragment, effective_source,
                              sizeof(effective_source), &error)) {
        return false;
    }
    return behavior_draft_from_source(world, entity, effective_source, out,
                                      &error);
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
    PaliProgram local_program;
    const LocalOverride *override = entity_override_const(world, entity);
    const PaliProgram *use_program = world_entity_program(world, entity);
    if (override != NULL && override->has_behavior) {
        PaliDocument handler;
        if (!world_build_use_behavior_document(
                world, entity, override->behavior, &handler, error) ||
            !resolve_behavior_program(
                world, &world->universe.prototypes[entity->prototype].document,
                &handler, &local_program, error)) {
            return false;
        }
        use_program = &local_program;
    }
    HostContext context;
    context.world = world;
    context.self = entity;
    context.program = use_program;
    PaliHost host;
    host.user = &context;
    host.get_property = host_get;
    host.set_property = host_set;
    host.call = host_call;
    const int budget = world_entity_has_behavior_patch(world, entity)
                           ? WORLD_BEHAVIOR_PATCH_BUDGET
                           : PALI_DEFAULT_BUDGET;
    if (!pali_run_use(context.program, &host, budget, error)) {
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
        if (override->has_behavior) {
            PaliDocument handler;
            PaliProgram validation;
            if (prototype != PROTOTYPE_APPLE ||
                !build_use_behavior_document(candidate_document.prototype_name,
                                             override->behavior, &handler,
                                             error) ||
                !resolve_behavior_program(world, &candidate_document,
                                          &handler, &validation, error)) {
                set_error(error, 1, 1,
                          "prototype patch conflicts with a local Behavior Patch");
                return false;
            }
        }
    }
    if (prototype == PROTOTYPE_APPLE) {
        for (int slot = 0; slot < WORLD_MAX_LINEAGES; ++slot) {
            const LineageDefinition *lineage =
                &world->universe.lineages[slot];
            if (!lineage->active) {
                continue;
            }
            const PaliValue *broader =
                pali_document_property(&candidate_document, "nutrition");
            if (lineage->has_nutrition_patch &&
                (broader == NULL || broader->type != PALI_VALUE_NUMBER)) {
                set_error(error, 1, 1,
                          "prototype patch conflicts with a Lineage value");
                return false;
            }
            if (lineage->has_behavior_patch) {
                PaliDocument handler;
                PaliProgram validation;
                if (!build_use_behavior_document(
                        candidate_document.prototype_name,
                        lineage->draft.behavior, &handler, error) ||
                    !resolve_behavior_program(world, &candidate_document,
                                              &handler, &validation, error)) {
                    set_error(error, 1, 1,
                              "prototype patch conflicts with a Lineage Behavior");
                    return false;
                }
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

static bool recycled_descendant_will_free_override(const World *world) {
    if (world == NULL) {
        return false;
    }
    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        const Entity *candidate = &world->universe.entities[index];
        if (candidate->active || candidate->parent_id == 0) {
            continue;
        }
        if (candidate->local_override >= 0 &&
            candidate->local_override < WORLD_MAX_LOCAL_OVERRIDES) {
            const LocalOverride *override =
                &world->universe
                     .local_overrides[candidate->local_override];
            if (override->active &&
                override->entity_id == candidate->id) {
                return true;
            }
        }
    }
    return false;
}

static bool local_override_is_empty(const LocalOverride *override) {
    return override == NULL ||
           (override->value_count == 0 && !override->has_behavior);
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

static bool provenance_is_valid(const World *world, const Entity *entity,
                                uint8_t reach, uint64_t provenance_id) {
    if (reach == (uint8_t)PATCH_REACH_ENTITY) {
        return provenance_id == entity->id;
    }
    if (reach == (uint8_t)PATCH_REACH_LINEAGE &&
        entity->parent_id != 0 && provenance_id == entity->parent_id) {
        const Entity *tree =
            world_entity_by_id_const(world, provenance_id);
        return tree != NULL && tree->prototype == PROTOTYPE_TREE;
    }
    return false;
}

bool world_restore_local_override(World *world, LocalOverride definition,
                                  PaliError *error) {
    Entity *entity = world_entity_by_id(world, definition.entity_id);
    if (world == NULL || entity == NULL || !definition.active ||
        entity->local_override != -1 ||
        definition.value_count > WORLD_LOCAL_PATCH_VALUES ||
        (definition.value_count == 0 && !definition.has_behavior)) {
        set_error(error, 0, 0, "saved Entity Patch is not valid");
        return false;
    }
    const PaliDocument *prototype =
        &world->universe.prototypes[entity->prototype].document;
    for (uint8_t index = 0; index < definition.value_count; ++index) {
        const LocalPatchValue *value = &definition.values[index];
        const ConceptDefinition *concept =
            lexicon_find_by_id(value->concept);
        const PaliValue *broader =
            concept != NULL
                ? pali_document_property(prototype, concept->name)
                : NULL;
        if (concept == NULL ||
            (concept->operation_flags & CONCEPT_OP_REPLACE) == 0 ||
            !lexicon_value_is_valid(concept, value->value) ||
            broader == NULL || broader->type != value->value.type ||
            !provenance_is_valid(world, entity, value->provenance_reach,
                                 value->provenance_id)) {
            set_error(error, 0, 0,
                      "saved Entity value violates its semantic contract");
            return false;
        }
        for (uint8_t earlier = 0; earlier < index; ++earlier) {
            if (definition.values[earlier].concept == value->concept) {
                set_error(error, 0, 0,
                          "saved Entity Patch repeats a concept");
                return false;
            }
        }
    }
    if (definition.has_behavior) {
        PaliDocument handler;
        PaliProgram validation;
        if (entity->prototype != PROTOTYPE_APPLE ||
            !provenance_is_valid(world, entity,
                                 definition.behavior_provenance_reach,
                                 definition.behavior_provenance_id) ||
            !world_build_use_behavior_document(
                world, entity, definition.behavior, &handler, error) ||
            !resolve_behavior_program(world, prototype, &handler,
                                      &validation, error)) {
            if (error != NULL && error->message[0] == '\0') {
                set_error(error, 0, 0,
                          "saved Entity Behavior violates its semantic contract");
            }
            return false;
        }
    }
    const int slot = available_override_slot(world);
    if (slot < 0) {
        set_error(error, 0, 0, "Entity Patch capacity reached");
        return false;
    }
    world->universe.local_overrides[slot] = definition;
    entity->local_override = (int8_t)slot;
    entity->dirty = true;
    return true;
}

bool world_clear_entity_behavior_patch(World *world, uint64_t entity_id,
                                       PaliError *error) {
    Entity *entity = world_entity_by_id(world, entity_id);
    if (entity == NULL || !entity->active) {
        set_error(error, 0, 0, "Behavior Patch target does not exist");
        return false;
    }
    if (!world_behavior_is_patchable(world, entity)) {
        set_error(error, 0, 0,
                  "Knowledge cannot Patch this Entity's Behavior");
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
    if (!override->has_behavior) {
        return true;
    }
    memset(&override->behavior, 0, sizeof(override->behavior));
    override->behavior_provenance_id = 0;
    override->behavior_provenance_reach = 0;
    override->has_behavior = false;
    if (local_override_is_empty(override)) {
        memset(override, 0, sizeof(*override));
        entity->local_override = -1;
    }
    entity->dirty = true;
    (void)snprintf(world->message, sizeof(world->message),
                   "Entity Behavior Patch removed.");
    return true;
}

bool world_apply_entity_behavior_patch(World *world, uint64_t entity_id,
                                       const PaliDocument *handler,
                                       PaliError *error) {
    Entity *entity = world_entity_by_id(world, entity_id);
    if (entity == NULL || !entity->active) {
        set_error(error, 0, 0, "Behavior Patch target does not exist");
        return false;
    }
    if (!world_behavior_is_patchable(world, entity)) {
        set_error(error, 0, 0,
                  "Knowledge cannot Patch this Entity's Behavior");
        return false;
    }
    if (handler == NULL) {
        set_error(error, 0, 0, "Behavior Patch has no typed document");
        return false;
    }

    char normalized_source[PALI_SOURCE_CAP];
    PaliDocument normalized_document;
    if (!pali_format_document(handler, normalized_source,
                              sizeof(normalized_source), error) ||
        !pali_parse_document(normalized_source, &normalized_document, error)) {
        return false;
    }
    PrototypeDefinition *prototype =
        &world->universe.prototypes[entity->prototype];
    PaliProgram validation_program;
    if (!resolve_behavior_program(world, &prototype->document,
                                  &normalized_document, &validation_program,
                                  error)) {
        return false;
    }
    UseBehaviorDraft draft;
    if (!behavior_draft_from_source(world, entity, normalized_source, &draft,
                                    error)) {
        return false;
    }

    PaliDocument inherited;
    char inherited_source[PALI_SOURCE_CAP];
    behavior_fragment_from_document(&prototype->document, &inherited);
    if (!pali_format_document(&inherited, inherited_source,
                              sizeof(inherited_source), error)) {
        return false;
    }
    if (strcmp(normalized_source, inherited_source) == 0) {
        return world_clear_entity_behavior_patch(world, entity_id, error);
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
    candidate.behavior = draft;
    candidate.behavior_provenance_id = entity_id;
    candidate.behavior_provenance_reach = (uint8_t)PATCH_REACH_ENTITY;
    candidate.has_behavior = true;
    world->universe.local_overrides[slot] = candidate;
    entity->local_override = (int8_t)slot;
    entity->dirty = true;
    (void)snprintf(world->message, sizeof(world->message),
                   "This %s now carries a local Behavior Scar.",
                   world_prototype_name((PrototypeId)entity->prototype));
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
    if (local_override_is_empty(override)) {
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
    candidate.values[value_index].provenance_id = entity_id;
    candidate.values[value_index].provenance_reach =
        (uint8_t)PATCH_REACH_ENTITY;
    world->universe.local_overrides[slot] = candidate;
    entity->local_override = (int8_t)slot;
    entity->dirty = true;
    (void)snprintf(world->message, sizeof(world->message),
                   "This %s now carries a local %s Scar.",
                   world_prototype_name((PrototypeId)entity->prototype),
                   concept->name);
    return true;
}

static bool behavior_drafts_equal(UseBehaviorDraft left,
                                  UseBehaviorDraft right) {
    return left.hunger == right.hunger && left.voice == right.voice &&
           left.fate == right.fate &&
           left.aftertaste == right.aftertaste;
}

static LineageDefinition *lineage_for_tree(World *world,
                                           uint64_t tree_id) {
    if (world == NULL || tree_id == 0) {
        return NULL;
    }
    for (int index = 0; index < WORLD_MAX_LINEAGES; ++index) {
        LineageDefinition *lineage = &world->universe.lineages[index];
        if (lineage->active && lineage->progenitor_id == tree_id) {
            return lineage;
        }
    }
    return NULL;
}

const LineageDefinition *world_tree_lineage(const World *world,
                                            const Entity *tree) {
    if (world == NULL || tree == NULL ||
        tree->prototype != PROTOTYPE_TREE) {
        return NULL;
    }
    return lineage_for_tree((World *)(uintptr_t)world, tree->id);
}

static LineageDefinition *available_lineage(World *world) {
    for (int index = 0; index < WORLD_MAX_LINEAGES; ++index) {
        if (!world->universe.lineages[index].active) {
            return &world->universe.lineages[index];
        }
    }
    return NULL;
}

static bool default_fruit_draft(const World *world,
                                FruitLineageDraft *out) {
    if (world == NULL || out == NULL) {
        return false;
    }
    const PaliValue *nutrition = pali_document_property(
        &world->universe.prototypes[PROTOTYPE_APPLE].document, "nutrition");
    Entity apple;
    memset(&apple, 0, sizeof(apple));
    apple.prototype = PROTOTYPE_APPLE;
    apple.local_override = -1;
    if (nutrition == NULL || nutrition->type != PALI_VALUE_NUMBER ||
        !world_get_entity_use_behavior_draft(world, &apple,
                                             &out->behavior)) {
        return false;
    }
    out->nutrition = nutrition->as.number;
    return true;
}

bool world_get_tree_lineage_draft(const World *world, const Entity *tree,
                                  FruitLineageDraft *out) {
    if (world == NULL || tree == NULL || out == NULL ||
        tree->prototype != PROTOTYPE_TREE ||
        !default_fruit_draft(world, out)) {
        return false;
    }
    const LineageDefinition *lineage = world_tree_lineage(world, tree);
    if (lineage != NULL) {
        if (lineage->has_nutrition_patch) {
            out->nutrition = lineage->draft.nutrition;
        }
        if (lineage->has_behavior_patch) {
            out->behavior = lineage->draft.behavior;
        }
    }
    return true;
}

static int fruit_nutrition_inflection(uint64_t tree_id,
                                      uint32_t birth_ordinal) {
    const uint64_t mixed =
        mix64(tree_id ^ ((uint64_t)birth_ordinal << 21) ^
              UINT64_C(0x494e464c454354));
    return (int)(mixed %
                 (uint64_t)(WORLD_FRUIT_INFLECTION_RADIUS * 2 + 1)) -
           WORLD_FRUIT_INFLECTION_RADIUS;
}

double world_tree_preview_fruit_nutrition(const World *world,
                                          const Entity *tree,
                                          FruitLineageDraft draft) {
    if (world == NULL || tree == NULL ||
        tree->prototype != PROTOTYPE_TREE) {
        return 0.0;
    }
    double nutrition =
        draft.nutrition +
        (double)fruit_nutrition_inflection(tree->id,
                                           tree->descendants_born + 1u);
    const ConceptDefinition *concept =
        lexicon_find_by_id(CONCEPT_NUTRITION);
    if (concept != NULL) {
        if (nutrition < concept->numeric_min) {
            nutrition = concept->numeric_min;
        } else if (nutrition > concept->numeric_max) {
            nutrition = concept->numeric_max;
        }
    }
    return nutrition;
}

double world_tree_next_fruit_nutrition(const World *world,
                                       const Entity *tree) {
    FruitLineageDraft draft;
    if (!world_get_tree_lineage_draft(world, tree, &draft)) {
        return 0.0;
    }
    return world_tree_preview_fruit_nutrition(world, tree, draft);
}

const Entity *world_tree_current_fruit(const World *world,
                                       const Entity *tree) {
    if (world == NULL || tree == NULL ||
        tree->prototype != PROTOTYPE_TREE) {
        return NULL;
    }
    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        const Entity *entity = &world->universe.entities[index];
        if (entity->active && entity->prototype == PROTOTYPE_APPLE &&
            entity->parent_id == tree->id) {
            return entity;
        }
    }
    return NULL;
}

bool world_tree_lineage_is_patchable(const World *world,
                                     const Entity *tree) {
    return world != NULL && tree != NULL && tree->active &&
           tree->prototype == PROTOTYPE_TREE &&
           world->knowledge.access_depth >= (uint8_t)ACCESS_DEPTH_LINEAGE &&
           world_has_reach(world, PATCH_REACH_LINEAGE);
}

static bool validate_lineage_draft(const World *world,
                                   FruitLineageDraft draft,
                                   PaliError *error) {
    const ConceptDefinition *nutrition =
        lexicon_find_by_id(CONCEPT_NUTRITION);
    PaliDocument handler;
    PaliProgram validation;
    return nutrition != NULL &&
           lexicon_value_is_valid(nutrition,
                                  pali_number(draft.nutrition)) &&
           world_build_apple_behavior_document(world, draft.behavior,
                                               &handler, error) &&
           resolve_behavior_program(
               world,
               &world->universe.prototypes[PROTOTYPE_APPLE].document,
               &handler, &validation, error);
}

bool world_apply_tree_lineage_draft(World *world, uint64_t tree_id,
                                    FruitLineageDraft draft,
                                    PaliError *error) {
    Entity *tree = world_entity_by_id(world, tree_id);
    if (!world_tree_lineage_is_patchable(world, tree)) {
        set_error(error, 0, 0,
                  "Knowledge cannot Patch this tree's future fruit");
        return false;
    }
    if (!validate_lineage_draft(world, draft, error)) {
        if (error != NULL && error->message[0] == '\0') {
            set_error(error, 0, 0,
                      "future fruit violates its Lineage grammar");
        }
        return false;
    }
    FruitLineageDraft inherited;
    if (!default_fruit_draft(world, &inherited)) {
        set_error(error, 0, 0, "apple inheritance has no broader meaning");
        return false;
    }
    const bool nutrition_changed = draft.nutrition != inherited.nutrition;
    const bool behavior_changed =
        !behavior_drafts_equal(draft.behavior, inherited.behavior);
    LineageDefinition *lineage = lineage_for_tree(world, tree_id);
    if (lineage == NULL && !nutrition_changed && !behavior_changed) {
        return true;
    }
    if (lineage == NULL) {
        lineage = available_lineage(world);
        if (lineage == NULL) {
            set_error(error, 0, 0, "Lineage capacity is full");
            return false;
        }
        memset(lineage, 0, sizeof(*lineage));
        lineage->active = true;
        lineage->progenitor_id = tree_id;
    }
    lineage->draft = draft;
    lineage->has_nutrition_patch = nutrition_changed;
    lineage->has_behavior_patch = behavior_changed;
    if (!nutrition_changed && !behavior_changed &&
        lineage->inherited_births == 0) {
        memset(lineage, 0, sizeof(*lineage));
    }
    tree->dirty = true;
    (void)snprintf(world->message, sizeof(world->message),
                   nutrition_changed || behavior_changed
                       ? "This tree will teach its future fruit."
                       : "This tree's future fruit returns to inheritance.");
    return true;
}

bool world_clear_tree_lineage_patch(World *world, uint64_t tree_id,
                                    PaliError *error) {
    const Entity *tree = world_entity_by_id_const(world, tree_id);
    FruitLineageDraft inherited;
    if (tree == NULL || !default_fruit_draft(world, &inherited)) {
        set_error(error, 0, 0, "Lineage target does not exist");
        return false;
    }
    return world_apply_tree_lineage_draft(world, tree_id, inherited, error);
}

bool world_restore_lineage(World *world, LineageDefinition definition,
                           PaliError *error) {
    Entity *tree = world_entity_by_id(world, definition.progenitor_id);
    if (tree == NULL || tree->prototype != PROTOTYPE_TREE ||
        !definition.active ||
        (!definition.has_nutrition_patch &&
         !definition.has_behavior_patch && definition.inherited_births == 0) ||
        lineage_for_tree(world, definition.progenitor_id) != NULL ||
        !validate_lineage_draft(world, definition.draft, error)) {
        if (error != NULL && error->message[0] == '\0') {
            set_error(error, 0, 0, "saved Lineage is not valid");
        }
        return false;
    }
    LineageDefinition *slot = available_lineage(world);
    if (slot == NULL) {
        set_error(error, 0, 0, "Lineage capacity is full");
        return false;
    }
    *slot = definition;
    return true;
}

static bool find_fruit_position(const World *world, const Entity *tree,
                                uint32_t birth_ordinal, float *out_x,
                                float *out_y) {
    static const float DIRECTIONS[8][2] = {
        {1.0f, 0.0f},  {0.7f, 0.7f},  {0.0f, 1.0f}, {-0.7f, 0.7f},
        {-1.0f, 0.0f}, {-0.7f, -0.7f}, {0.0f, -1.0f}, {0.7f, -0.7f}};
    const uint64_t start =
        mix64(tree->id ^ ((uint64_t)birth_ordinal << 13));
    for (int attempt = 0; attempt < 16; ++attempt) {
        const int direction = (int)((start + (uint64_t)attempt) % 8u);
        const float radius = attempt < 8 ? 10.0f : 15.0f;
        const float x = tree->x + DIRECTIONS[direction][0] * radius;
        const float y = tree->y + DIRECTIONS[direction][1] * radius;
        if (!position_blocked(world, x, y, 2.0f, 0) &&
            !position_has_entity(world, x, y, 4.0f)) {
            *out_x = x;
            *out_y = y;
            return true;
        }
    }
    return false;
}

static bool materialize_fruit_inheritance(World *world, Entity *fruit,
                                          Entity *tree,
                                          LineageDefinition *lineage,
                                          double nutrition) {
    FruitLineageDraft broader;
    if (!default_fruit_draft(world, &broader)) {
        return false;
    }
    const bool inherit_nutrition =
        nutrition != broader.nutrition ||
        (lineage != NULL && lineage->has_nutrition_patch);
    const bool inherit_behavior =
        lineage != NULL && lineage->has_behavior_patch;
    if (!inherit_nutrition && !inherit_behavior) {
        return true;
    }
    const int slot = available_override_slot(world);
    if (slot < 0) {
        return false;
    }
    LocalOverride *override = &world->universe.local_overrides[slot];
    memset(override, 0, sizeof(*override));
    override->active = true;
    override->entity_id = fruit->id;
    if (inherit_nutrition) {
        override->value_count = 1;
        override->values[0].concept = CONCEPT_NUTRITION;
        override->values[0].value = pali_number(nutrition);
        override->values[0].provenance_id = tree->id;
        override->values[0].provenance_reach =
            (uint8_t)PATCH_REACH_LINEAGE;
    }
    if (inherit_behavior) {
        override->behavior = lineage->draft.behavior;
        override->behavior_provenance_id = tree->id;
        override->behavior_provenance_reach =
            (uint8_t)PATCH_REACH_LINEAGE;
        override->has_behavior = true;
    }
    fruit->local_override = (int8_t)slot;
    return true;
}

static void step_tree_fruit(World *world, Entity *tree) {
    if (world == NULL || tree == NULL || !tree->active ||
        tree->prototype != PROTOTYPE_TREE ||
        world_tree_current_fruit(world, tree) != NULL) {
        return;
    }
    if (tree->fruit_ticks > 0) {
        tree->fruit_ticks--;
        tree->dirty = true;
        if (tree->fruit_ticks > 0) {
            return;
        }
    }
    const uint32_t ordinal = tree->descendants_born + 1u;
    const double nutrition = world_tree_next_fruit_nutrition(world, tree);
    FruitLineageDraft broader;
    if (!default_fruit_draft(world, &broader)) {
        tree->fruit_ticks = 60;
        return;
    }
    LineageDefinition *lineage = lineage_for_tree(world, tree->id);
    const bool needs_override =
        nutrition != broader.nutrition ||
        (lineage != NULL && (lineage->has_nutrition_patch ||
                             lineage->has_behavior_patch));
    if ((needs_override && available_override_slot(world) < 0 &&
         !recycled_descendant_will_free_override(world)) ||
        ordinal == 0) {
        tree->fruit_ticks = 60;
        return;
    }
    float x = 0.0f;
    float y = 0.0f;
    if (!find_fruit_position(world, tree, ordinal, &x, &y)) {
        tree->fruit_ticks = 60;
        return;
    }
    const uint64_t id = world_descendant_id(
        world, tree->id, ordinal, PROTOTYPE_APPLE);
    PaliError error;
    if (!world_restore_descendant(world, id, tree->id, ordinal,
                                  PROTOTYPE_APPLE, &error)) {
        tree->fruit_ticks = 60;
        return;
    }
    Entity *fruit = world_entity_by_id(world, id);
    if (fruit == NULL || !materialize_fruit_inheritance(
                             world, fruit, tree, lineage, nutrition)) {
        if (fruit != NULL) {
            fruit->active = false;
        }
        tree->fruit_ticks = 60;
        return;
    }
    fruit->x = x;
    fruit->y = y;
    tree->descendants_born = ordinal;
    tree->fruit_ticks = 0;
    tree->dirty = true;
    if (lineage != NULL &&
        (lineage->has_nutrition_patch || lineage->has_behavior_patch)) {
        lineage->inherited_births++;
        (void)snprintf(world->message, sizeof(world->message),
                       "A future sentence becomes fruit.");
    }
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

static uint64_t hash_double(uint64_t hash, double value) {
    uint64_t bits = 0;
    memcpy(&bits, &value, sizeof(bits));
    return hash_bytes(hash, &bits, sizeof(bits));
}

static uint64_t hash_behavior_draft(uint64_t hash,
                                    UseBehaviorDraft draft) {
    const uint8_t clauses[4] = {
        (uint8_t)draft.hunger, (uint8_t)draft.voice,
        (uint8_t)draft.fate, (uint8_t)draft.aftertaste};
    return hash_bytes(hash, clauses, sizeof(clauses));
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
    uint16_t genesis_count = 0;
    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        if (world->universe.entities[index].parent_id == 0) {
            genesis_count++;
        }
    }
    hash = hash_bytes(hash, &genesis_count, sizeof(genesis_count));
    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        const Entity *entity = &world->universe.entities[index];
        if (entity->parent_id != 0) {
            continue;
        }
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
    hash = hash_bytes(hash, world->knowledge.observed_prototypes,
                      sizeof(world->knowledge.observed_prototypes));
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
    hash = hash_float(hash, world->embodiment.vigor);
    for (int index = 0; index < PROTOTYPE_COUNT; ++index) {
        hash = hash_bytes(hash,
                          world->universe.prototypes[index].current_source,
                          strlen(world->universe.prototypes[index].current_source));
    }
    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        const Entity *entity = &world->universe.entities[index];
        hash = hash_bytes(hash, &entity->id, sizeof(entity->id));
        hash = hash_bytes(hash, &entity->prototype,
                          sizeof(entity->prototype));
        hash = hash_bytes(hash, &entity->parent_id,
                          sizeof(entity->parent_id));
        hash = hash_bytes(hash, &entity->birth_ordinal,
                          sizeof(entity->birth_ordinal));
        hash = hash_bytes(hash, &entity->descendants_born,
                          sizeof(entity->descendants_born));
        hash = hash_bytes(hash, &entity->fruit_ticks,
                          sizeof(entity->fruit_ticks));
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
                hash = hash_bytes(
                    hash, &override->values[value].provenance_id,
                    sizeof(override->values[value].provenance_id));
                hash = hash_bytes(
                    hash, &override->values[value].provenance_reach,
                    sizeof(override->values[value].provenance_reach));
            }
            const uint8_t has_behavior =
                override->has_behavior ? 1u : 0u;
            hash = hash_bytes(hash, &has_behavior, sizeof(has_behavior));
            if (override->has_behavior) {
                hash = hash_behavior_draft(hash, override->behavior);
                hash = hash_bytes(
                    hash, &override->behavior_provenance_id,
                    sizeof(override->behavior_provenance_id));
                hash = hash_bytes(
                    hash, &override->behavior_provenance_reach,
                    sizeof(override->behavior_provenance_reach));
            }
        }
    }
    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        const Entity *tree = &world->universe.entities[index];
        if (tree->prototype != PROTOTYPE_TREE) {
            continue;
        }
        const LineageDefinition *lineage =
            world_tree_lineage(world, tree);
        const uint8_t active = lineage != NULL ? 1u : 0u;
        hash = hash_bytes(hash, &active, sizeof(active));
        if (lineage == NULL) {
            continue;
        }
        hash = hash_bytes(hash, &lineage->progenitor_id,
                          sizeof(lineage->progenitor_id));
        hash = hash_double(hash, lineage->draft.nutrition);
        hash = hash_behavior_draft(hash, lineage->draft.behavior);
        hash = hash_bytes(hash, &lineage->inherited_births,
                          sizeof(lineage->inherited_births));
        const uint8_t flags[2] = {
            lineage->has_nutrition_patch ? 1u : 0u,
            lineage->has_behavior_patch ? 1u : 0u};
        hash = hash_bytes(hash, flags, sizeof(flags));
    }
    return hash;
}

_Static_assert(sizeof(World) <= 262144,
               "World memory contract exceeds 256 KiB");
