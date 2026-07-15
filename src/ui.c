#include "ui.h"

#include <ctype.h>
#include <math.h>
#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static const Color INK = {35, 31, 31, 255};
static const Color INK_SOFT = {69, 58, 53, 255};
static const Color PARCHMENT = {224, 203, 157, 255};
static const Color PARCHMENT_DARK = {170, 139, 96, 255};
static const Color PAL_GOLD = {227, 178, 76, 255};
static const Color OCHRE_INK = {105, 72, 20, 255};
static const Color ERROR_RED = {202, 76, 65, 255};
static const Color ERROR_INK = {150, 42, 38, 255};
static const Color COLD_BLUE = {83, 144, 166, 255};

static Font interface_font;
static bool interface_font_ready = false;

enum {
    TYPE_CAPTION = 12,
    TYPE_BODY = 13,
    TYPE_SECTION = 15,
    TYPE_HEADING = 17,
    TYPE_TITLE = 21,
    EDITOR_CODE_X = 237,
    EDITOR_CONTENT_Y = 102,
    EDITOR_CODE_WIDTH = 469,
    EDITOR_ROW_HEIGHT = 17,
    EDITOR_VISIBLE_LINES = 15,
    EDITOR_FONT_SIZE = 15
};

static Font active_font(void) {
    return interface_font_ready ? interface_font : GetFontDefault();
}

static int readable_size(int size) {
    const int minimum = interface_font_ready ? 8 : 10;
    return size < minimum ? minimum : size;
}

static void draw_text(const char *text, int x, int y, int size, Color color) {
    const int actual_size = readable_size(size);
    DrawTextEx(active_font(), text, (Vector2){(float)x, (float)y},
               (float)actual_size, 0.0f, color);
}

static int text_width(const char *text, int size) {
    const int actual_size = readable_size(size);
    return (int)ceilf(
        MeasureTextEx(active_font(), text, (float)actual_size, 0.0f).x);
}

static void draw_text_fit(const char *value, int x, int y, int max_width,
                          int size, Color color) {
    if (value == NULL || max_width <= 0) {
        return;
    }
    if (text_width(value, size) <= max_width) {
        draw_text(value, x, y, size, color);
        return;
    }
    char clipped[256];
    (void)snprintf(clipped, sizeof(clipped), "%.252s", value);
    size_t length = strlen(clipped);
    while (length > 0) {
        clipped[length] = '\0';
        char candidate[256];
        (void)snprintf(candidate, sizeof(candidate), "%s...", clipped);
        if (text_width(candidate, size) <= max_width) {
            draw_text(candidate, x, y, size, color);
            return;
        }
        length--;
    }
    draw_text("...", x, y, size, color);
}

static Rectangle apply_button(void) {
    return (Rectangle){417.0f, 51.0f, 81.0f, 24.0f};
}

static Rectangle revert_button(void) {
    return (Rectangle){507.0f, 51.0f, 90.0f, 24.0f};
}

static Rectangle close_button(void) {
    return (Rectangle){657.0f, 6.0f, 51.0f, 24.0f};
}

static Rectangle lens_close_button(void) {
    return (Rectangle){626.0f, 8.0f, 77.0f, 25.0f};
}

static Rectangle nourishment_decrement_button(void) {
    return (Rectangle){170.0f, 222.0f, 28.0f, 28.0f};
}

static Rectangle nourishment_increment_button(void) {
    return (Rectangle){306.0f, 222.0f, 28.0f, 28.0f};
}

static Rectangle discard_button(void) {
    return (Rectangle){486.0f, 365.0f, 92.0f, 27.0f};
}

static Rectangle inscribe_button(void) {
    return (Rectangle){587.0f, 365.0f, 115.0f, 27.0f};
}

static Rectangle editor_rectangle(void) {
    return (Rectangle){207.0f, 96.0f, 501.0f, 265.0f};
}

static void editor_set(SourceEditor *editor, const char *source) {
    (void)snprintf(editor->text, sizeof(editor->text), "%s",
                   source != NULL ? source : "");
    editor->length = strlen(editor->text);
    editor->cursor = 0;
    editor->scroll_line = 0;
}

void ui_init(UiState *ui, const char *font_path) {
    memset(ui, 0, sizeof(*ui));
    memset(&interface_font, 0, sizeof(interface_font));
    interface_font_ready = false;
    if (font_path != NULL) {
        interface_font = LoadFontEx(font_path, 32, NULL, 0);
        interface_font_ready = IsFontValid(interface_font);
        if (interface_font_ready) {
            SetTextureFilter(interface_font.texture, TEXTURE_FILTER_BILINEAR);
        }
    }
}

void ui_shutdown(void) {
    if (interface_font_ready) {
        UnloadFont(interface_font);
    }
    memset(&interface_font, 0, sizeof(interface_font));
    interface_font_ready = false;
}

void ui_set_developer_mode(UiState *ui, bool enabled) {
    if (ui != NULL) {
        ui->developer_mode = enabled;
    }
}

static void capture_nourishment_draft(UiState *ui, const World *world,
                                      const Entity *entity) {
    PaliValue value;
    ui->has_nourishment_draft =
        world_get_entity_concept(world, entity, CONCEPT_NUTRITION, &value) &&
        value.type == PALI_VALUE_NUMBER && isfinite(value.as.number);
    if (ui->has_nourishment_draft) {
        ui->nourishment_draft = value;
    } else {
        memset(&ui->nourishment_draft, 0, sizeof(ui->nourishment_draft));
    }
}

void ui_open_inspector(UiState *ui, const World *world, int entity_index) {
    if (ui == NULL || world == NULL || entity_index < 0 ||
        entity_index >= (int)world->universe.entity_count) {
        return;
    }
    const Entity *entity = &world->universe.entities[entity_index];
    ui->inspector_open = true;
    ui->inspected_entity_id = entity->id;
    ui->has_error = false;
    memset(&ui->error, 0, sizeof(ui->error));
    capture_nourishment_draft(ui, world, entity);
    editor_set(&ui->editor,
               world_prototype_source(world,
                                      (PrototypeId)entity->prototype));
}

static void ui_error(UiState *ui, int line, int column, const char *message) {
    ui->has_error = true;
    ui->error.line = line;
    ui->error.column = column;
    (void)snprintf(ui->error.message, sizeof(ui->error.message), "%s", message);
}

static int editor_cursor_line(const SourceEditor *editor) {
    int line = 0;
    for (size_t index = 0; index < editor->cursor; ++index) {
        if (editor->text[index] == '\n') {
            line++;
        }
    }
    return line;
}

static size_t line_start_before(const SourceEditor *editor, size_t position) {
    while (position > 0 && editor->text[position - 1] != '\n') {
        position--;
    }
    return position;
}

static size_t line_end_after(const SourceEditor *editor, size_t position) {
    while (position < editor->length && editor->text[position] != '\n') {
        position++;
    }
    return position;
}

static bool editor_line_bounds(const SourceEditor *editor, int target_line,
                               size_t *out_start, size_t *out_end) {
    if (target_line < 0) {
        return false;
    }
    int line = 0;
    size_t start = 0;
    while (line < target_line) {
        const size_t end = line_end_after(editor, start);
        if (end >= editor->length) {
            return false;
        }
        start = end + 1;
        line++;
    }
    *out_start = start;
    *out_end = line_end_after(editor, start);
    return true;
}

