#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"

# This is deliberately opt-in: it initializes raylib and therefore requires a
# supported desktop graphics session. Headless validation uses geometry-diagnostics-smoke.sh.
if [ -z "${DISPLAY:-}${WAYLAND_DISPLAY:-}" ]; then
  fail "geometry graphical capture requires DISPLAY or WAYLAND_DISPLAY; run this only in a graphics-capable environment"
fi
base="$repo_root/artifacts/geometry/M026"
input="$repo_root/tests/fixtures/geometry/all-supported-shapes.json"
capture="$base/all-supported-shapes/geometry-graphical-capture.png"
dotnet_cmd run --project src/Agentic2D.DebugClient.Raylib -- geometry --input "$input" --capture "$capture"
require_file "$capture"
require_file "${capture%.png}.metadata.json"
