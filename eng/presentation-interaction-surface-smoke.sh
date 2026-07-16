#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/m021-interaction-surface"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- presentation inspect --project . --scenario presentation.interaction-prompt-smoke --output "$out"
require_file "$out/interaction-prompts.jsonl"
require_file "$out/notifications.jsonl"