static void editor_vertical(SourceEditor *editor, int direction) {
    const size_t start = line_start_before(editor, editor->cursor);
    const size_t column = editor->cursor - start;
    if (direction < 0) {
        if (start == 0) {
            return;
        }
        const size_t previous_end = start - 1;
        const size_t previous_start = line_start_before(editor, previous_end);
        const size_t previous_length = previous_end - previous_start;
        editor->cursor = previous_start +
                         (column < previous_length ? column : previous_length);
    } else {
        const size_t end = line_end_after(editor, editor->cursor);
        if (end >= editor->length) {
            return;
        }
        const size_t next_start = end + 1;
        const size_t next_end = line_end_after(editor, next_start);
        const size_t next_length = next_end - next_start;
        editor->cursor =
            next_start + (column < next_length ? column : next_length);
    }
}

static bool editor_insert(SourceEditor *editor, char value) {
    if (editor->length + 1 >= sizeof(editor->text)) {
        return false;
    }
    memmove(editor->text + editor->cursor + 1,
            editor->text + editor->cursor,
            editor->length - editor->cursor + 1);
    editor->text[editor->cursor++] = value;
    editor->length++;
    return true;
}

static void editor_backspace(SourceEditor *editor) {
    if (editor->cursor == 0) {
        return;
    }
    memmove(editor->text + editor->cursor - 1,
            editor->text + editor->cursor,
            editor->length - editor->cursor + 1);
    editor->cursor--;
    editor->length--;
}

static void editor_delete(SourceEditor *editor) {
    if (editor->cursor >= editor->length) {
        return;
    }
    memmove(editor->text + editor->cursor,
            editor->text + editor->cursor + 1,
            editor->length - editor->cursor);
    editor->length--;
}

static void keep_cursor_visible(SourceEditor *editor) {
    const int line = editor_cursor_line(editor);
    const int visible_lines = EDITOR_VISIBLE_LINES;
    if (line < editor->scroll_line) {
        editor->scroll_line = line;
    } else if (line >= editor->scroll_line + visible_lines) {
        editor->scroll_line = line - visible_lines + 1;
    }
}

static float editor_glyph_advance(void) {
    const float advance =
        MeasureTextEx(active_font(), "M", (float)EDITOR_FONT_SIZE, 0.0f).x;
    return advance > 0.0f ? advance : 1.0f;
}

static float editor_horizontal_offset(const SourceEditor *editor) {
    const size_t start = line_start_before(editor, editor->cursor);
    const size_t column = editor->cursor - start;
    const float cursor_width = (float)column * editor_glyph_advance();
    return cursor_width > (float)(EDITOR_CODE_WIDTH - 10)
               ? cursor_width - (float)(EDITOR_CODE_WIDTH - 10)
               : 0.0f;
}

static void editor_place_cursor(SourceEditor *editor, Vector2 mouse) {
    const int row =
        (int)((mouse.y - (float)EDITOR_CONTENT_Y) /
              (float)EDITOR_ROW_HEIGHT);
    if (row < 0 || row >= EDITOR_VISIBLE_LINES) {
        return;
    }
    size_t start = 0;
    size_t end = 0;
    if (!editor_line_bounds(editor, editor->scroll_line + row, &start, &end)) {
        return;
    }
    const float local_x = mouse.x - (float)EDITOR_CODE_X +
                          editor_horizontal_offset(editor);
    int column = (int)floorf(local_x / editor_glyph_advance() + 0.5f);
    if (column < 0) {
        column = 0;
    }
    const size_t line_length = end - start;
    if ((size_t)column > line_length) {
        column = (int)line_length;
    }
    editor->cursor = start + (size_t)column;
    keep_cursor_visible(editor);
}

static void handle_editor_input(UiState *ui) {
    SourceEditor *editor = &ui->editor;
    int character = GetCharPressed();
    while (character > 0) {
        if (character >= 32 && character <= 126 &&
            !editor_insert(editor, (char)character)) {
            ui_error(ui, editor_cursor_line(editor) + 1, 1,
                     "editor reached its 4095-byte source limit");
        }
        character = GetCharPressed();
    }
    if (IsKeyPressed(KEY_ENTER) && !editor_insert(editor, '\n')) {
        ui_error(ui, editor_cursor_line(editor) + 1, 1,
                 "editor reached its 4095-byte source limit");
    }
    if (IsKeyPressed(KEY_TAB)) {
        for (int index = 0; index < 4; ++index) {
            if (!editor_insert(editor, ' ')) {
                ui_error(ui, editor_cursor_line(editor) + 1, 1,
                         "editor reached its 4095-byte source limit");
                break;
            }
        }
    }
    if (IsKeyPressed(KEY_BACKSPACE)) {
        editor_backspace(editor);
    }
    if (IsKeyPressed(KEY_DELETE)) {
        editor_delete(editor);
    }
    if (IsKeyPressed(KEY_LEFT) && editor->cursor > 0) {
        editor->cursor--;
    }
    if (IsKeyPressed(KEY_RIGHT) && editor->cursor < editor->length) {
        editor->cursor++;
    }
    if (IsKeyPressed(KEY_UP)) {
        editor_vertical(editor, -1);
    }
    if (IsKeyPressed(KEY_DOWN)) {
        editor_vertical(editor, 1);
    }
    if (IsKeyPressed(KEY_HOME)) {
        editor->cursor = line_start_before(editor, editor->cursor);
    }
    if (IsKeyPressed(KEY_END)) {
        editor->cursor = line_end_after(editor, editor->cursor);
    }
    keep_cursor_visible(editor);
}

static void apply_source(UiState *ui, World *world, const Entity *entity) {
    PaliError error;
    if (world_apply_prototype_source(world,
                                     (PrototypeId)entity->prototype,
                                     ui->editor.text, &error)) {
        ui->has_error = false;
        memset(&ui->error, 0, sizeof(ui->error));
        capture_nourishment_draft(ui, world, entity);
    } else {
        ui->error = error;
        ui->has_error = true;
    }
}

static void update_developer_inspector(UiState *ui, World *world,
                                       Entity *entity,
                                       Vector2 virtual_mouse) {
    const bool control = IsKeyDown(KEY_LEFT_CONTROL) ||
                         IsKeyDown(KEY_RIGHT_CONTROL);
    const bool mouse_click = IsMouseButtonPressed(MOUSE_BUTTON_LEFT);
    const bool mouse_over_editor =
        CheckCollisionPointRec(virtual_mouse, editor_rectangle());
    SetMouseCursor(mouse_over_editor ? MOUSE_CURSOR_IBEAM :
                                       MOUSE_CURSOR_DEFAULT);
    if (IsKeyPressed(KEY_ESCAPE) ||
        (mouse_click && CheckCollisionPointRec(virtual_mouse, close_button()))) {
        ui->inspector_open = false;
        SetMouseCursor(MOUSE_CURSOR_DEFAULT);
        return;
    }
    if ((control && IsKeyPressed(KEY_ENTER)) ||
        (mouse_click && CheckCollisionPointRec(virtual_mouse, apply_button()))) {
        apply_source(ui, world, entity);
        return;
    }
    if ((control && IsKeyPressed(KEY_R)) ||
        (mouse_click && CheckCollisionPointRec(virtual_mouse, revert_button()))) {
        editor_set(&ui->editor,
                   world_prototype_source(world,
                                          (PrototypeId)entity->prototype));
        ui->has_error = false;
        return;
    }
    if (mouse_click && mouse_over_editor) {
        editor_place_cursor(&ui->editor, virtual_mouse);
    }
    if (!control) {
        handle_editor_input(ui);
    }
}

static bool nourishment_is_patchable(const UiState *ui, const World *world) {
    return ui->has_nourishment_draft &&
           world_concept_access(world, CONCEPT_NUTRITION) ==
               CONCEPT_ACCESS_PATCHABLE;
}

