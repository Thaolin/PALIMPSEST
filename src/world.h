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
#define WORLD_MAX_LOCAL_OVERRIDES 32
#define WORLD_MAX_LINEAGES 12
#define WORLD_LOCAL_PATCH_VALUES 4
#define WORLD_BEHAVIOR_PATCH_BUDGET 24
#define WORLD_FRUIT_REGROW_TICKS 300
#define WORLD_FRUIT_INFLECTION_RADIUS 2
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
    PROTOTYPE_TREE = 1,
    PROTOTYPE_APPLE = 2,
    PROTOTYPE_FIRE = 3,
    PROTOTYPE_MOTH = 4,
    PROTOTYPE_COUNT = 5
} PrototypeId;

_Static_assert(PROTOTYPE_COUNT <= 32,
               "observed Prototype kinds must fit in a 32-bit mask");
_Static_assert(CONCEPT_COUNT <= 32,
               "known concept notations must fit in a 32-bit mask");

typedef enum ObservationResult {
    OBSERVATION_REJECTED = 0,
    OBSERVATION_REPEATED,
    OBSERVATION_RECORDED,
    OBSERVATION_REVELATION,
    OBSERVATION_NOTATION
} ObservationResult;

typedef enum InquiryId {
    INQUIRY_NONE = 0,
    INQUIRY_FIRST_SCAR,
    INQUIRY_WEIGHT_OF_THINGS,
    INQUIRY_SENTENCE_INSIDE,
    INQUIRY_FRUIT_REMEMBERS,
    INQUIRY_COUNT
} InquiryId;

typedef enum KnowledgeGrant {
    KNOWLEDGE_GRANT_NONE = 0,
    KNOWLEDGE_GRANT_BEHAVIOR_DEPTH,
    KNOWLEDGE_GRANT_LINEAGE_DEPTH
} KnowledgeGrant;

typedef struct InquiryProgress {
    InquiryId id;
    uint8_t completed_steps;
    uint8_t step_count;
} InquiryProgress;

typedef enum BehaviorHungerClause {
    BEHAVIOR_HUNGER_SOOTHE = 0,
    BEHAVIOR_HUNGER_SHARPEN,
    BEHAVIOR_HUNGER_LEAVE,
    BEHAVIOR_HUNGER_COUNT
} BehaviorHungerClause;

typedef enum BehaviorVoiceClause {
    BEHAVIOR_VOICE_FADE = 0,
    BEHAVIOR_VOICE_REMEMBER,
    BEHAVIOR_VOICE_SILENT,
    BEHAVIOR_VOICE_COUNT
} BehaviorVoiceClause;

typedef enum BehaviorFateClause {
    BEHAVIOR_FATE_CEASE = 0,
    BEHAVIOR_FATE_REMAIN,
    BEHAVIOR_FATE_COUNT
} BehaviorFateClause;

typedef enum BehaviorAftertasteClause {
    BEHAVIOR_AFTERTASTE_NONE = 0,
    BEHAVIOR_AFTERTASTE_KINDLE,
    BEHAVIOR_AFTERTASTE_QUICKEN,
    BEHAVIOR_AFTERTASTE_COUNT
} BehaviorAftertasteClause;

typedef struct UseBehaviorDraft {
    BehaviorHungerClause hunger;
    BehaviorVoiceClause voice;
    BehaviorFateClause fate;
    BehaviorAftertasteClause aftertaste;
} UseBehaviorDraft;

typedef struct FruitLineageDraft {
    double nutrition;
    UseBehaviorDraft behavior;
} FruitLineageDraft;

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
    uint64_t parent_id;
    float x;
    float y;
    float move_x;
    float move_y;
    uint32_t birth_ordinal;
    uint32_t descendants_born;
    uint16_t direction_ticks;
    uint16_t fruit_ticks;
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
    uint64_t provenance_id;
    uint8_t provenance_reach;
} LocalPatchValue;

typedef struct LocalOverride {
    uint64_t entity_id;
    LocalPatchValue values[WORLD_LOCAL_PATCH_VALUES];
    UseBehaviorDraft behavior;
    uint64_t behavior_provenance_id;
    uint8_t value_count;
    uint8_t behavior_provenance_reach;
    bool has_behavior;
    bool active;
} LocalOverride;

typedef struct LineageDefinition {
    uint64_t progenitor_id;
    FruitLineageDraft draft;
    uint32_t inherited_births;
    bool has_nutrition_patch;
    bool has_behavior_patch;
    bool active;
} LineageDefinition;

