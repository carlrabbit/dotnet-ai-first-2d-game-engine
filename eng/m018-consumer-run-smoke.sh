#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "usage: $0 <workspace>" >&2
  exit 2
fi

workspace="$1"
"$workspace/eng/run.sh" scenario.minimal.smoke
run="$workspace/artifacts/runs/scenario.minimal.smoke"
test -f "$run/run-manifest.json"
grep -q '"render": {' "$run/run-manifest.json"
grep -q '"status": "present"' "$run/run-manifest.json"
test -f "$run/render/render-result.json"
test -f "$run/render/render-snapshot.json"
test -f "$run/render/render-frame.json"
test -f "$run/render/render-items.jsonl"
test -f "$run/render/render-commands.jsonl"
test -f "$run/render/asset-bindings.json"
test -f "$run/render/render-diagnostics.json"
echo "m018-consumer-run-smoke: passed"
