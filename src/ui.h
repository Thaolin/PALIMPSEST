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

typedef struct UiState {
    bool inspector_open;
    bool developer_mode;
    bool has_error;
    bool has_nourishment_draft;
    uint64_t inspected_entity_id;
    ConceptId hovered_concept;
    PaliValue nourishment_draft;
    SourceEditor editor;
    PaliError error;
} UiState;

void ui_init(UiState *ui, const char *font_path);
void ui_shutdown(void);
void ui_set_developer_mode(UiState *ui, bool enabled);
void ui_open_inspector(UiState *ui, const World *world, int entity_index);
void ui_update(UiState *ui, World *world, Vector2 virtual_mouse);
void ui_draw(const UiState *ui, const World *world, int nearby_entity,
             const char *save_hint);

#endif
