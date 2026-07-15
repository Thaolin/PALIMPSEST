#include "lexicon.h"

#include <float.h>
#include <math.h>
#include <string.h>

static const ConceptDefinition base_concepts[CONCEPT_COUNT] = {
    [CONCEPT_TAG] = {CONCEPT_TAG, "tag", PALI_VALUE_TEXT, ACCESS_DEPTH_STATE,
                     FACET_HISTORICAL, CONCEPT_OP_PROTECTED, 0.0, 0.0, 0.0},
    [CONCEPT_MASS] = {CONCEPT_MASS, "mass", PALI_VALUE_NUMBER,
                      ACCESS_DEPTH_STATE, FACET_MATERIAL, CONCEPT_OP_PROTECTED,
                      0.0, 1000000.0, 0.1},
    [CONCEPT_NUTRITION] = {CONCEPT_NUTRITION, "nutrition", PALI_VALUE_NUMBER,
                           ACCESS_DEPTH_STATE, FACET_VITAL, CONCEPT_OP_REPLACE,
                           0.0, 100.0, 1.0},
    [CONCEPT_COLOR] = {CONCEPT_COLOR, "color", PALI_VALUE_TEXT,
                       ACCESS_DEPTH_STATE, FACET_SENSORY, CONCEPT_OP_PROTECTED,
                       0.0, 0.0, 0.0},
    [CONCEPT_RIPE] = {CONCEPT_RIPE, "ripe", PALI_VALUE_BOOL,
                      ACCESS_DEPTH_STATE, FACET_VITAL, CONCEPT_OP_PROTECTED,
                      0.0, 0.0, 0.0},
    [CONCEPT_HEAT] = {CONCEPT_HEAT, "heat", PALI_VALUE_NUMBER,
                      ACCESS_DEPTH_STATE, FACET_MATERIAL, CONCEPT_OP_PROTECTED,
                      0.0, 100.0, 1.0},
    [CONCEPT_ALIVE] = {CONCEPT_ALIVE, "alive", PALI_VALUE_BOOL,
                       ACCESS_DEPTH_STATE, FACET_VITAL, CONCEPT_OP_PROTECTED,
                       0.0, 0.0, 0.0},
    [CONCEPT_FRIENDLY] = {CONCEPT_FRIENDLY, "friendly", PALI_VALUE_BOOL,
                          ACCESS_DEPTH_STATE, FACET_RELATIONAL,
                          CONCEPT_OP_PROTECTED, 0.0, 0.0, 0.0},
    [CONCEPT_EDIBLE] = {CONCEPT_EDIBLE, "edible", PALI_VALUE_BOOL,
                        ACCESS_DEPTH_STATE, FACET_VITAL, CONCEPT_OP_PROTECTED,
                        0.0, 0.0, 0.0},
    [CONCEPT_LABEL] = {CONCEPT_LABEL, "label", PALI_VALUE_TEXT,
                       ACCESS_DEPTH_STATE, FACET_HISTORICAL,
                       CONCEPT_OP_PROTECTED, 0.0, 0.0, 0.0},
    [CONCEPT_HUNGER] = {CONCEPT_HUNGER, "hunger", PALI_VALUE_NUMBER,
                        ACCESS_DEPTH_STATE, FACET_VITAL, CONCEPT_OP_PROTECTED,
                        0.0, 100.0, 1.0},
    [CONCEPT_WARMTH] = {CONCEPT_WARMTH, "warmth", PALI_VALUE_NUMBER,
                        ACCESS_DEPTH_STATE, FACET_VITAL, CONCEPT_OP_PROTECTED,
                        0.0, 100.0, 1.0},
    [CONCEPT_X] = {CONCEPT_X, "x", PALI_VALUE_NUMBER, ACCESS_DEPTH_STATE,
                   FACET_SPATIAL, CONCEPT_OP_PROTECTED, 0.0, 10000.0, 0.1},
    [CONCEPT_Y] = {CONCEPT_Y, "y", PALI_VALUE_NUMBER, ACCESS_DEPTH_STATE,
                   FACET_SPATIAL, CONCEPT_OP_PROTECTED, 0.0, 10000.0, 0.1},
};

static bool bounded_text(const char text[PALI_TEXT_CAP]) {
    for (size_t index = 0; index < PALI_TEXT_CAP; ++index) {
        if (text[index] == '\0') {
            return true;
        }
    }
    return false;
}

static bool value_matches_step(const ConceptDefinition *definition,
                               double value) {
    if (definition->numeric_step <= 0.0) {
        return true;
    }

    const double steps =
        (value - definition->numeric_min) / definition->numeric_step;
    if (steps < 0.0 || steps >= (double)UINT64_MAX) {
        return false;
    }

    const uint64_t whole_steps = (uint64_t)steps;
    const double fraction = steps - (double)whole_steps;
    const double tolerance = DBL_EPSILON * 8.0 * (steps + 1.0);
    return fraction <= tolerance || 1.0 - fraction <= tolerance;
}

const ConceptDefinition *lexicon_find_by_id(ConceptId id) {
    if (id == CONCEPT_NONE || id >= CONCEPT_COUNT) {
        return NULL;
    }
    return &base_concepts[id];
}

const ConceptDefinition *lexicon_find_by_name(const char *name) {
    if (name == NULL) {
        return NULL;
    }

    for (ConceptId id = CONCEPT_TAG; id < CONCEPT_COUNT; ++id) {
        const ConceptDefinition *definition = &base_concepts[id];
        if (strcmp(name, definition->name) == 0) {
            return definition;
        }
    }
    return NULL;
}

bool lexicon_value_is_valid(const ConceptDefinition *definition,
                            PaliValue value) {
    if (definition == NULL || value.type != definition->value_type) {
        return false;
    }

    switch (value.type) {
        case PALI_VALUE_NIL:
            return true;
        case PALI_VALUE_NUMBER:
            return isfinite(value.as.number) &&
                   value.as.number >= definition->numeric_min &&
                   value.as.number <= definition->numeric_max &&
                   value_matches_step(definition, value.as.number);
        case PALI_VALUE_BOOL:
            return true;
        case PALI_VALUE_TEXT:
            return bounded_text(value.as.text);
        default:
            return false;
    }
}

uint64_t concept_bit(ConceptId id) {
    if (id == CONCEPT_NONE || id >= CONCEPT_COUNT || id >= 64u) {
        return UINT64_C(0);
    }
    return UINT64_C(1) << id;
}

uint32_t patch_reach_bit(PatchReach reach) {
    if ((unsigned int)reach >= (unsigned int)PATCH_REACH_COUNT) {
        return UINT32_C(0);
    }
    return UINT32_C(1) << (unsigned int)reach;
}
