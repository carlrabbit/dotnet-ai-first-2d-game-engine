#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
dotnet_cmd run --project src/Agentic2D.Tools -- render project --scenario animation-semantic-replay-smoke --tick final --output artifacts/render/animation-replay
require_file artifacts/render/animation-replay/animated-render-items.jsonl
require_file artifacts/render/animation-replay/render-result.json
echo "animated-render-smoke: passed"
