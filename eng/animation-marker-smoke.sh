#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
dotnet_cmd run --project src/Agentic2D.Tools -- animation project --scenario animation-overlay-marker-smoke --output artifacts/animation/overlay-marker
require_file artifacts/animation/overlay-marker/animation-markers.jsonl
test -s artifacts/animation/overlay-marker/animation-markers.jsonl
echo "animation-marker-smoke: passed"
