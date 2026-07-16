#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/gameplay-defeat"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- gameplay inspect --project . --scenario gameplay.defeat-lifecycle-smoke --output "$out"
require_file "$out/lifecycle-transitions.jsonl"
