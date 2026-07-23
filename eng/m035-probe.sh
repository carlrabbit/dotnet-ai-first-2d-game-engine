#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
if [ "$#" -ne 1 ]; then fail "usage: ./eng/m035-probe.sh <performance|health|deadlock|fault|compatibility|recovery|reproduction|session|readiness|campaign>"; fi
out="$repo_root/artifacts/readiness/M035"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- simulation m035-readiness --output "$out"
case "$1" in
  performance) jq -e '.schema == "agentic2d.performance-budget.v1" and (.metrics | length >= 15)' "$out/performance-budgets.json" >/dev/null ;;
  health) jq -e '.schema == "agentic2d.runtime-health-summary.v1" and .hasFailure == false' "$out/runtime-health-summary.json" >/dev/null ;;
  deadlock) jq -e '.status == "passed" and (.detectors | length >= 10)' "$out/deadlock-livelock-report.json" >/dev/null ;;
  fault) jq -e '.disabledByDefault == true and .testCompositionOnly == true and (.cases | length >= 13)' "$out/fault-campaign-report.json" >/dev/null ;;
  compatibility) jq -e '.status == "passed" and .currentSchema == 2' "$out/save-compatibility-matrix.json" >/dev/null ;;
  recovery) jq -e '.previousGoodPreserved == true and .corruptRejected == true' "$out/save-recovery-report.json" >/dev/null ;;
  reproduction) jq -e '(.bundles | length >= 13) and all(.bundles[]; .sanitized and .bounded)' "$out/reproduction-bundle-index.json" >/dev/null ;;
  session) jq -e '(.sessions | length) == 1 and .sessions[0].diagnostics == "continuous-bounded"' "$out/tester-session-index.json" >/dev/null ;;
  readiness) jq -e '.schema == "agentic2d.m035.manifest.v1" and .retention.logs == 64' "$out/m035-manifest.json" >/dev/null ;;
  campaign)
    jq -e '.completedDays == 365 and .transitions >= 1000 and .saveLoadCycles == 250 and .earlyTermination == false' "$out/headless-soak-report.json" >/dev/null
    for campaign in population-entity pathfinding-work abstract-queue fidelity-transition persistence-cycle infrastructure-shortage headless-365-day; do
      jq -e --arg id "$campaign" '.schema == "agentic2d.stress-campaign.v1" and .campaign == $id and .status == "passed" and .expectedCaseCount == .completedCaseCount and .failedCaseCount == 0 and .partial == false' "$out/campaigns/$campaign/verify.json" >/dev/null
      jq -e --arg id "$campaign" '.schema == "agentic2d.stress-campaign.v1" and .campaign == $id and .status == "passed" and .earlyTermination == false' "$out/campaigns/$campaign/receipts/deterministic-reference.json" >/dev/null
    done
    ;;
  *) fail "unknown M035 probe: $1" ;;
esac
