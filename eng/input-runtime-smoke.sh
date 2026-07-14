#!/usr/bin/env bash
set -euo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"
out="$repo_root/artifacts/scenarios/input-runtime-smoke"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- scenario run input.runtime-approach-and-interact-smoke --output "$out"
require_file "$out/result.json"
echo "input-runtime-smoke: passed"
