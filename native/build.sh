#!/bin/bash
set -e

# Determine script directory
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Check if g++ exists in path
if ! command -v g++ &> /dev/null; then
  echo "========================================================================"
  echo "WARNING: g++ compiler was not found in the environment PATH."
  echo "Native technical indicators will not be compiled."
  echo "Nexus Trading Engine will automatically use its managed C# fallback paths."
  echo "========================================================================"
  exit 0
fi

if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" || "$OSTYPE" == "cygwin" ]]; then
  echo "Compiling native library for Windows..."
  g++ -shared -O3 -o "$DIR/Nexus.Native/nexus_native.dll" "$DIR/Nexus.Native/NexusNative.cpp"
  echo "Native library compiled successfully at $DIR/Nexus.Native/nexus_native.dll"
else
  echo "Compiling native library for Linux..."
  g++ -shared -fPIC -O3 -o "$DIR/Nexus.Native/libnexus_native.so" "$DIR/Nexus.Native/NexusNative.cpp"
  echo "Native library compiled successfully at $DIR/Nexus.Native/libnexus_native.so"
fi
