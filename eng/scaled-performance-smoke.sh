#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
dotnet_cmd run -c Release --project src/Agentic2D.Engineering -- performance capture --label m026-scaled --output "$repo_root/artifacts/performance/M026/capture"
require_file "$repo_root/artifacts/performance/M026/capture/performance-capture.json"