typedef struct UniverseState {
    uint64_t root_seed;
    uint64_t tick;
    uint8_t tiles[WORLD_MAP_HEIGHT][WORLD_MAP_WIDTH];
    Entity entities[WORLD_MAX_ENTITIES];
    uint16_t entity_count;
    PrototypeDefinition prototypes[PROTOTYPE_COUNT];
    LocalOverride local_overrides[WORLD_MAX_LOCAL_OVERRIDES];
    LineageDefinition lineages[WORLD_MAX_LINEAGES];
} UniverseState;

typedef struct KnowledgeState {
    uint64_t perceived_concepts;
    uint64_t readable_concepts;
    uint64_t patchable_concepts;
    uint32_t known_notations;
    uint32_t observed_prototypes[CONCEPT_COUNT];
    uint32_t reach_mask;
    uint8_t access_depth;
} KnowledgeState;

typedef struct EmbodimentState {
    uint64_t entity_id;
    float x;
    float y;
    float hunger;
    float warmth;
    float vigor;
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
bool world_entity_behavior_document(const World *world, const Entity *entity,
                                    PaliDocument *out);
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
bool world_behavior_is_patchable(const World *world, const Entity *entity);
bool world_build_use_behavior_document(const World *world,
                                       const Entity *entity,
                                       UseBehaviorDraft draft,
                                       PaliDocument *out,
                                       PaliError *error);
bool world_build_apple_behavior_document(const World *world,
                                         UseBehaviorDraft draft,
                                         PaliDocument *out,
                                         PaliError *error);
bool world_get_entity_use_behavior_draft(const World *world,
                                         const Entity *entity,
                                         UseBehaviorDraft *out);
bool world_behavior_draft_from_document(const World *world,
                                        const Entity *entity,
                                        const PaliDocument *handler,
                                        UseBehaviorDraft *out,
                                        PaliError *error);
bool world_apply_entity_behavior_patch(World *world, uint64_t entity_id,
                                       const PaliDocument *handler,
                                       PaliError *error);
bool world_clear_entity_behavior_patch(World *world, uint64_t entity_id,
                                       PaliError *error);
bool world_entity_has_behavior_patch(const World *world,
                                     const Entity *entity);
PatchReach world_entity_behavior_provenance(const World *world,
                                            const Entity *entity,
                                            uint64_t *out_id);
PatchReach world_entity_concept_provenance(const World *world,
                                           const Entity *entity,
                                           ConceptId concept,
                                           uint64_t *out_id);

bool world_tree_lineage_is_patchable(const World *world,
                                     const Entity *tree);
bool world_get_tree_lineage_draft(const World *world, const Entity *tree,
                                  FruitLineageDraft *out);
bool world_apply_tree_lineage_draft(World *world, uint64_t tree_id,
                                    FruitLineageDraft draft,
                                    PaliError *error);
bool world_clear_tree_lineage_patch(World *world, uint64_t tree_id,
                                    PaliError *error);
const LineageDefinition *world_tree_lineage(const World *world,
                                            const Entity *tree);
double world_tree_next_fruit_nutrition(const World *world,
                                       const Entity *tree);
double world_tree_preview_fruit_nutrition(const World *world,
                                          const Entity *tree,
                                          FruitLineageDraft draft);
const Entity *world_tree_current_fruit(const World *world,
                                       const Entity *tree);

uint64_t world_descendant_id(const World *world, uint64_t parent_id,
                             uint32_t birth_ordinal,
                             PrototypeId prototype);
bool world_restore_descendant(World *world, uint64_t id,
                              uint64_t parent_id,
                              uint32_t birth_ordinal,
                              PrototypeId prototype, PaliError *error);
bool world_restore_lineage(World *world, LineageDefinition definition,
                           PaliError *error);
bool world_restore_local_override(World *world, LocalOverride definition,
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
ObservationResult world_observe_entity_concept(World *world,
                                               uint64_t entity_id,
                                               ConceptId concept);
uint8_t world_concept_observation_count(const World *world,
                                        ConceptId concept);
bool world_knows_exact_notation(const World *world, ConceptId concept);
bool world_has_reach(const World *world, PatchReach reach);
void world_grant_developer_knowledge(World *world);

InquiryProgress world_inquiry_progress(const World *world, InquiryId inquiry);
InquiryId world_active_inquiry(const World *world);
KnowledgeGrant world_reconcile_inquiry_knowledge(World *world);

uint64_t world_genesis_fingerprint(const World *world);
uint64_t world_state_fingerprint(const World *world);

#endif
