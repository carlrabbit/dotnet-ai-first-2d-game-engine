#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
base="$repo_root/artifacts/performance/M026"
./eng/scaled-performance-smoke.sh
dotnet_cmd run -c Release --project src/Agentic2D.Engineering -- performance capture --label m026-before --output "$base/before"
dotnet_cmd run -c Release --project src/Agentic2D.Engineering -- performance capture --label m026-after --output "$base/after"
dotnet_cmd run -c Release --project src/Agentic2D.Engineering -- performance report --milestone M026 --before "$base/before" --after "$base/after" --output "$base"
require_file "$base/performance-report.md"
