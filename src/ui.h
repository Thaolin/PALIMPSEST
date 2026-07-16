#ifndef PALIMPSEST_UI_H
#define PALIMPSEST_UI_H

#include "raylib.h"
#include "world.h"

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

#define PAL_VIRTUAL_WIDTH 720
#define PAL_VIRTUAL_HEIGHT 405
#define PAL_WORLD_ZOOM 1.5f
#define PAL_MAP_Y 24
#define PAL_HUD_HEIGHT 36
#define PAL_PANEL_X 480

typedef struct SourceEditor {
    char text[PALI_SOURCE_CAP];
    size_t length;
    size_t cursor;
    int scroll_line;
} SourceEditor;

typedef enum UiKnowledgeNotice {
    UI_KNOWLEDGE_NOTICE_NONE = 0,
    UI_KNOWLEDGE_NOTICE_BEHAVIOR,
    UI_KNOWLEDGE_NOTICE_GRAMMAR,
    UI_KNOWLEDGE_NOTICE_LINEAGE
} UiKnowledgeNotice;

typedef struct UiState {
    bool inspector_open;
    bool inquiry_panel_expanded;
    bool developer_mode;
    bool has_error;
    bool has_nourishment_draft;
    bool has_behavior_draft;
    bool has_lineage_draft;
    uint64_t inspected_entity_id;
    ConceptId hovered_concept;
    PaliValue nourishment_draft;
    UseBehaviorDraft behavior_draft;
    FruitLineageDraft lineage_draft;
    UiKnowledgeNotice knowledge_notice;
    uint16_t knowledge_notice_frames;
    SourceEditor editor;
    PaliError error;
} UiState;

void ui_init(UiState *ui, const char *font_path);
void ui_shutdown(void);
void ui_set_developer_mode(UiState *ui, bool enabled);
void ui_open_inspector(UiState *ui, const World *world, int entity_index);
bool ui_update_world_panel(UiState *ui, Vector2 virtual_mouse);
void ui_present_knowledge_grant(UiState *ui, KnowledgeGrant grant);
void ui_update(UiState *ui, World *world, Vector2 virtual_mouse);
void ui_draw(const UiState *ui, const World *world, int nearby_entity,
             const char *save_hint);

#endif
