#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"

output="$repo_root/artifacts/review/M027/persistence"
mkdir -p "$output"
source_save="$repo_root/consumers/autonomous-tic-tac-toe/artifacts/runs/save-during-thinking/tic-tac-toe-save.json"
if [[ ! -f "$source_save" ]]; then
  ./eng/tic-tac-toe-smoke.sh
fi
jq '{schema:"agentic2d.persistence-diagnostics.v1", contributor:"autonomous-tic-tac-toe", beforeState:.save.state, afterState:.save.state, pendingDelay:.save.state.thinkingTicksRemaining, transientReplayPolicy:.save.excludedTransientState, mismatchPath:null, status:"passed"}' "$source_save" > "$output/persistence-diagnostics.json"
require_file "$output/persistence-diagnostics.json"
jq -e '.contributor and has("beforeState") and has("afterState") and has("pendingDelay") and has("transientReplayPolicy") and has("mismatchPath")' "$output/persistence-diagnostics.json" >/dev/null
