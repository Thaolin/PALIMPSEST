#include "platform.h"
#include "save.h"
#include "ui.h"
#include "world.h"

#include "raylib.h"

#include <errno.h>
#include <stdbool.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

typedef struct Options {
    uint64_t seed;
    bool force_new;
    bool help;
    const char *save_path;
    const char *capture_path;
    bool capture_inspector;
    bool developer;
} Options;

static World game_world;
static UiState game_ui;

static void print_usage(void) {
    (void)printf(
        "PALIMPSEST: The First Patch\n"
        "  palimpest [--seed N] [--new] [--save PATH]\n"
        "             [--capture PNG] [--capture-inspector PNG] [--developer]\n"
        "\n--seed and --new begin a deterministic new clearing.\n");
}

static bool parse_seed(const char *text, uint64_t *out) {
    if (text == NULL || text[0] == '\0' || text[0] == '-') {
        return false;
    }
    errno = 0;
    char *end = NULL;
    const unsigned long long value = strtoull(text, &end, 0);
    if (errno != 0 || end == NULL || *end != '\0') {
        return false;
    }
    *out = (uint64_t)value;
    return true;
}

static bool parse_options(int argc, char **argv, Options *options) {
    memset(options, 0, sizeof(*options));
    options->seed = UINT64_C(0x50414c494d503031);
    for (int index = 1; index < argc; ++index) {
        if (strcmp(argv[index], "--help") == 0 ||
            strcmp(argv[index], "-h") == 0) {
            options->help = true;
        } else if (strcmp(argv[index], "--new") == 0) {
            options->force_new = true;
        } else if (strcmp(argv[index], "--seed") == 0 && index + 1 < argc) {
            if (!parse_seed(argv[++index], &options->seed)) {
                return false;
            }
            options->force_new = true;
        } else if (strcmp(argv[index], "--save") == 0 && index + 1 < argc) {
            options->save_path = argv[++index];
        } else if (strcmp(argv[index], "--capture") == 0 && index + 1 < argc) {
            options->capture_path = argv[++index];
        } else if (strcmp(argv[index], "--capture-inspector") == 0 &&
                   index + 1 < argc) {
            options->capture_path = argv[++index];
            options->capture_inspector = true;
        } else if (strcmp(argv[index], "--developer") == 0) {
            options->developer = true;
        } else {
            return false;
        }
    }
    return true;
}

static bool join_path(char *out, size_t capacity, const char *base,
                      const char *suffix) {
    const size_t length = strlen(base);
    const bool separator = length > 0 && base[length - 1] != '/' &&
                           base[length - 1] != '\\';
    const int written = snprintf(out, capacity, "%s%s%s", base,
                                 separator ? "/" : "", suffix);
    return written >= 0 && (size_t)written < capacity;
}

static Vector2 mouse_in_virtual_space(int scale, float offset_x,
                                      float offset_y) {
    const Vector2 mouse = GetMousePosition();
    return (Vector2){(mouse.x - offset_x) / (float)scale,
                     (mouse.y - offset_y) / (float)scale};
}

