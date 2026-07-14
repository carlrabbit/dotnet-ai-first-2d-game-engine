#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"

root="$(mktemp -d)"
trap 'rm -rf "$root"' EXIT
workspace="$root/minimal-game"
dotnet_cmd run --project src/Agentic2D.Tools -- workspace create "$workspace" --template minimal-game --engine-directory "$repo_root" --engine-placement reference --output "$root/create"
"$workspace/eng/validate.sh"
"$workspace/eng/run.sh" scenario.minimal.smoke
run="$workspace/artifacts/runs/scenario.minimal.smoke"
"$workspace/eng/inspect.sh" "$run"
"$workspace/eng/review.sh" "$run"
test -f "$run/run-manifest.json"
echo "workspace-minimal-game-run-smoke: passed"
