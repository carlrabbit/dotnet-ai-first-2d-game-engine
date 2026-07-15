#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "usage: $0 <workspace>" >&2
  exit 2
fi

workspace="$1"
run="$workspace/artifacts/runs/scenario.minimal.smoke"
test -f "$run/run-manifest.json"
"$workspace/eng/inspect.sh" "$run"
"$workspace/eng/review.sh" "$run"
test -f "$run/inspection/run-inspection.json"
test -f "$run/review/review-manifest.json"
echo "m018-consumer-review-smoke: passed"
