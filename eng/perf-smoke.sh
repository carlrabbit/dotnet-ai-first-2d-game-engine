#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
dotnet_cmd run -c Release --project src/Agentic2D.Engineering -- performance smoke
