#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/m020-persistent-world-review"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- project run . --scenario gameplay.persistent-world-resume-smoke --output "$out"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- run review "$out" --output "$out/review"
require_file "$out/review/review-summary.md"

