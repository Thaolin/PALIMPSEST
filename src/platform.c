#include "platform.h"

#include <errno.h>
#include <stdlib.h>
#include <string.h>

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <direct.h>
#include <io.h>
#else
#include <sys/stat.h>
#include <sys/types.h>
#include <unistd.h>
#endif

static bool platform_error(PaliError *error, const char *message) {
    if (error != NULL) {
        error->line = 0;
        error->column = 0;
        (void)snprintf(error->message, sizeof(error->message), "%s", message);
    }
    return false;
}

bool platform_ensure_directory(const char *path, PaliError *error) {
    if (path == NULL || path[0] == '\0') {
        return platform_error(error, "directory path is empty");
    }
#ifdef _WIN32
    if (CreateDirectoryA(path, NULL) != 0 ||
        GetLastError() == ERROR_ALREADY_EXISTS) {
        return true;
    }
#else
    if (mkdir(path, 0700) == 0 || errno == EEXIST) {
        return true;
    }
#endif
    return platform_error(error, "could not create save directory");
}

bool platform_default_save_path(char *out, size_t capacity, PaliError *error) {
    if (out == NULL || capacity == 0) {
        return platform_error(error, "save path storage is invalid");
    }
    const char *base = NULL;
    char directory[PLATFORM_PATH_CAP];
#ifdef _WIN32
    base = getenv("LOCALAPPDATA");
    if (base == NULL || base[0] == '\0') {
        return platform_error(error, "LOCALAPPDATA is unavailable");
    }
    const int directory_length =
        snprintf(directory, sizeof(directory), "%s\\PALIMPSEST", base);
    if (directory_length < 0 ||
        (size_t)directory_length >= sizeof(directory) ||
        !platform_ensure_directory(directory, error)) {
        return false;
    }
    const int length = snprintf(out, capacity, "%s\\save.pal", directory);
#else
    base = getenv("XDG_STATE_HOME");
    if (base == NULL || base[0] == '\0') {
        base = getenv("HOME");
        if (base == NULL || base[0] == '\0') {
            return platform_error(error, "HOME is unavailable");
        }
        const int directory_length =
            snprintf(directory, sizeof(directory), "%s/.palimpsest", base);
        if (directory_length < 0 ||
            (size_t)directory_length >= sizeof(directory) ||
            !platform_ensure_directory(directory, error)) {
            return false;
        }
    } else {
        const int directory_length =
            snprintf(directory, sizeof(directory), "%s/PALIMPSEST", base);
        if (directory_length < 0 ||
            (size_t)directory_length >= sizeof(directory) ||
            !platform_ensure_directory(directory, error)) {
            return false;
        }
    }
    const int length = snprintf(out, capacity, "%s/save.pal", directory);
#endif
    if (length < 0 || (size_t)length >= capacity) {
        return platform_error(error, "default save path is too long");
    }
    return true;
}

bool platform_atomic_replace(const char *temporary_path,
                             const char *destination_path, PaliError *error) {
#ifdef _WIN32
    if (MoveFileExA(temporary_path, destination_path,
                    MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH) != 0) {
        return true;
    }
#else
    if (rename(temporary_path, destination_path) == 0) {
        return true;
    }
#endif
    return platform_error(error, "could not atomically replace the save");
}

bool platform_flush_file(FILE *file, PaliError *error) {
    if (file == NULL || fflush(file) != 0) {
        return platform_error(error, "could not flush save data");
    }
#ifdef _WIN32
    if (_commit(_fileno(file)) != 0) {
        return platform_error(error, "could not commit save data to disk");
    }
#else
    if (fsync(fileno(file)) != 0) {
        return platform_error(error, "could not commit save data to disk");
    }
#endif
    return true;
}

bool platform_file_exists(const char *path) {
    if (path == NULL) {
        return false;
    }
#ifdef _WIN32
    const DWORD attributes = GetFileAttributesA(path);
    return attributes != INVALID_FILE_ATTRIBUTES &&
           (attributes & FILE_ATTRIBUTE_DIRECTORY) == 0;
#else
    struct stat status;
    return stat(path, &status) == 0 && S_ISREG(status.st_mode);
#endif
}
