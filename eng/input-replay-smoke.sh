#!/usr/bin/env bash
set -euo pipefail
"$(dirname "$0")/input-mapping-smoke.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"
out="$repo_root/artifacts/input/replay-smoke"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- input replay --scenario input.runtime-approach-and-interact-smoke --recording "$repo_root/artifacts/input/mapping-smoke/input-recording.json" --output "$out"
require_file "$out/input-replay-result.json"
echo "input-replay-smoke: passed"
