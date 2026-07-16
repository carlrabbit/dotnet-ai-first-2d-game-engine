#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/m020-save-incompatibility"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- project run . --scenario gameplay.persistent-world-resume-smoke --output "$out/run"
if dotnet_cmd run --no-build --project src/Agentic2D.Tools -- save validate "$out/run/save" --project project.incompatible --output "$out/validation"; then fail "incompatible save unexpectedly validated"; fi
require_file "$out/validation/save-validation.json"

