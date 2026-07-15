#ifndef PALIMPSEST_PLATFORM_H
#define PALIMPSEST_PLATFORM_H

#include "pali.h"

#include <stdbool.h>
#include <stddef.h>
#include <stdio.h>

#define PLATFORM_PATH_CAP 512

bool platform_ensure_directory(const char *path, PaliError *error);
bool platform_default_save_path(char *out, size_t capacity, PaliError *error);
bool platform_atomic_replace(const char *temporary_path,
                             const char *destination_path, PaliError *error);
bool platform_flush_file(FILE *file, PaliError *error);
bool platform_file_exists(const char *path);

#endif
