#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/m021-integrated"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- run review "$out" --output "$out/review"
require_file "$out/review/review-summary.md"
require_file "$out/review/review-manifest.json"
