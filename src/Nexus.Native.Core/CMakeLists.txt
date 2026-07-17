cmake_minimum_required(VERSION 3.15)
project(nexus_native_core CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Configure output directories
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)

# Include paths
include_directories(include)

# Source files
set(SOURCES
    src/core_runtime.cpp
    src/market_state.cpp
    src/market_vector.cpp
    src/accumulator.cpp
)

# Shared library
add_library(nexus_native_core SHARED ${SOURCES})

# Configure compiler flags (Calibrated for MSVC VS 2026 compiler)
if(MSVC)
    # REASON: Added /wd4324 to suppress structure alignment padding warnings.
    # alignas(32) is required for bare-metal AVX2 vector optimizations, so padding is fully expected.
    target_compile_options(nexus_native_core PRIVATE /W4 /WX /wd4324)
else()
    target_compile_options(nexus_native_core PRIVATE -Wall -Wextra -Werror -O3 -fPIC)
endif()

# Simple native test executable
add_executable(native_core_tests tests/native_tests.cpp)
target_link_libraries(native_core_tests PRIVATE nexus_native_core)