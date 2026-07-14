#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
dotnet run --project src/Agentic2D.Tools -- content validate visuals --output artifacts/smoke/m015/visuals
dotnet run --project src/Agentic2D.Tools -- content validate entities --output artifacts/smoke/m015/entities
dotnet run --project src/Agentic2D.Tools -- content validate maps --output artifacts/smoke/m015/maps
echo "visual-content-smoke: passed"
