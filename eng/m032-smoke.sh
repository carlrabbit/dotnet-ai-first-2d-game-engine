#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
dotnet_cmd run --no-build --project src/Agentic2D.Engineering -- suite m032-smoke "$@"