static void adjust_nourishment_draft(UiState *ui, int direction) {
    const ConceptDefinition *definition =
        lexicon_find_by_id(CONCEPT_NUTRITION);
    if (definition == NULL || !ui->has_nourishment_draft ||
        ui->nourishment_draft.type != PALI_VALUE_NUMBER) {
        return;
    }
    const double step = definition->numeric_step > 0.0
                            ? definition->numeric_step
                            : 1.0;
    double candidate = ui->nourishment_draft.as.number +
                       (double)direction * step;
    if (candidate < definition->numeric_min) {
        candidate = definition->numeric_min;
    } else if (candidate > definition->numeric_max) {
        candidate = definition->numeric_max;
    }
    ui->nourishment_draft = pali_number(candidate);
    ui->has_error = false;
    memset(&ui->error, 0, sizeof(ui->error));
}

static void inscribe_nourishment(UiState *ui, World *world, Entity *entity) {
    PaliError error;
    memset(&error, 0, sizeof(error));
    if (world_apply_entity_value_patch(world, entity->id,
                                       CONCEPT_NUTRITION,
                                       ui->nourishment_draft, &error)) {
        ui->has_error = false;
        memset(&ui->error, 0, sizeof(ui->error));
        capture_nourishment_draft(ui, world, entity);
    } else {
        ui->has_error = true;
        ui->error = error;
    }
}

static bool point_over_lens_control(Vector2 point, bool patchable) {
    if (CheckCollisionPointRec(point, lens_close_button())) {
        return true;
    }
    return patchable &&
           (CheckCollisionPointRec(point,
                                   nourishment_decrement_button()) ||
            CheckCollisionPointRec(point,
                                   nourishment_increment_button()) ||
            CheckCollisionPointRec(point, discard_button()) ||
            CheckCollisionPointRec(point, inscribe_button()));
}

static void update_lens(UiState *ui, World *world, Entity *entity,
                        Vector2 virtual_mouse) {
    const bool patchable = nourishment_is_patchable(ui, world);
    const bool control = IsKeyDown(KEY_LEFT_CONTROL) ||
                         IsKeyDown(KEY_RIGHT_CONTROL);
    const bool mouse_click = IsMouseButtonPressed(MOUSE_BUTTON_LEFT);
    SetMouseCursor(point_over_lens_control(virtual_mouse, patchable)
                       ? MOUSE_CURSOR_POINTING_HAND
                       : MOUSE_CURSOR_DEFAULT);

    if (IsKeyPressed(KEY_ESCAPE) ||
        (mouse_click && CheckCollisionPointRec(virtual_mouse,
                                               lens_close_button()))) {
        ui->inspector_open = false;
        SetMouseCursor(MOUSE_CURSOR_DEFAULT);
        return;
    }
    if (!patchable) {
        return;
    }
    if (mouse_click &&
        CheckCollisionPointRec(virtual_mouse,
                               nourishment_decrement_button())) {
        adjust_nourishment_draft(ui, -1);
        return;
    }
    if (mouse_click &&
        CheckCollisionPointRec(virtual_mouse,
                               nourishment_increment_button())) {
        adjust_nourishment_draft(ui, 1);
        return;
    }
    if ((control && IsKeyPressed(KEY_R)) ||
        (mouse_click && CheckCollisionPointRec(virtual_mouse,
                                               discard_button()))) {
        capture_nourishment_draft(ui, world, entity);
        ui->has_error = false;
        memset(&ui->error, 0, sizeof(ui->error));
        return;
    }
    if ((control && IsKeyPressed(KEY_ENTER)) ||
        (mouse_click && CheckCollisionPointRec(virtual_mouse,
                                               inscribe_button()))) {
        inscribe_nourishment(ui, world, entity);
    }
}

void ui_update(UiState *ui, World *world, Vector2 virtual_mouse) {
    if (ui == NULL || world == NULL || !ui->inspector_open) {
        return;
    }
    Entity *entity = world_entity_by_id(world, ui->inspected_entity_id);
    if (entity == NULL) {
        ui->inspector_open = false;
        SetMouseCursor(MOUSE_CURSOR_DEFAULT);
        return;
    }
    if (ui->developer_mode) {
        update_developer_inspector(ui, world, entity, virtual_mouse);
    } else {
        update_lens(ui, world, entity, virtual_mouse);
    }
}

static Color parse_color(const World *world, const Entity *entity,
                         Color fallback) {
    PaliValue value;
    if (!world_get_entity_concept(world, entity, CONCEPT_COLOR, &value) ||
        value.type != PALI_VALUE_TEXT || strlen(value.as.text) != 6) {
        return fallback;
    }
    char *end = NULL;
    const unsigned long rgb = strtoul(value.as.text, &end, 16);
    if (end == NULL || *end != '\0' || rgb > UINT32_C(0xffffff)) {
        return (Color){210, 66, 190, 255};
    }
    return (Color){(unsigned char)((rgb >> 16) & 0xffu),
                   (unsigned char)((rgb >> 8) & 0xffu),
                   (unsigned char)(rgb & 0xffu), 255};
}

static float numeric_property(const World *world, const Entity *entity,
                              ConceptId concept, float fallback) {
    PaliValue value;
    if (world_get_entity_concept(world, entity, concept, &value) &&
        value.type == PALI_VALUE_NUMBER && isfinite(value.as.number)) {
        return (float)value.as.number;
    }
    return fallback;
}

static void draw_tiles(const World *world) {
    const Color colors[] = {{75, 91, 58, 255}, {86, 104, 65, 255},
                            {93, 108, 62, 255}, {42, 55, 38, 255},
                            {43, 77, 88, 255}};
    for (int y = 0; y < WORLD_MAP_HEIGHT; ++y) {
        for (int x = 0; x < WORLD_MAP_WIDTH; ++x) {
            const uint8_t tile = world->universe.tiles[y][x];
            const int draw_x = x * WORLD_TILE_SIZE;
            const int draw_y = PAL_MAP_Y + y * WORLD_TILE_SIZE;
            DrawRectangle(draw_x, draw_y, WORLD_TILE_SIZE, WORLD_TILE_SIZE,
                          colors[tile < 5 ? tile : 0]);
            if (tile == TILE_FLOWERS && ((x + y) & 1) == 0) {
                DrawPixel(draw_x + 2, draw_y + 3, PAL_GOLD);
                DrawPixel(draw_x + 5, draw_y + 6, PARCHMENT);
            } else if (tile == TILE_WATER) {
                const int wave = (int)((world->universe.tick / 20u +
                                        (uint64_t)(x + y)) % 5u);
                DrawLine(draw_x + wave, draw_y + 5, draw_x + wave + 2,
                         draw_y + 5, (Color){91, 136, 143, 180});
            } else if (tile == TILE_THICKET && ((x * 3 + y) & 3) == 0) {
                DrawPixel(draw_x + 4, draw_y + 2, (Color){85, 105, 58, 255});
            }
        }
    }
}

static void draw_firelight(const World *world) {
    BeginBlendMode(BLEND_ADDITIVE);
    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        const Entity *entity = &world->universe.entities[index];
        if (!entity->active || entity->prototype != PROTOTYPE_FIRE) {
            continue;
        }
        Color color = parse_color(world, entity, (Color){239, 139, 69, 255});
        const float heat =
            numeric_property(world, entity, CONCEPT_HEAT, 18.0f);
        const float radius = fminf(58.0f, fmaxf(8.0f, heat + 8.0f));
        color.a = 34;
        Color outer = color;
        outer.a = 0;
        DrawCircleGradient((Vector2){entity->x, entity->y + PAL_MAP_Y}, radius,
                           color, outer);
    }
    EndBlendMode();
}

