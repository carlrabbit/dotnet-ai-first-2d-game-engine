#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/m020-door"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- project run . --scenario gameplay.persistent-world-resume-smoke --output "$out"
require_file "$out/persistent-world/projection-invalidations.jsonl"
require_file "$out/persistent-world/door-transitions.jsonl"

