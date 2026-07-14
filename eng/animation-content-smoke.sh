#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
dotnet_cmd run --project src/Agentic2D.Tools -- content validate animations --output artifacts/content/animations
require_file artifacts/content/animations/result.json
require_file artifacts/content/animations/diagnostics.json
echo "animation-content-smoke: passed"