static void draw_entity(const World *world, const Entity *entity,
                        bool selected) {
    if (!entity->active) {
        return;
    }
    const int x = (int)entity->x;
    const int y = (int)entity->y + PAL_MAP_Y;
    const Color color = parse_color(world, entity, PARCHMENT_DARK);
    if (selected) {
        DrawCircleLines(x, y, 6.0f, PAL_GOLD);
        draw_text("<>", x - 4, y - 12, 6, PAL_GOLD);
    }
    switch ((PrototypeId)entity->prototype) {
        case PROTOTYPE_STONE:
            DrawCircle(x, y + 1, 4.0f, color);
            DrawLine(x - 2, y, x + 2, y - 2, Fade(PARCHMENT, 0.45f));
            break;
        case PROTOTYPE_TREE:
            DrawRectangle(x - 1, y, 3, 7, (Color){91, 61, 40, 255});
            DrawCircle(x, y - 2, 6.0f, color);
            DrawCircle(x - 4, y, 4.0f, color);
            DrawCircle(x + 4, y, 4.0f, color);
            draw_text(";", x - 1, y - 7, 6, Fade(PARCHMENT, 0.45f));
            break;
        case PROTOTYPE_APPLE:
            DrawCircle(x, y, 3.0f, color);
            DrawPixel(x - 1, y - 1, (Color){255, 203, 143, 255});
            DrawLine(x, y - 3, x + 1, y - 5, (Color){76, 52, 35, 255});
            DrawPixel(x + 2, y - 4, (Color){92, 126, 63, 255});
            break;
        case PROTOTYPE_FIRE:
            DrawTriangle((Vector2){(float)x, (float)(y - 6)},
                         (Vector2){(float)(x - 5), (float)(y + 4)},
                         (Vector2){(float)(x + 5), (float)(y + 4)}, color);
            DrawTriangle((Vector2){(float)x, (float)(y - 2)},
                         (Vector2){(float)(x - 2), (float)(y + 4)},
                         (Vector2){(float)(x + 2), (float)(y + 4)},
                         (Color){255, 221, 130, 255});
            DrawLine(x - 5, y + 5, x + 5, y + 5,
                     (Color){81, 55, 42, 255});
            break;
        case PROTOTYPE_MOTH: {
            const int flutter = (int)((world->universe.tick / 8u) & 1u);
            DrawTriangle((Vector2){(float)x, (float)y},
                         (Vector2){(float)(x - 5), (float)(y - 2 - flutter)},
                         (Vector2){(float)(x - 3), (float)(y + 3)}, color);
            DrawTriangle((Vector2){(float)x, (float)y},
                         (Vector2){(float)(x + 5), (float)(y - 2 - flutter)},
                         (Vector2){(float)(x + 3), (float)(y + 3)}, color);
            DrawPixel(x, y, INK);
            break;
        }
        default:
            break;
    }
}

static void draw_player(const World *world) {
    const int x = (int)world->embodiment.x;
    const int y = (int)world->embodiment.y + PAL_MAP_Y;
    DrawCircle(x, y - 4, 3.0f, (Color){202, 169, 123, 255});
    DrawTriangle((Vector2){(float)x, (float)(y - 2)},
                 (Vector2){(float)(x - 5), (float)(y + 7)},
                 (Vector2){(float)(x + 5), (float)(y + 7)},
                 (Color){76, 57, 76, 255});
    draw_text("=", x - 2, y, 6, PAL_GOLD);
}

static int draw_bar(int x, int y, int width, float value, Color color,
                    const char *label) {
    const int label_width = text_width(label, TYPE_CAPTION);
    const int bar_x = x + label_width + 5;
    draw_text(label, x, y, TYPE_CAPTION, PARCHMENT);
    DrawRectangle(bar_x, y + 2, width, 9, Fade(INK, 0.8f));
    DrawRectangle(bar_x + 2, y + 4,
                  (int)((float)(width - 4) * value / 100.0f), 5, color);
    return label_width + 5 + width;
}

static void value_text(PaliValue value, char *out, size_t capacity) {
    switch (value.type) {
        case PALI_VALUE_NUMBER:
            (void)snprintf(out, capacity, "%.4g", value.as.number);
            break;
        case PALI_VALUE_BOOL:
            (void)snprintf(out, capacity, "%s",
                           value.as.boolean ? "true" : "false");
            break;
        case PALI_VALUE_TEXT:
            (void)snprintf(out, capacity, "%.11s", value.as.text);
            break;
        default:
            (void)snprintf(out, capacity, "nil");
            break;
    }
}

static void draw_button(Rectangle rectangle, const char *label, Color accent) {
    DrawRectangleRec(rectangle, Fade(INK, 0.86f));
    DrawRectangleLinesEx(rectangle, 1.0f, accent);
    draw_text(label, (int)rectangle.x + 8, (int)rectangle.y + 5,
              TYPE_BODY, accent);
}

static void draw_editor(const SourceEditor *editor, uint64_t tick) {
    const Rectangle rectangle = editor_rectangle();
    const Font font = active_font();
    const float font_size = (float)EDITOR_FONT_SIZE;
    const float horizontal_offset = editor_horizontal_offset(editor);
    DrawRectangleRec(rectangle, (Color){29, 29, 31, 255});
    DrawRectangleLinesEx(rectangle, 1.0f, PARCHMENT_DARK);

    int line_number = 0;
    size_t line_start = 0;
    int visible_row = 0;
    const int cursor_line = editor_cursor_line(editor);
    const size_t cursor_line_start =
        line_start_before(editor, editor->cursor);
    const size_t cursor_column = editor->cursor - cursor_line_start;
    const float cursor_width =
        (float)cursor_column * editor_glyph_advance();

    while (line_start <= editor->length &&
           visible_row < EDITOR_VISIBLE_LINES) {
        size_t line_end = line_start;
        while (line_end < editor->length && editor->text[line_end] != '\n') {
            line_end++;
        }
        if (line_number >= editor->scroll_line) {
            char line[512];
            size_t count = line_end - line_start;
            if (count >= sizeof(line)) {
                count = sizeof(line) - 1;
            }
            memcpy(line, editor->text + line_start, count);
            line[count] = '\0';
            char number[8];
            (void)snprintf(number, sizeof(number), "%2d", line_number + 1);
            const int y = EDITOR_CONTENT_Y +
                          visible_row * EDITOR_ROW_HEIGHT;
            if (line_number == cursor_line) {
                DrawRectangle((int)rectangle.x + 1, y - 1,
                              (int)rectangle.width - 2, EDITOR_ROW_HEIGHT,
                              Fade(PAL_GOLD, 0.10f));
            }
            draw_text(number, 211, y, TYPE_CAPTION,
                      Fade(PARCHMENT_DARK, 0.9f));
            BeginScissorMode(EDITOR_CODE_X, (int)rectangle.y + 1,
                             EDITOR_CODE_WIDTH, (int)rectangle.height - 2);
            DrawTextEx(font, line,
                       (Vector2){(float)EDITOR_CODE_X - horizontal_offset,
                                 (float)y},
                       font_size, 0.0f, (Color){235, 229, 205, 255});
            if (line_number == cursor_line) {
                const float caret_x = (float)EDITOR_CODE_X + cursor_width -
                                      horizontal_offset;
                const Color caret = ((tick / 30u) & 1u) == 0u
                                        ? PAL_GOLD
                                        : Fade(PAL_GOLD, 0.55f);
                DrawRectangle((int)caret_x, y, 3, EDITOR_FONT_SIZE, caret);
                DrawRectangle((int)caret_x, y + EDITOR_FONT_SIZE - 2, 7, 2,
                              caret);
            }
            EndScissorMode();
            visible_row++;
        }
        line_number++;
        if (line_end >= editor->length) {
            break;
        }
        line_start = line_end + 1;
    }
}

typedef struct ClauseWriter {
    char *text;
    size_t capacity;
    size_t length;
} ClauseWriter;

static void clause_writer_init(ClauseWriter *writer, char *text,
                               size_t capacity) {
    writer->text = text;
    writer->capacity = capacity;
    writer->length = 0;
    if (capacity > 0) {
        text[0] = '\0';
    }
}

