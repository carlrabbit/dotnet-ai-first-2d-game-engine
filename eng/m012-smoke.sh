#!/usr/bin/env bash
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

"${repo_root}/eng/behavior-smoke.sh"
"${repo_root}/eng/grid-spatial-smoke.sh"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- review pack --input artifacts --output "${repo_root}/artifacts/review/m012"
require_file "${repo_root}/artifacts/review/m012/review-manifest.json"
