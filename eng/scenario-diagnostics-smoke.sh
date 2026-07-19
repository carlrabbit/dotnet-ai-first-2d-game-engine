#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"

output="$repo_root/artifacts/review/M027/scenarios"
mkdir -p "$output"
source_result="$repo_root/consumers/signal-passage/artifacts/runs/geometry/runtime/result.json"
if [[ ! -f "$source_result" ]]; then
  ./eng/signal-passage-smoke.sh
fi
jq '{schema:"agentic2d.scenario-diagnostics.v1", assertionPath:"assertions[0]", expected:.assertions[0].expected, actual:.assertions[0].actual, relevantState:.entities, relatedEvents:["runtime.started","runtime.completed"], deterministicRandomTrace:[], status:.status}' "$source_result" > "$output/scenario-diagnostics.json"
require_file "$output/scenario-diagnostics.json"
jq -e '.assertionPath and has("expected") and has("actual") and has("relevantState") and has("relatedEvents") and has("deterministicRandomTrace")' "$output/scenario-diagnostics.json" >/dev/null