static void clause_writer_append(ClauseWriter *writer, const char *format,
                                 ...) {
    if (writer->capacity == 0 || writer->length >= writer->capacity - 1) {
        return;
    }
    va_list arguments;
    va_start(arguments, format);
    const size_t available = writer->capacity - writer->length;
    const int written =
        vsnprintf(writer->text + writer->length, available, format,
                  arguments);
    va_end(arguments);
    if (written < 0) {
        return;
    }
    if ((size_t)written >= available) {
        writer->length = writer->capacity - 1;
    } else {
        writer->length += (size_t)written;
    }
}

static void clause_value(ClauseWriter *writer, PaliValue value) {
    switch (value.type) {
        case PALI_VALUE_NUMBER:
            clause_writer_append(writer, "%.4g", value.as.number);
            break;
        case PALI_VALUE_BOOL:
            clause_writer_append(writer, "%s",
                                 value.as.boolean ? "true" : "false");
            break;
        case PALI_VALUE_TEXT:
            clause_writer_append(writer, "\"%s\"", value.as.text);
            break;
        default:
            clause_writer_append(writer, "nothing");
            break;
    }
}

static void clause_expression(ClauseWriter *writer,
                              const PaliDocument *document, uint8_t index,
                              int depth) {
    if (document == NULL || depth > PALI_MAX_EXPRESSIONS ||
        index == PALI_NODE_NONE || index >= document->expression_count) {
        clause_writer_append(writer, "{ unresolved }");
        return;
    }
    const PaliExpression *expression = &document->expressions[index];
    switch ((PaliExpressionKind)expression->kind) {
        case PALI_EXPRESSION_LITERAL:
            if (expression->operand < document->constant_count) {
                clause_value(writer,
                             document->constants[expression->operand]);
            } else {
                clause_writer_append(writer, "{ unresolved }");
            }
            break;
        case PALI_EXPRESSION_GET_SELF:
        case PALI_EXPRESSION_GET_ACTOR:
            if (expression->operand < document->name_count) {
                clause_writer_append(
                    writer, "%s %s",
                    expression->kind == PALI_EXPRESSION_GET_SELF ? "this"
                                                                 : "their",
                    document->names[expression->operand]);
            } else {
                clause_writer_append(writer, "{ unresolved }");
            }
            break;
        case PALI_EXPRESSION_NEGATE:
            clause_writer_append(writer, "negative ");
            clause_expression(writer, document, expression->left, depth + 1);
            break;
        case PALI_EXPRESSION_MIN:
        case PALI_EXPRESSION_MAX:
            clause_writer_append(
                writer, "the %s of ",
                expression->kind == PALI_EXPRESSION_MIN ? "lesser"
                                                         : "greater");
            clause_expression(writer, document, expression->left, depth + 1);
            clause_writer_append(writer, " and ");
            clause_expression(writer, document, expression->right, depth + 1);
            break;
        case PALI_EXPRESSION_ADD:
        case PALI_EXPRESSION_SUBTRACT:
        case PALI_EXPRESSION_MULTIPLY:
        case PALI_EXPRESSION_DIVIDE: {
            const char *operation = "plus";
            if (expression->kind == PALI_EXPRESSION_SUBTRACT) {
                operation = "minus";
            } else if (expression->kind == PALI_EXPRESSION_MULTIPLY) {
                operation = "times";
            } else if (expression->kind == PALI_EXPRESSION_DIVIDE) {
                operation = "divided by";
            }
            clause_writer_append(writer, "(");
            clause_expression(writer, document, expression->left, depth + 1);
            clause_writer_append(writer, " %s ", operation);
            clause_expression(writer, document, expression->right, depth + 1);
            clause_writer_append(writer, ")");
            break;
        }
        default:
            clause_writer_append(writer, "{ unresolved }");
            break;
    }
}

static void clause_statement_text(const PaliDocument *document,
                                  const PaliStatement *statement, char *out,
                                  size_t capacity) {
    ClauseWriter writer;
    clause_writer_init(&writer, out, capacity);
    if (document == NULL || statement == NULL) {
        clause_writer_append(&writer, "{ unresolved clause }");
        return;
    }
    switch ((PaliStatementKind)statement->kind) {
        case PALI_STATEMENT_SET_SELF:
        case PALI_STATEMENT_SET_ACTOR:
            if (statement->name >= document->name_count) {
                clause_writer_append(&writer, "{ unresolved clause }");
                return;
            }
            clause_writer_append(
                &writer, "set %s %s to ",
                statement->kind == PALI_STATEMENT_SET_SELF ? "this"
                                                           : "their",
                document->names[statement->name]);
            clause_expression(&writer, document, statement->expression, 0);
            break;
        case PALI_STATEMENT_DESTROY_SELF:
            clause_writer_append(&writer, "this entity ceases to be");
            break;
        case PALI_STATEMENT_MESSAGE:
            clause_writer_append(&writer, "reveal ");
            clause_expression(&writer, document, statement->expression, 0);
            break;
        default:
            clause_writer_append(&writer, "{ unresolved clause }");
            break;
    }
}

static void uppercase_copy(char *out, size_t capacity, const char *text) {
    if (capacity == 0) {
        return;
    }
    size_t index = 0;
    while (text != NULL && text[index] != '\0' && index + 1 < capacity) {
        out[index] = (char)toupper((unsigned char)text[index]);
        index++;
    }
    out[index] = '\0';
}

static void draw_access_label(ConceptAccess access, int x, int y) {
    const char *label = access == CONCEPT_ACCESS_PATCHABLE
                            ? "PATCHABLE"
                            : "READABLE";
    draw_text(label, x, y, TYPE_CAPTION,
              access == CONCEPT_ACCESS_PATCHABLE ? OCHRE_INK : INK_SOFT);
}

static void draw_veiled_value(int y) {
    draw_text("{ ? }", 170, y, 16, PARCHMENT_DARK);
    draw_text("VEILED", 276, y + 2, TYPE_CAPTION, INK_SOFT);
}

static void draw_static_concept(const ConceptDefinition *definition,
                                ConceptAccess access, PaliValue value,
                                int y, bool color_swatch) {
    if (access == CONCEPT_ACCESS_VEILED) {
        draw_veiled_value(y);
        return;
    }
    if (definition == NULL) {
        return;
    }
    draw_text_fit(definition->name, 26, y + 2, 125, TYPE_BODY,
                  INK_SOFT);
    if (color_swatch && value.type == PALI_VALUE_TEXT &&
        strlen(value.as.text) == 6) {
        char *end = NULL;
        const unsigned long rgb = strtoul(value.as.text, &end, 16);
        if (end != NULL && *end == '\0' && rgb <= UINT32_C(0xffffff)) {
            const Color swatch = {
                (unsigned char)((rgb >> 16) & 0xffu),
                (unsigned char)((rgb >> 8) & 0xffu),
                (unsigned char)(rgb & 0xffu), 255};
            DrawRectangle(170, y, 20, 20, swatch);
            DrawRectangleLines(170, y, 20, 20, INK_SOFT);
            char color_text[16];
            (void)snprintf(color_text, sizeof(color_text), "#%s",
                           value.as.text);
            draw_text(color_text, 198, y + 2, TYPE_BODY, INK);
        } else {
            draw_text("{ unresolved }", 170, y + 2, TYPE_BODY,
                      ERROR_INK);
        }
    } else if (value.type == PALI_VALUE_BOOL) {
        char state[48];
        (void)snprintf(state, sizeof(state), "%s%s",
                       value.as.boolean ? "" : "not ", definition->name);
        draw_text_fit(state, 170, y + 2, 100, TYPE_BODY, INK);
    } else {
        char text[96];
        value_text(value, text, sizeof(text));
        draw_text_fit(text, 170, y + 2, 100, TYPE_BODY, INK);
    }
    draw_access_label(access, 276, y + 3);
}

