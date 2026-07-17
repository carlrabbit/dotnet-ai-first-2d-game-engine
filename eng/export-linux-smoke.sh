#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/m024-export"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- project export . --target linux-x64 --output "$out/game"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- export inspect "$out/game" --output "$out/inspect"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- export validate "$out/game" --output "$out/validate"
require_file "$out/game/agentic2d-game"
require_file "$out/game/native/libraylib.so"
require_file "$out/game/agentic2d.export.json"
require_file "$out/validate/export-validation.json"

negative_root="$(mktemp -d /tmp/agentic2d-m024-negative.XXXXXX)"
cp -a "$out/game" "$negative_root/missing-manifest"
mv "$negative_root/missing-manifest/agentic2d.export.json" "$negative_root/missing-manifest/agentic2d.export.json.missing"
if dotnet_cmd run --no-build --project src/Agentic2D.Tools -- export validate "$negative_root/missing-manifest" --output "$out/negative-missing-manifest"; then fail "missing manifest unexpectedly validated"; fi
cp -a "$out/game" "$negative_root/tampered-content"
printf 'tamper\n' >> "$negative_root/tampered-content/game/scenarios/smoke/runtime-smoke.json"
if dotnet_cmd run --no-build --project src/Agentic2D.Tools -- export validate "$negative_root/tampered-content" --output "$out/negative-tampered-content"; then fail "tampered content unexpectedly validated"; fi
cp -a "$out/game" "$negative_root/missing-managed"
mv "$negative_root/missing-managed/Agentic2D.Engine.dll" "$negative_root/missing-managed/Agentic2D.Engine.dll.missing"
if dotnet_cmd run --no-build --project src/Agentic2D.Tools -- export validate "$negative_root/missing-managed" --output "$out/negative-missing-managed"; then fail "missing managed file unexpectedly validated"; fi
cp -a "$out/game" "$negative_root/missing-native"
mv "$negative_root/missing-native/native/libraylib.so" "$negative_root/missing-native/native/libraylib.so.missing"
if dotnet_cmd run --no-build --project src/Agentic2D.Tools -- export validate "$negative_root/missing-native" --output "$out/negative-missing-native"; then fail "missing native file unexpectedly validated"; fi
