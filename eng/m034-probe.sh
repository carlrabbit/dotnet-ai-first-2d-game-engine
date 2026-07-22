#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
if [ "$#" -ne 1 ]; then fail "usage: ./eng/m034-probe.sh <construction|water|farm|comfort|maintenance|road|alerts|operations|persistence|settlement>"; fi
out="$repo_root/artifacts/simulation/M034"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- simulation m034-settlement --output "$out"
case "$1" in
  construction) jq -e '.plans | any(.state == "Cancelled") and any(.state == "Completed")' "$out/construction-plans.json" >/dev/null ;;
  water) jq -e '.zeroLoss == true' "$out/water-flow.json" >/dev/null ;;
  farm) jq -e '.growthAcrossFidelity == true' "$out/farm-production.json" >/dev/null ;;
  comfort) jq -e '.finiteReservations == true' "$out/comfort-capacity.json" >/dev/null ;;
  maintenance) jq -e '.failureRecovered == true' "$out/maintenance.json" >/dev/null ;;
  road) jq -e '.detailedCost == .abstractCost and .sharedAuthoredModifier == true' "$out/roads.json" >/dev/null ;;
  alerts) jq -e 'all(.alert; .causes | length > 0)' "$out/alerts.jsonl" >/dev/null ;;
  operations) jq -e '.activeRegion == "region.fields" and (.regions | length == 3)' "$out/world-dashboard.json" >/dev/null ;;
  persistence) jq -e '.status == "passed" and .freshProcessRequired == true' "$out/persistence-report.json" >/dev/null ;;
  settlement) jq -e '.status == "passed" and .days == 14 and .exactOneDetailed == true' "$out/m034-manifest.json" >/dev/null ;;
  *) fail "unknown M034 probe: $1" ;;
esac