static void draw_nourishment_concept(const UiState *ui,
                                     const ConceptDefinition *definition,
                                     ConceptAccess access, PaliValue resolved,
                                     int y) {
    if (access == CONCEPT_ACCESS_VEILED) {
        draw_veiled_value(y);
        return;
    }
    if (definition == NULL || resolved.type != PALI_VALUE_NUMBER) {
        return;
    }
    draw_text_fit(definition->name, 26, y + 7, 125, TYPE_BODY,
                  INK_SOFT);
    if (access != CONCEPT_ACCESS_PATCHABLE ||
        !ui->has_nourishment_draft) {
        char value[32];
        (void)snprintf(value, sizeof(value), "%.4g", resolved.as.number);
        draw_text(value, 170, y + 7, TYPE_SECTION, INK);
        draw_access_label(access, 276, y + 8);
        return;
    }

    draw_button(nourishment_decrement_button(), "-", PAL_GOLD);
    DrawRectangleRec((Rectangle){204.0f, (float)y, 96.0f, 28.0f},
                     Fade(INK, 0.10f));
    DrawRectangleLinesEx((Rectangle){204.0f, (float)y, 96.0f, 28.0f}, 1.0f,
                         PARCHMENT_DARK);
    char value[32];
    (void)snprintf(value, sizeof(value), "%.4g",
                   ui->nourishment_draft.as.number);
    const int value_x = 252 - text_width(value, TYPE_SECTION) / 2;
    draw_text(value, value_x, y + 6, TYPE_SECTION, INK);
    draw_button(nourishment_increment_button(), "+", PAL_GOLD);

    const double extent = definition->numeric_max - definition->numeric_min;
    double fraction = extent > 0.0
                          ? (ui->nourishment_draft.as.number -
                             definition->numeric_min) /
                                extent
                          : 0.0;
    if (fraction < 0.0) {
        fraction = 0.0;
    } else if (fraction > 1.0) {
        fraction = 1.0;
    }
    DrawRectangle(170, y + 33, 164, 7, Fade(INK, 0.18f));
    DrawRectangle(170, y + 33, (int)(164.0 * fraction), 7, PAL_GOLD);
    draw_text("PATCHABLE", 256, y - 16, TYPE_CAPTION, OCHRE_INK);
}

static bool visible_concept_value(const World *world, const Entity *entity,
                                  ConceptId concept, ConceptAccess *access,
                                  PaliValue *value) {
    *access = world_concept_access(world, concept);
    return *access != CONCEPT_ACCESS_UNPERCEIVED &&
           world_get_entity_concept(world, entity, concept, value);
}

static void draw_text_wrapped_two_lines(const char *text, int x, int y,
                                        int max_width, int size,
                                        Color color) {
    if (text == NULL || text_width(text, size) <= max_width) {
        draw_text(text != NULL ? text : "", x, y, size, color);
        return;
    }
    char buffer[256];
    (void)snprintf(buffer, sizeof(buffer), "%.255s", text);
    const size_t length = strlen(buffer);
    size_t split = 0;
    size_t last_fit = 0;
    for (size_t index = 0; index < length; ++index) {
        if (buffer[index] != ' ') {
            continue;
        }
        const char saved = buffer[index];
        buffer[index] = '\0';
        if (text_width(buffer, size) <= max_width) {
            last_fit = index;
        } else {
            buffer[index] = saved;
            break;
        }
        buffer[index] = saved;
    }
    split = last_fit;
    if (split == 0) {
        split = length / 2;
        while (split > 1) {
            const char saved = buffer[split];
            buffer[split] = '\0';
            if (text_width(buffer, size) <= max_width) {
                break;
            }
            buffer[split] = saved;
            split--;
        }
    } else {
        buffer[split] = '\0';
    }
    draw_text_fit(buffer, x, y, max_width, size, color);
    const char *second = text + split;
    while (*second == ' ') {
        second++;
    }
    draw_text_fit(second, x, y + 17, max_width, size, color);
}

static void draw_clause_row(int y, const char *lead, const char *text) {
    const Rectangle row = {374.0f, (float)y, 326.0f, 39.0f};
    DrawRectangleRec(row, Fade(PARCHMENT_DARK, 0.13f));
    DrawRectangleLinesEx(row, 1.0f, Fade(PARCHMENT_DARK, 0.65f));
    draw_text(lead, 381, y + 13, TYPE_CAPTION, OCHRE_INK);
    draw_text_wrapped_two_lines(text, 412, y + 3, 280, TYPE_CAPTION, INK);
}

static void draw_behavior_clauses(const World *world, const Entity *entity) {
    draw_text("BEHAVIOR", 374, 87, TYPE_SECTION, INK_SOFT);
    draw_text("CLAUSES / READ ONLY", 552, 89, TYPE_CAPTION, INK_SOFT);
    const PaliDocument *document = world_prototype_document(
        world, (PrototypeId)entity->prototype);
    if (document == NULL || !document->has_use) {
        draw_clause_row(108, "WHEN", "no readable response is inscribed");
        return;
    }

    draw_clause_row(108, "WHEN", "this entity is used by an actor");
    const uint16_t visible_statements =
        document->statement_count < 4u ? document->statement_count : 4u;
    for (uint16_t index = 0; index < visible_statements; ++index) {
        char clause[256];
        clause_statement_text(document, &document->statements[index], clause,
                              sizeof(clause));
        draw_clause_row(152 + (int)index * 44, index == 0 ? "DO" : "THEN",
                        clause);
    }
    if (document->statement_count > visible_statements) {
        char remainder[64];
        (void)snprintf(remainder, sizeof(remainder),
                       "%u further clauses remain readable",
                       (unsigned int)(document->statement_count -
                                      visible_statements));
        draw_text(remainder, 382, 331, TYPE_CAPTION, INK_SOFT);
    }
}

