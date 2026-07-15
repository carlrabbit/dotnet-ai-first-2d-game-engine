#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"

root="$(mktemp -d)"
trap 'rm -rf "$root"' EXIT
workspace="$root/minimal-game"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- workspace create "$workspace" --template minimal-game --engine-directory "$repo_root" --engine-placement reference --output "$root/create"
"$workspace/eng/validate.sh"
"$workspace/eng/run.sh" scenario.minimal.smoke
run="$workspace/artifacts/runs/scenario.minimal.smoke"
"$workspace/eng/inspect.sh" "$run"
"$workspace/eng/review.sh" "$run"
test -f "$run/run-manifest.json"
! grep -R -E "python|python3|jq|node|bun|ruby|perl" "$workspace/eng"
grep -q "\"render\": {" "$run/run-manifest.json"
grep -q "\"status\": \"present\"" "$run/run-manifest.json"
test -f "$run/render/render-result.json"
test -f "$run/render/render-snapshot.json"
test -f "$run/render/render-frame.json"
test -f "$run/render/render-items.jsonl"
test -f "$run/render/render-commands.jsonl"
test -f "$run/render/asset-bindings.json"
test -f "$run/render/render-diagnostics.json"
echo "workspace-minimal-game-run-smoke: passed"
