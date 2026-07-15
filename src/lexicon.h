#ifndef PALIMPSEST_LEXICON_H
#define PALIMPSEST_LEXICON_H

#include "pali.h"

#include <stdbool.h>
#include <stdint.h>

#define LEXICON_CAPACITY 64u

typedef uint16_t ConceptId;

enum {
    CONCEPT_NONE = 0,
    CONCEPT_TAG = 1,
    CONCEPT_MASS = 2,
    CONCEPT_NUTRITION = 3,
    CONCEPT_COLOR = 4,
    CONCEPT_RIPE = 5,
    CONCEPT_HEAT = 6,
    CONCEPT_ALIVE = 7,
    CONCEPT_FRIENDLY = 8,
    CONCEPT_EDIBLE = 9,
    CONCEPT_LABEL = 10,
    CONCEPT_HUNGER = 11,
    CONCEPT_WARMTH = 12,
    CONCEPT_X = 13,
    CONCEPT_Y = 14,
    CONCEPT_COUNT = 15
};

_Static_assert(CONCEPT_COUNT < LEXICON_CAPACITY,
               "concept IDs must fit in a concept bit");

typedef enum AccessDepth {
    ACCESS_DEPTH_STATE = 0,
    ACCESS_DEPTH_BEHAVIOR,
    ACCESS_DEPTH_LINEAGE,
    ACCESS_DEPTH_ARCHETYPE,
    ACCESS_DEPTH_LAW
} AccessDepth;

typedef enum Facet {
    FACET_SENSORY = 0,
    FACET_MATERIAL,
    FACET_VITAL,
    FACET_RELATIONAL,
    FACET_HISTORICAL,
    FACET_METAPHYSICAL,
    FACET_SPATIAL
} Facet;

typedef enum ConceptAccess {
    CONCEPT_ACCESS_UNPERCEIVED = 0,
    CONCEPT_ACCESS_VEILED,
    CONCEPT_ACCESS_READABLE,
    CONCEPT_ACCESS_PATCHABLE
} ConceptAccess;

typedef enum PatchReach {
    PATCH_REACH_ENTITY = 0,
    PATCH_REACH_LINEAGE,
    PATCH_REACH_PROTOTYPE,
    PATCH_REACH_ARCHETYPE,
    PATCH_REACH_UNIVERSE,
    PATCH_REACH_COUNT
} PatchReach;

enum {
    CONCEPT_OP_REPLACE = UINT32_C(1) << 0,
    CONCEPT_OP_PROTECTED = UINT32_C(1) << 1
};

typedef struct ConceptDefinition {
    ConceptId id;
    const char *name;
    PaliValueType value_type;
    AccessDepth depth;
    Facet facet;
    uint32_t operation_flags;
    double numeric_min;
    double numeric_max;
    double numeric_step;
} ConceptDefinition;

const ConceptDefinition *lexicon_find_by_id(ConceptId id);
const ConceptDefinition *lexicon_find_by_name(const char *name);
bool lexicon_value_is_valid(const ConceptDefinition *definition,
                            PaliValue value);

uint64_t concept_bit(ConceptId id);
uint32_t patch_reach_bit(PatchReach reach);

#endif
