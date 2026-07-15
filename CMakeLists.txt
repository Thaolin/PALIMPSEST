cmake_minimum_required(VERSION 3.24)

project(PALIMPSEST VERSION 0.2.0 LANGUAGES C)

set(CMAKE_C_STANDARD 17)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin")

include(CTest)
include(FetchContent)

option(PALIMPSEST_FETCH_RAYLIB "Fetch the pinned raylib dependency" ON)

if(PALIMPSEST_FETCH_RAYLIB AND NOT TARGET raylib)
    set(BUILD_EXAMPLES OFF CACHE BOOL "" FORCE)
    set(BUILD_GAMES OFF CACHE BOOL "" FORCE)
    FetchContent_Declare(
        raylib
        URL https://github.com/raysan5/raylib/archive/refs/tags/6.0.tar.gz
        DOWNLOAD_EXTRACT_TIMESTAMP FALSE
    )
    FetchContent_MakeAvailable(raylib)
endif()

add_library(pal_core STATIC
    src/lexicon.c
    src/pali_lexer.c
    src/pali_compiler.c
    src/pali_vm.c
    src/world.c
    src/save.c
    src/platform.c
)
target_include_directories(pal_core PUBLIC src)

add_executable(pal_tests tests/test_main.c)
target_link_libraries(pal_tests PRIVATE pal_core)
target_compile_definitions(pal_tests PRIVATE
    PAL_TEST_ASSET_ROOT="${CMAKE_CURRENT_SOURCE_DIR}/assets/pali"
    PAL_TEST_TMP_ROOT="${CMAKE_BINARY_DIR}/test-data"
)
add_test(NAME pal_core_tests COMMAND pal_tests)

if(TARGET raylib)
    add_executable(palimpest src/main.c src/ui.c)
    target_link_libraries(palimpest PRIVATE pal_core raylib)
    if(MINGW)
        target_link_options(palimpest PRIVATE -static)
    endif()
    add_custom_command(TARGET palimpest POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy_directory
                "${CMAKE_CURRENT_SOURCE_DIR}/assets"
                "$<TARGET_FILE_DIR:palimpest>/assets"
    )
    install(TARGETS palimpest RUNTIME DESTINATION . COMPONENT Palimpest)
    install(DIRECTORY assets DESTINATION . COMPONENT Palimpest)
    install(FILES README.md THIRD_PARTY_NOTICES.md DESTINATION .
            COMPONENT Palimpest)
endif()

foreach(target pal_core pal_tests palimpest)
    if(TARGET ${target})
        if(MSVC)
            target_compile_options(${target} PRIVATE /W4)
        else()
            target_compile_options(${target} PRIVATE
                -Wall -Wextra -Wpedantic -Wshadow -Wconversion
            )
        endif()
    endif()
endforeach()