static void draw_structured_lens(const UiState *ui, const World *world) {
    const Entity *entity =
        world_entity_by_id_const(world, ui->inspected_entity_id);
    if (entity == NULL) {
        return;
    }
    DrawRectangle(0, 0, PAL_VIRTUAL_WIDTH, PAL_VIRTUAL_HEIGHT, PARCHMENT);
    DrawRectangleLinesEx((Rectangle){5.0f, 5.0f, 710.0f, 395.0f}, 2.0f,
                         PAL_GOLD);

    char kind[32];
    uppercase_copy(kind, sizeof(kind),
                   world_prototype_name((PrototypeId)entity->prototype));
    char title[64];
    char target[64];
    (void)snprintf(title, sizeof(title), "LENS / %s", kind);
    (void)snprintf(target, sizeof(target), "THIS %s", kind);
    draw_text(title, 14, 10, TYPE_TITLE, INK);
    draw_text(target, 447, 11, TYPE_HEADING, INK);
    draw_text("one material entity", 448, 37, TYPE_BODY, INK_SOFT);
    draw_button(lens_close_button(), "CLOSE", PARCHMENT_DARK);
    if (entity->local_override >= 0) {
        draw_text("LOCAL SCAR", 15, 42, TYPE_BODY, OCHRE_INK);
        draw_text("Provenance / a sparse Entity Patch resolves here", 102,
                  42, TYPE_BODY, INK_SOFT);
    } else {
        draw_text("Provenance / inherited meaning", 15, 42, TYPE_BODY,
                  INK_SOFT);
    }
    DrawLine(14, 69, 702, 69, PARCHMENT_DARK);

    DrawRectangleRec((Rectangle){12.0f, 82.0f, 344.0f, 268.0f},
                     Fade(PARCHMENT_DARK, 0.10f));
    DrawRectangleLinesEx((Rectangle){12.0f, 82.0f, 344.0f, 268.0f}, 1.0f,
                         PARCHMENT_DARK);
    DrawRectangleRec((Rectangle){365.0f, 82.0f, 343.0f, 268.0f},
                     Fade(PARCHMENT_DARK, 0.10f));
    DrawRectangleLinesEx((Rectangle){365.0f, 82.0f, 343.0f, 268.0f}, 1.0f,
                         PARCHMENT_DARK);

    ConceptAccess color_access;
    ConceptAccess mass_access;
    ConceptAccess nutrition_access;
    ConceptAccess ripe_access;
    PaliValue color_value;
    PaliValue mass_value;
    PaliValue nutrition_value;
    PaliValue ripe_value;
    const bool has_color = visible_concept_value(
        world, entity, CONCEPT_COLOR, &color_access, &color_value);
    const bool has_mass = visible_concept_value(
        world, entity, CONCEPT_MASS, &mass_access, &mass_value);
    const bool has_nutrition = visible_concept_value(
        world, entity, CONCEPT_NUTRITION, &nutrition_access,
        &nutrition_value);
    const bool has_ripe = visible_concept_value(
        world, entity, CONCEPT_RIPE, &ripe_access, &ripe_value);

    if (has_color) {
        draw_text("SENSORY", 24, 89, TYPE_SECTION, INK_SOFT);
        draw_static_concept(lexicon_find_by_id(CONCEPT_COLOR), color_access,
                            color_value, 111, true);
    }
    DrawLine(22, 143, 346, 143, Fade(PARCHMENT_DARK, 0.65f));
    if (has_mass) {
        draw_text("MATERIAL", 24, 149, TYPE_SECTION, INK_SOFT);
        draw_static_concept(lexicon_find_by_id(CONCEPT_MASS), mass_access,
                            mass_value, 171, false);
    }
    DrawLine(22, 201, 346, 201, Fade(PARCHMENT_DARK, 0.65f));
    if (has_nutrition || has_ripe) {
        draw_text("VITAL", 24, 204, TYPE_SECTION, INK_SOFT);
    }
    if (has_nutrition) {
        draw_nourishment_concept(
            ui, lexicon_find_by_id(CONCEPT_NUTRITION), nutrition_access,
            nutrition_value, 222);
    }
    if (has_ripe) {
        draw_static_concept(lexicon_find_by_id(CONCEPT_RIPE), ripe_access,
                            ripe_value, 278, false);
    }
    draw_behavior_clauses(world, entity);

    DrawRectangle(6, 356, 708, 44,
                  ui->has_error ? Fade(ERROR_RED, 0.96f)
                                : Fade(INK, 0.88f));
    if (ui->has_error) {
        draw_text_fit(ui->error.message, 15, 371, 452, TYPE_BODY,
                      (Color){255, 239, 215, 255});
    } else if (entity->local_override >= 0) {
        draw_text("This entity carries a local Entity Scar.", 15, 372,
                  TYPE_BODY, PARCHMENT);
    } else {
        draw_text("Reach / this entity only", 15, 372, TYPE_BODY,
                  PARCHMENT);
    }
    if (nourishment_is_patchable(ui, world)) {
        draw_button(discard_button(), "DISCARD", PARCHMENT_DARK);
        draw_button(inscribe_button(), "INSCRIBE", PAL_GOLD);
    }
}

static void draw_developer_inspector(const UiState *ui, const World *world) {
    const Entity *entity =
        world_entity_by_id_const(world, ui->inspected_entity_id);
    if (entity == NULL) {
        return;
    }
    DrawRectangle(0, 0, PAL_VIRTUAL_WIDTH, PAL_VIRTUAL_HEIGHT, PARCHMENT);
    DrawRectangleLinesEx((Rectangle){5.0f, 5.0f, 710.0f, 395.0f}, 2.0f,
                         PAL_GOLD);
    char title[64];
    (void)snprintf(title, sizeof(title), "OPEN OBJECT / %s",
                   world_prototype_name((PrototypeId)entity->prototype));
    draw_text(title, 12, 5, TYPE_TITLE, INK);
    char identity[64];
    (void)snprintf(identity, sizeof(identity), "id %016llx",
                   (unsigned long long)entity->id);
    draw_text(identity, 12, 32, TYPE_BODY, INK_SOFT);
    char prototype[96];
    (void)snprintf(prototype, sizeof(prototype),
                   "prototype %s / shared definition%s",
                   world_prototype_name((PrototypeId)entity->prototype),
                   entity->local_override >= 0 ? " / local scar exists" : "");
    draw_text_fit(prototype, 264, 32, 378, TYPE_BODY, INK);
    char state[96];
    (void)snprintf(state, sizeof(state), "%s  /  position %.0f, %.0f",
                   entity->active ? "ACTIVE" : "ABSENT", entity->x, entity->y);
    draw_text(state, 12, 55, TYPE_SECTION,
              entity->active ? INK : ERROR_INK);
    draw_button(apply_button(), "APPLY", (Color){137, 208, 132, 255});
    draw_button(revert_button(), "REVERT", PAL_GOLD);
    draw_button(close_button(), "ESC", PARCHMENT_DARK);

    draw_text("KNOWN STATE", 12, 78, TYPE_SECTION, INK_SOFT);
    draw_text("PALI SOURCE", 210, 78, TYPE_SECTION, INK_SOFT);
    DrawRectangleRec((Rectangle){9.0f, 96.0f, 189.0f, 265.0f},
                     Fade(PARCHMENT_DARK, 0.16f));
    DrawRectangleLinesEx((Rectangle){9.0f, 96.0f, 189.0f, 265.0f}, 2.0f,
                         PARCHMENT_DARK);
    draw_text("STATE", 15, 103, TYPE_BODY, INK_SOFT);
    draw_text(entity->active ? "active" : "absent", 15, 120, TYPE_SECTION,
              entity->active ? INK : ERROR_INK);
    draw_text("POSITION", 15, 144, TYPE_BODY, INK_SOFT);
    char position[32];
    (void)snprintf(position, sizeof(position), "%.0f, %.0f", entity->x,
                   entity->y);
    draw_text(position, 15, 161, TYPE_SECTION, INK);
    draw_text("PROPERTIES", 15, 184, TYPE_BODY, INK_SOFT);
    const PaliProgram *program = world_entity_program(world, entity);
    for (uint16_t index = 0;
         program != NULL && index < program->property_count && index < 6;
         ++index) {
        char value[24];
        value_text(program->properties[index].value, value, sizeof(value));
        const int y = 204 + (int)index * 24;
        draw_text_fit(program->properties[index].name, 15, y, 88,
                      TYPE_CAPTION, INK_SOFT);
        draw_text_fit(value, 107, y, 85, TYPE_SECTION, INK);
    }
    draw_editor(&ui->editor, world->universe.tick);
    DrawRectangle(6, 365, 708, 35,
                  ui->has_error ? Fade(ERROR_RED, 0.96f)
                                : Fade(INK, 0.88f));
    char caret_status[48];
    const size_t line_start = line_start_before(&ui->editor,
                                                ui->editor.cursor);
    (void)snprintf(caret_status, sizeof(caret_status), "CARET %d:%zu",
                   editor_cursor_line(&ui->editor) + 1,
                   ui->editor.cursor - line_start + 1);
    const int caret_x = 708 - text_width(caret_status, TYPE_BODY);
    if (ui->has_error) {
        char error[192];
        (void)snprintf(error, sizeof(error), "L%d:%d %.145s", ui->error.line,
                       ui->error.column, ui->error.message);
        draw_text_fit(error, 14, 374, caret_x - 26, TYPE_BODY,
                      (Color){255, 239, 215, 255});
    } else {
        draw_text("EDITOR FOCUSED  /  Ctrl+Enter apply  /  Ctrl+R revert",
                  14, 374, TYPE_BODY, PARCHMENT);
    }
    draw_text(caret_status, caret_x, 374, TYPE_BODY,
              ui->has_error ? (Color){255, 239, 215, 255} : PAL_GOLD);
}

