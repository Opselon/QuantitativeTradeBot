#!/bin/bash
set -e

# Determine script directory
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo "Compiling native library..."
g++ -shared -fPIC -O3 -o "$DIR/Nexus.Native/libnexus_native.so" "$DIR/Nexus.Native/NexusNative.cpp"
echo "Native library compiled successfully at $DIR/Nexus.Native/libnexus_native.so"
