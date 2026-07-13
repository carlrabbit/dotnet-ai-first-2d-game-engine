#!/usr/bin/env bash
set -euo pipefail
./eng/entity-definition-smoke.sh
./eng/spatial-query-trigger-smoke.sh
./eng/interaction-smoke.sh
dotnet run --no-build --project src/Agentic2D.Tools -- review pack --input artifacts --output artifacts/review/m014
test -s artifacts/review/m014/review-manifest.json
