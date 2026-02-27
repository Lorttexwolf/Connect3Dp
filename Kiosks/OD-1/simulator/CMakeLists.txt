cmake_minimum_required(VERSION 3.16)
project(OD1Simulator CXX C)

set(CMAKE_C_STANDARD   11)
set(CMAKE_CXX_STANDARD 17)

# ── Fetch LVGL v8 ─────────────────────────────────────────────────────────────
include(FetchContent)

FetchContent_Declare(lvgl
    GIT_REPOSITORY https://github.com/lvgl/lvgl.git
    GIT_TAG        v8.3.11
    GIT_SHALLOW    TRUE
)
FetchContent_MakeAvailable(lvgl)

# Tell LVGL to find lv_conf.h via a plain #include "lv_conf.h" so it picks up
# the file from this directory (added to the include path below).
target_compile_definitions(lvgl PUBLIC LV_CONF_INCLUDE_SIMPLE)
target_include_directories(lvgl PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})

# ── SDL2 ──────────────────────────────────────────────────────────────────────
find_package(SDL2 REQUIRED)

# ── Simulator executable ──────────────────────────────────────────────────────
add_executable(OD1Simulator main.cpp)

target_include_directories(OD1Simulator PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}      # lv_conf.h
    ${CMAKE_CURRENT_SOURCE_DIR}/..   # config.h, machine_state.h, ui_manager.h
)

target_link_libraries(OD1Simulator PRIVATE lvgl)

# Handle both modern CMake SDL2 targets and legacy find-module variables.
if(TARGET SDL2::SDL2)
    target_link_libraries(OD1Simulator PRIVATE SDL2::SDL2)
else()
    target_include_directories(OD1Simulator PRIVATE ${SDL2_INCLUDE_DIRS})
    target_link_libraries(OD1Simulator PRIVATE ${SDL2_LIBRARIES})
endif()
