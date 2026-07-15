#ifndef PALIMPSEST_SAVE_H
#define PALIMPSEST_SAVE_H

#include "world.h"

#define PAL_SAVE_VERSION 3

bool save_write_atomic(const World *world, const char *path, PaliError *error);
bool save_load(World *world, const char *path, const char *pali_asset_root,
               PaliError *error);
bool save_validate_file(const char *path, PaliError *error);

#endif
