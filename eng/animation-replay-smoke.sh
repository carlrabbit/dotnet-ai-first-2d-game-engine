#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
dotnet_cmd run --project src/Agentic2D.Tools -- animation project --scenario animation-semantic-replay-smoke --output artifacts/animation/replay-a
dotnet_cmd run --project src/Agentic2D.Tools -- animation project --scenario animation-semantic-replay-smoke --output artifacts/animation/replay-b
cmp artifacts/animation/replay-a/animation-samples.jsonl artifacts/animation/replay-b/animation-samples.jsonl
cmp artifacts/animation/replay-a/animation-markers.jsonl artifacts/animation/replay-b/animation-markers.jsonl
cmp artifacts/animation/replay-a/animated-render-items.jsonl artifacts/animation/replay-b/animated-render-items.jsonl
echo "animation-replay-smoke: passed"
