#ifndef PALIMPSEST_WORLD_H
#define PALIMPSEST_WORLD_H

#include "lexicon.h"
#include "pali.h"

#include <stdbool.h>
#include <stdint.h>

#define WORLD_MAP_WIDTH 40
#define WORLD_MAP_HEIGHT 28
#define WORLD_TILE_SIZE 8
#define WORLD_MAX_ENTITIES 64
#define WORLD_INSTANCE_PROPERTIES 4
#define WORLD_MAX_LOCAL_OVERRIDES 4
#define WORLD_LOCAL_PATCH_VALUES 4
#define WORLD_MESSAGE_CAP 160

typedef enum TileKind {
    TILE_GRASS_DARK = 0,
    TILE_GRASS_LIGHT,
    TILE_FLOWERS,
    TILE_THICKET,
    TILE_WATER
} TileKind;

typedef enum PrototypeId {
    PROTOTYPE_STONE = 0,
    PROTOTYPE_TREE,
    PROTOTYPE_APPLE,
    PROTOTYPE_FIRE,
    PROTOTYPE_MOTH,
    PROTOTYPE_COUNT
} PrototypeId;

typedef struct PrototypeDefinition {
    char name[PALI_NAME_CAP];
    char default_source[PALI_SOURCE_CAP];
    char current_source[PALI_SOURCE_CAP];
    PaliDocument document;
    PaliProgram program;
    bool patched;
} PrototypeDefinition;

typedef struct Entity {
    uint64_t id;
    uint64_t rng_state;
    float x;
    float y;
    float move_x;
    float move_y;
    uint16_t direction_ticks;
    int8_t local_override;
    uint8_t prototype;
    uint8_t state_count;
    bool active;
    bool dirty;
    PaliProperty state[WORLD_INSTANCE_PROPERTIES];
} Entity;

typedef struct LocalPatchValue {
    ConceptId concept;
    PaliValue value;
} LocalPatchValue;

typedef struct LocalOverride {
    uint64_t entity_id;
    LocalPatchValue values[WORLD_LOCAL_PATCH_VALUES];
    uint8_t value_count;
    bool active;
} LocalOverride;

typedef struct UniverseState {
    uint64_t root_seed;
    uint64_t tick;
    uint8_t tiles[WORLD_MAP_HEIGHT][WORLD_MAP_WIDTH];
    Entity entities[WORLD_MAX_ENTITIES];
    uint16_t entity_count;
    PrototypeDefinition prototypes[PROTOTYPE_COUNT];
    LocalOverride local_overrides[WORLD_MAX_LOCAL_OVERRIDES];
} UniverseState;

typedef struct KnowledgeState {
    uint64_t perceived_concepts;
    uint64_t readable_concepts;
    uint64_t patchable_concepts;
    uint32_t known_notations;
    uint32_t reach_mask;
    uint8_t access_depth;
} KnowledgeState;

typedef struct EmbodimentState {
    uint64_t entity_id;
    float x;
    float y;
    float hunger;
    float warmth;
} EmbodimentState;

typedef struct World {
    UniverseState universe;
    KnowledgeState knowledge;
    EmbodimentState embodiment;
    char message[WORLD_MESSAGE_CAP];
} World;

typedef struct WorldInput {
    float move_x;
    float move_y;
} WorldInput;

bool world_init(World *world, uint64_t seed, const char *pali_asset_root,
                PaliError *error);
void world_step(World *world, WorldInput input);

int world_nearest_entity(const World *world, float maximum_distance);
Entity *world_entity_by_id(World *world, uint64_t id);
const Entity *world_entity_by_id_const(const World *world, uint64_t id);
const PaliProgram *world_entity_program(const World *world,
                                        const Entity *entity);
bool world_use_entity(World *world, int entity_index, PaliError *error);

bool world_apply_prototype_source(World *world, PrototypeId prototype,
                                  const char *source, PaliError *error);
bool world_apply_entity_value_patch(World *world, uint64_t entity_id,
                                    ConceptId concept, PaliValue value,
                                    PaliError *error);
bool world_clear_entity_value_patch(World *world, uint64_t entity_id,
                                    ConceptId concept, PaliError *error);
bool world_apply_prototype_value_patch(World *world, PrototypeId prototype,
                                       ConceptId concept, PaliValue value,
                                       PaliError *error);
const char *world_prototype_source(const World *world, PrototypeId prototype);
const char *world_prototype_name(PrototypeId prototype);
const PaliDocument *world_prototype_document(const World *world,
                                             PrototypeId prototype);

bool world_get_entity_property(const World *world, const Entity *entity,
                               const char *name, PaliValue *out);
bool world_get_entity_concept(const World *world, const Entity *entity,
                              ConceptId concept, PaliValue *out);
bool world_tile_is_blocking(uint8_t tile);

ConceptAccess world_concept_access(const World *world, ConceptId concept);
bool world_has_reach(const World *world, PatchReach reach);
void world_grant_developer_knowledge(World *world);

uint64_t world_genesis_fingerprint(const World *world);
uint64_t world_state_fingerprint(const World *world);

#endif
