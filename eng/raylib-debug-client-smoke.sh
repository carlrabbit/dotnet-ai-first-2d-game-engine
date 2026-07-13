#!/usr/bin/env bash
set -euo pipefail
# Prerequisites: a supported desktop display/session, raylib native library from Raylib-cs, and graphics access.
# Set DISPLAY or WAYLAND_DISPLAY. This command draws the real checked-in render atlas once; capture is opt-in.
if [[ -z "${DISPLAY:-}${WAYLAND_DISPLAY:-}" ]]; then echo "raylib-debug-client-smoke: skipped (no DISPLAY/WAYLAND_DISPLAY graphics environment)"; exit 0; fi
dotnet run --project src/Agentic2D.DebugClient.Raylib -- scenario --scenario interaction.npc-smoke --capture artifacts/smoke/m015/raylib/frame.png
test -s artifacts/smoke/m015/raylib/frame.png
echo "raylib-debug-client-smoke: passed"
