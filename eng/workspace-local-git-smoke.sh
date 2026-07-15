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
cp -a "$repo_root/src/Agentic2D.Workspaces" "$fixture/src/"
cp "$repo_root/src/Agentic2D.Tools/Agentic2D.Tools.csproj" "$fixture/src/Agentic2D.Tools/"
cp "$repo_root/src/Agentic2D.Tools/ToolsCli.cs" "$fixture/src/Agentic2D.Tools/"
cp "$repo_root/src/Agentic2D.Animation/Animation.cs" "$fixture/src/Agentic2D.Animation/"
cp "$repo_root/src/Agentic2D.Rendering/Rendering.cs" "$fixture/src/Agentic2D.Rendering/"
cp "$repo_root/src/Agentic2D.Validation/VisualDefinitionContent.cs" "$fixture/src/Agentic2D.Validation/"
git -C "$fixture" add -A
git -C "$fixture" commit -m "local M018 engine fixture" >/dev/null
workspace="$root/minimal-git"
revision="$(git -C "$fixture" rev-parse HEAD)"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- workspace create "$workspace" --template minimal-game --engine-git "$fixture" --engine-revision "$revision" --output "$root/create"
grep -q "\"resolved\": \"$revision\"" "$workspace/agentic2d.workspace.json"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- workspace validate "$workspace" --output "$root/validate"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- project run "$workspace" --scenario scenario.minimal.smoke --output "$root/run"
"$workspace/eng/validate.sh"
echo "workspace-local-git-smoke: passed"
