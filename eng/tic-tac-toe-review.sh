#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
./eng/tic-tac-toe-smoke.sh
./eng/generated-sound-linkage-smoke.sh
./eng/m026-performance-report.sh
dotnet_cmd run --no-build --project src/Agentic2D.Engineering -- review request --id review.m026.geometry-diagnostics-and-autonomous-tic-tac-toe --subject "M026 geometry diagnostics and Autonomous Tic-Tac-Toe" --class visual-ux-artifact-quality --level required --source M026 --evidence artifacts/geometry/M026,artifacts/sound-linkage/M026,consumers/autonomous-tic-tac-toe/artifacts/runs,artifacts/performance/M026,artifacts/consumer-boundaries/M026,artifacts/validation/m026-smoke --triggers diagnostic-format,contrast-policy,board-mapping,controller-interaction,thinking-mark-particle,linkage,boundary-decisions