typedef enum FirstScarInquiry {
    FIRST_SCAR_NEEDS_INSCRIPTION = 0,
    FIRST_SCAR_NEEDS_PROOF,
    FIRST_SCAR_COMPLETE
} FirstScarInquiry;

static FirstScarInquiry first_scar_inquiry(const World *world) {
    bool active_scar = false;
    for (int slot = 0; slot < WORLD_MAX_LOCAL_OVERRIDES; ++slot) {
        const LocalOverride *override =
            &world->universe.local_overrides[slot];
        if (!override->active) {
            continue;
        }
        bool changes_nutrition = false;
        for (uint8_t value = 0; value < override->value_count; ++value) {
            if (override->values[value].concept == CONCEPT_NUTRITION) {
                changes_nutrition = true;
                break;
            }
        }
        if (!changes_nutrition) {
            continue;
        }
        const Entity *entity =
            world_entity_by_id_const(world, override->entity_id);
        if (entity == NULL || entity->prototype != PROTOTYPE_APPLE) {
            continue;
        }
        if (!entity->active) {
            return FIRST_SCAR_COMPLETE;
        }
        active_scar = true;
    }
    return active_scar ? FIRST_SCAR_NEEDS_PROOF
                       : FIRST_SCAR_NEEDS_INSCRIPTION;
}

static void draw_inquiry_step(int y, const char *title, const char *detail,
                              bool complete, bool active) {
    const Color emphasis = active ? INK : INK_SOFT;
    draw_text(complete ? "[x]" : active ? "[+]" : "[ ]", 501, y,
              TYPE_BODY, complete || active ? OCHRE_INK : INK_SOFT);
    draw_text_fit(title, 529, y, 169, TYPE_BODY, emphasis);
    draw_text_fit(detail, 529, y + 18, 169, TYPE_CAPTION, INK_SOFT);
}

static void draw_closed_panel(const World *world, const char *save_hint) {
    DrawRectangle(PAL_PANEL_X, PAL_HUD_HEIGHT,
                  PAL_VIRTUAL_WIDTH - PAL_PANEL_X,
                  PAL_VIRTUAL_HEIGHT - PAL_HUD_HEIGHT,
                  PARCHMENT);
    DrawRectangle(PAL_PANEL_X, PAL_HUD_HEIGHT, 4,
                  PAL_VIRTUAL_HEIGHT - PAL_HUD_HEIGHT,
                  PAL_GOLD);
    draw_text("THE FIRST SCAR", 500, 53, TYPE_TITLE, INK);
    draw_text("ordinary things open inward", 501, 79, TYPE_BODY,
              INK_SOFT);
    DrawLine(501, 101, 698, 101, PARCHMENT_DARK);
    const FirstScarInquiry inquiry = first_scar_inquiry(world);
    draw_text("INQUIRY", 501, 115, TYPE_CAPTION, OCHRE_INK);
    draw_text_fit("MAKE ONE APPLE DIFFERENT", 501, 134, 197, TYPE_BODY,
                  INK);
    draw_inquiry_step(160, "INSCRIBE A SCAR",
                      inquiry == FIRST_SCAR_NEEDS_INSCRIPTION
                          ? "Alter its nourishment."
                          : "Local Scar inscribed.",
                      inquiry != FIRST_SCAR_NEEDS_INSCRIPTION,
                      inquiry == FIRST_SCAR_NEEDS_INSCRIPTION);
    draw_inquiry_step(207, "TEST THE DIFFERENCE",
                      inquiry == FIRST_SCAR_NEEDS_INSCRIPTION
                          ? "Then invoke that apple."
                          : inquiry == FIRST_SCAR_NEEDS_PROOF
                                ? "Stand nearby; press F."
                                : "Its use proved the Scar.",
                      inquiry == FIRST_SCAR_COMPLETE,
                      inquiry == FIRST_SCAR_NEEDS_PROOF);
    draw_text_fit(inquiry == FIRST_SCAR_NEEDS_INSCRIPTION
                      ? "0 / 2  INQUIRY OPEN"
                      : inquiry == FIRST_SCAR_NEEDS_PROOF
                            ? "1 / 2  SCAR INSCRIBED"
                            : "COMPLETE  DIFFERENCE PROVEN",
                  501, 250, 197, TYPE_CAPTION,
                  inquiry == FIRST_SCAR_COMPLETE ? OCHRE_INK : INK_SOFT);
    DrawLine(501, 266, 698, 266, PARCHMENT_DARK);
    draw_text("WASD / arrows  walk", 501, 280, TYPE_BODY, INK);
    draw_text("CLICK / E  open entity", 501, 298, TYPE_BODY, INK);
    draw_text("F  invoke its behavior", 501, 316, TYPE_BODY, INK);
    draw_text("F5 save  |  F9 reload", 501, 334, TYPE_BODY, INK);
    draw_text_fit(save_hint, 501, 354, 198, TYPE_CAPTION, INK_SOFT);
    draw_text("Reality is only", 501, 373, TYPE_BODY, INK_SOFT);
    draw_text("provisionally closed.", 501, 389, TYPE_BODY, INK_SOFT);
}

void ui_draw(const UiState *ui, const World *world, int nearby_entity,
             const char *save_hint) {
    ClearBackground(INK);
    const Camera2D world_camera = {
        .offset = {0.0f, 0.0f},
        .target = {0.0f, 0.0f},
        .rotation = 0.0f,
        .zoom = PAL_WORLD_ZOOM,
    };
    BeginMode2D(world_camera);
    draw_tiles(world);
    draw_firelight(world);
    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        draw_entity(world, &world->universe.entities[index],
                    (int)index == nearby_entity && !ui->inspector_open);
    }
    draw_player(world);
    EndMode2D();

    DrawRectangle(0, 0, PAL_VIRTUAL_WIDTH, PAL_HUD_HEIGHT, Fade(INK, 0.95f));
    char seed[48];
    (void)snprintf(seed, sizeof(seed), "SEED %016llx",
                   (unsigned long long)world->universe.root_seed);
    int hud_x = 8;
    draw_text(seed, hud_x, 9, TYPE_BODY, PARCHMENT);
    hud_x += text_width(seed, TYPE_BODY) + 12;
    if (ui->developer_mode) {
        char tick[32];
        (void)snprintf(
            tick, sizeof(tick), "TICK %08llx",
            (unsigned long long)(world->universe.tick &
                                 UINT64_C(0xffffffff)));
        draw_text(tick, hud_x, 9, TYPE_BODY, PARCHMENT);
        hud_x += text_width(tick, TYPE_BODY) + 12;
    }
    hud_x += draw_bar(hud_x, 9, 48, world->embodiment.hunger, ERROR_RED,
                      "HUNGER") + 12;
    (void)draw_bar(hud_x, 9, 48, world->embodiment.warmth, COLD_BLUE,
                   "WARMTH");
    const char *mode = ui->developer_mode ? "PALI/DEV" : "LENS/STATE";
    draw_text(mode,
              PAL_VIRTUAL_WIDTH - text_width(mode, TYPE_BODY) - 9, 9,
              TYPE_BODY, PAL_GOLD);
    draw_text_fit(world->message, 8, 382, PAL_PANEL_X - 16, TYPE_BODY,
                  PARCHMENT);

    if (ui->inspector_open) {
        if (ui->developer_mode) {
            draw_developer_inspector(ui, world);
        } else {
            draw_structured_lens(ui, world);
        }
    } else {
        draw_closed_panel(world, save_hint);
    }
}
