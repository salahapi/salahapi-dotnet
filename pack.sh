#!/usr/bin/env bash
set -euo pipefail

PROJECT="src/SalahAPI/SalahAPI.csproj"
OUTPUT="artifacts"

# Allow overriding the version via an environment variable, e.g.:
#   VERSION=1.2.0 ./pack.sh
VERSION="${VERSION:-}"

echo "Building release..."
dotnet build "$PROJECT" -c Release

echo "Running tests..."
dotnet test SalahAPI.sln -c Release --no-build 2>/dev/null || \
  dotnet test SalahAPI.sln -c Release

echo "Packing..."
PACK_ARGS=("-c" "Release" "-o" "$OUTPUT" "--no-build")
if [[ -n "$VERSION" ]]; then
  PACK_ARGS+=("/p:Version=$VERSION")
fi

dotnet pack "$PROJECT" "${PACK_ARGS[@]}"

echo ""
echo "Package written to: $OUTPUT/"
ls -lh "$OUTPUT"/*.nupkg
