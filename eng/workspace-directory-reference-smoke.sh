#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"

before="$(capture_git_status)"
root="$(mktemp -d)"
trap 'rm -rf "$root"' EXIT
workspace="$root/minimal-reference"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- workspace create "$workspace" --template minimal-game --engine-directory "$repo_root" --engine-placement reference --output "$root/create"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- workspace validate "$workspace" --output "$root/validate"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- project run "$workspace" --scenario scenario.minimal.smoke --output "$root/run"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- run inspect "$root/run" --output "$root/inspection"
test ! -e "$workspace/engine-src"
assert_git_status_unchanged "$before"
echo "workspace-directory-reference-smoke: passed"
