#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
require_command git

root="$(mktemp -d)"
trap 'rm -rf "$root"' EXIT
fixture="$root/engine-fixture"
git clone --no-local "$repo_root" "$fixture" >/dev/null
git -C "$fixture" config user.email smoke@example.invalid
git -C "$fixture" config user.name smoke
revision="$(git -C "$fixture" rev-parse HEAD)"
workspace="$root/minimal-git"
dotnet_cmd run --project src/Agentic2D.Tools -- workspace create "$workspace" --template minimal-game --engine-git "$fixture" --engine-revision "$revision" --output "$root/create"
test "$(python3 - "$workspace/agentic2d.workspace.json" <<'PY'
import json, sys
print(json.load(open(sys.argv[1]))['engine']['resolved'])
PY
)" = "$revision"
dotnet_cmd run --project src/Agentic2D.Tools -- workspace validate "$workspace" --output "$root/validate"
dotnet_cmd run --project src/Agentic2D.Tools -- project run "$workspace" --scenario scenario.minimal.smoke --output "$root/run"
echo "workspace-local-git-smoke: passed"
