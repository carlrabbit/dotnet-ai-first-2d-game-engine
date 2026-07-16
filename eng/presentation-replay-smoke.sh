#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
a="$repo_root/artifacts/smoke/m021-replay-a"
b="$repo_root/artifacts/smoke/m021-replay-b"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- presentation inspect --project . --scenario presentation.persistent-world-player-facing-smoke --output "$a"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- presentation inspect --project . --scenario presentation.persistent-world-player-facing-smoke --output "$b"
cmp "$a/presentation-composition.jsonl" "$b/presentation-composition.jsonl"
cmp "$a/particle-samples.jsonl" "$b/particle-samples.jsonl"