static int entity_at_virtual_mouse(const World *world, Vector2 mouse) {
    if (world == NULL || mouse.x < 0.0f || mouse.x >= (float)PAL_PANEL_X ||
        mouse.y < (float)PAL_HUD_HEIGHT ||
        mouse.y >= (float)PAL_VIRTUAL_HEIGHT) {
        return -1;
    }
    const float world_x = mouse.x / PAL_WORLD_ZOOM;
    const float world_y = mouse.y / PAL_WORLD_ZOOM - (float)PAL_MAP_Y;
    const float limit = 7.0f * 7.0f;
    float best = limit;
    int result = -1;
    for (uint16_t index = 0; index < world->universe.entity_count; ++index) {
        const Entity *entity = &world->universe.entities[index];
        if (!entity->active) {
            continue;
        }
        const float dx = entity->x - world_x;
        const float dy = entity->y - world_y;
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

static void set_world_message(const char *prefix, const PaliError *error) {
    (void)snprintf(game_world.message, sizeof(game_world.message),
                   "%s: %.120s", prefix,
                   error != NULL ? error->message : "unknown error");
}

int main(int argc, char **argv) {
    Options options;
    if (!parse_options(argc, argv, &options)) {
        print_usage();
        return 2;
    }
    if (options.help) {
        print_usage();
        return 0;
    }

    SetConfigFlags(FLAG_WINDOW_RESIZABLE | FLAG_VSYNC_HINT);
    InitWindow(1440, 810, "PALIMPSEST - The First Patch");
    SetExitKey(KEY_NULL);
    SetTargetFPS(60);

    char asset_root[PLATFORM_PATH_CAP];
    if (!join_path(asset_root, sizeof(asset_root), GetApplicationDirectory(),
                   "assets/pali")) {
        (void)fprintf(stderr, "application asset path is too long\n");
        CloseWindow();
        return 1;
    }
    char font_path[PLATFORM_PATH_CAP];
    if (!join_path(font_path, sizeof(font_path), GetApplicationDirectory(),
                   "assets/fonts/AnonymousPro-Bold.ttf")) {
        (void)fprintf(stderr, "application font path is too long\n");
        CloseWindow();
        return 1;
    }
    char default_save[PLATFORM_PATH_CAP];
    PaliError error;
    if (options.save_path == NULL) {
        if (!platform_default_save_path(default_save, sizeof(default_save),
                                        &error)) {
            (void)fprintf(stderr, "%s\n", error.message);
            CloseWindow();
            return 1;
        }
        options.save_path = default_save;
    }

    bool loaded = false;
    char load_failure[PALI_ERROR_CAP] = "";
    if (!options.force_new && platform_file_exists(options.save_path)) {
        loaded = save_load(&game_world, options.save_path, asset_root, &error);
        if (!loaded) {
            (void)snprintf(load_failure, sizeof(load_failure), "%s",
                           error.message);
        }
    }
    if (!loaded) {
        if (!world_init(&game_world, options.seed, asset_root, &error)) {
            (void)fprintf(stderr, "world init failed at %d:%d: %s\n",
                          error.line, error.column, error.message);
            CloseWindow();
            return 1;
        }
        if (load_failure[0] != '\0') {
            (void)snprintf(game_world.message, sizeof(game_world.message),
                           "Save rejected; new world: %.105s", load_failure);
        }
    } else {
        (void)snprintf(game_world.message, sizeof(game_world.message),
                       "Save resumed exactly. E opens the Lens.");
    }

    ui_init(&game_ui, font_path);
    if (options.developer) {
        world_grant_developer_knowledge(&game_world);
        ui_set_developer_mode(&game_ui, true);
    }
    if (options.capture_inspector) {
        for (uint16_t index = 0; index < game_world.universe.entity_count;
             ++index) {
            if (game_world.universe.entities[index].prototype ==
                PROTOTYPE_APPLE) {
                ui_open_inspector(&game_ui, &game_world, (int)index);
                break;
            }
        }
    }

    RenderTexture2D canvas =
        LoadRenderTexture(PAL_VIRTUAL_WIDTH, PAL_VIRTUAL_HEIGHT);
    SetTextureFilter(canvas.texture, TEXTURE_FILTER_POINT);
    double accumulator = 0.0;
    const double fixed_step = 1.0 / 60.0;
    int rendered_frames = 0;
    bool running = true;

    while (running && !WindowShouldClose()) {
        const int scale_x = GetScreenWidth() / PAL_VIRTUAL_WIDTH;
        const int scale_y = GetScreenHeight() / PAL_VIRTUAL_HEIGHT;
        int scale = scale_x < scale_y ? scale_x : scale_y;
        if (scale < 1) {
            scale = 1;
        }
        const float draw_width = (float)(PAL_VIRTUAL_WIDTH * scale);
        const float draw_height = (float)(PAL_VIRTUAL_HEIGHT * scale);
        const float offset_x = ((float)GetScreenWidth() - draw_width) * 0.5f;
        const float offset_y = ((float)GetScreenHeight() - draw_height) * 0.5f;
        const Vector2 virtual_mouse =
            mouse_in_virtual_space(scale, offset_x, offset_y);

        if (game_ui.inspector_open) {
            ui_update(&game_ui, &game_world, virtual_mouse);
            accumulator = 0.0;
        } else {
            if (IsMouseButtonPressed(MOUSE_BUTTON_LEFT)) {
                const int clicked =
                    entity_at_virtual_mouse(&game_world, virtual_mouse);
                if (clicked >= 0) {
                    ui_open_inspector(&game_ui, &game_world, clicked);
                }
            }
            if (IsKeyPressed(KEY_E)) {
                const int nearby = world_nearest_entity(&game_world, 18.0f);
                if (nearby >= 0) {
                    ui_open_inspector(&game_ui, &game_world, nearby);
                } else {
                    (void)snprintf(game_world.message,
                                   sizeof(game_world.message),
                                   "Nothing nearby is open yet.");
                }
            }
            if (IsKeyPressed(KEY_F)) {
                const int nearby = world_nearest_entity(&game_world, 18.0f);
                if (nearby < 0) {
                    (void)snprintf(game_world.message,
                                   sizeof(game_world.message),
                                   "Nothing nearby answers use.");
                } else {
                    (void)world_use_entity(&game_world, nearby, &error);
                }
            }

            float frame_time = GetFrameTime();
            if (frame_time > 0.25f) {
                frame_time = 0.25f;
            }
            accumulator += (double)frame_time;
            WorldInput input = {0.0f, 0.0f};
            input.move_x =
                (IsKeyDown(KEY_D) || IsKeyDown(KEY_RIGHT) ? 1.0f : 0.0f) -
                (IsKeyDown(KEY_A) || IsKeyDown(KEY_LEFT) ? 1.0f : 0.0f);
            input.move_y =
                (IsKeyDown(KEY_S) || IsKeyDown(KEY_DOWN) ? 1.0f : 0.0f) -
                (IsKeyDown(KEY_W) || IsKeyDown(KEY_UP) ? 1.0f : 0.0f);
            int steps = 0;
            while (accumulator >= fixed_step && steps < 8) {
                world_step(&game_world, input);
                accumulator -= fixed_step;
                steps++;
            }
            if (steps == 8) {
                accumulator = 0.0;
            }
        }

        if (IsKeyPressed(KEY_F5)) {
            if (save_write_atomic(&game_world, options.save_path, &error)) {
                (void)snprintf(game_world.message,
                               sizeof(game_world.message),
                               "Save validated and atomically replaced.");
            } else {
                set_world_message("Save failed", &error);
            }
        }
        if (IsKeyPressed(KEY_F9)) {
            if (save_load(&game_world, options.save_path, asset_root, &error)) {
                game_ui.inspector_open = false;
                (void)snprintf(game_world.message,
                               sizeof(game_world.message),
                               "Complete save restored.");
            } else {
                set_world_message("Reload failed", &error);
            }
        }
        if ((IsKeyDown(KEY_LEFT_CONTROL) || IsKeyDown(KEY_RIGHT_CONTROL)) &&
            IsKeyPressed(KEY_Q)) {
            running = false;
        }

        const int hovered = entity_at_virtual_mouse(&game_world, virtual_mouse);
        const int nearby = hovered >= 0
                               ? hovered
                               : world_nearest_entity(&game_world, 18.0f);
        BeginTextureMode(canvas);
        ui_draw(&game_ui, &game_world, nearby,
                options.save_path == default_save ? "save: LocalAppData" :
                                                    "save: explicit path");
        EndTextureMode();

        rendered_frames++;
        const bool capture_now = options.capture_path != NULL &&
                                 rendered_frames == 8;
        if (capture_now) {
            Image screenshot = LoadImageFromTexture(canvas.texture);
            ImageFlipVertical(&screenshot);
            ImageResizeNN(&screenshot, PAL_VIRTUAL_WIDTH * 2,
                          PAL_VIRTUAL_HEIGHT * 2);
            if (!ExportImage(screenshot, options.capture_path)) {
                (void)fprintf(stderr, "capture failed: %s\n",
                              options.capture_path);
            }
            UnloadImage(screenshot);
        }
        BeginDrawing();
        ClearBackground((Color){16, 15, 18, 255});
        const Rectangle source = {0.0f, 0.0f, (float)PAL_VIRTUAL_WIDTH,
                                  -(float)PAL_VIRTUAL_HEIGHT};
        const Rectangle destination = {offset_x, offset_y, draw_width,
                                       draw_height};
        DrawTexturePro(canvas.texture, source, destination,
                       (Vector2){0.0f, 0.0f}, 0.0f, WHITE);
        EndDrawing();

        if (capture_now) {
            running = false;
        }
    }

    if (options.capture_path == NULL &&
        !save_write_atomic(&game_world, options.save_path, &error)) {
        (void)fprintf(stderr, "save on exit failed: %s\n", error.message);
    }
    UnloadRenderTexture(canvas);
    ui_shutdown();
    CloseWindow();
    return 0;
}
