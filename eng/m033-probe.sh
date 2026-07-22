#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
if [ "$#" -ne 1 ]; then fail "usage: ./eng/m033-probe.sh <scheduler|activity|travel|needs|fidelity|reconciliation|persistence|equivalence|standalone|multi-region|graphics>"; fi
out="$repo_root/artifacts/simulation/M033"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- simulation m033-multi-region --output "$out"
case "$1" in
  scheduler) jq -e '(.triggers | length) > 0 and ([.triggers[].status] | all(. != "delivered"))' "$out/queue-inspection.json" >/dev/null ;;
  activity) jq -e '.status == "passed" and .singleCompletion == true and .reservationIntegrity == true' "$out/invariants.json" >/dev/null ;;
  travel) jq -e '.route.cost == 7 and .route.edgeIds == ["edge.housing-forest"]' "$out/abstract-routes.jsonl" >/dev/null ;;
  needs) jq -e '.models | index("eat=1000000") and index("drink=1000000") and index("rest=1000000")' "$out/duration-models.json" >/dev/null ;;
  fidelity) jq -e '.oneDetailed == true and ([.regions[].fidelity] | map(select(. == "Detailed")) | length) == 1' "$out/executor-ownership.json" >/dev/null ;;
  reconciliation) jq -s -e 'map(select(.transition.status == "committed") | .transition.direction) | (index("abstract-to-detailed") != null and index("detailed-to-abstract") != null)' "$out/transition-events.jsonl" >/dev/null ;;
  persistence) jq -e '.status == "passed" and .noHalfTransition == true and .queueRestored == true' "$out/persistence-report.json" >/dev/null ;;
  equivalence) jq -e '.status == "passed" and .zeroDivergence == true and .deterministicRerun == true' "$out/equivalence-report.json" >/dev/null ;;
  standalone) jq -e '.status == "passed" and .completedDays == 30 and .safetyStopped == false' "$out/long-horizon-report.json" >/dev/null; jq -e '.status == "passed" and .processExitCode == 0' "$out/fresh-process.json" >/dev/null ;;
  multi-region) jq -e '.status == "passed" and .days == 30 and .exactOneDetailed == true' "$out/m033-manifest.json" >/dev/null ;;
  graphics) jq -e '.status == "skipped-not-graphics-capable" or .status == "passed"' "$out/graphical-evidence/environment.json" >/dev/null ;;
  *) fail "unknown M033 probe: $1" ;;
esac
