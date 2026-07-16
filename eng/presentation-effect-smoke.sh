#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/m021-effects"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- content validate effects --output "$out/content"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- presentation inspect --project . --scenario presentation.effect-damage-smoke --output "$out/presentation"
require_file "$out/presentation/effect-definitions.json"
require_file "$out/presentation/effect-requests.jsonl"
require_file "$out/presentation/effect-instances.jsonl"
