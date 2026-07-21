#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/simulation/M031"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- simulation wood-workflow --output "$out"
jq -s -e 'all(.[]; .status == "accepted") and any(.[]; .type == "domain.fact.ResourceHarvested")' "$out/command-results.jsonl" >/dev/null
jq -s -e 'any(.[]; .type == "ResourceHarvested") and any(.[]; .type == "ResourceDeposited")' "$out/domain-events.jsonl" >/dev/null
