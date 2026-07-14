#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"

root="$(mktemp -d)"
trap 'rm -rf "$root"' EXIT
workspace="$root/minimal-copy"
dotnet_cmd run --project src/Agentic2D.Tools -- workspace create "$workspace" --template minimal-game --engine-directory "$repo_root" --engine-placement copy --output "$root/create"
test -f "$workspace/engine-src/src/Agentic2D.Tools/Agentic2D.Tools.csproj"
test ! -d "$workspace/engine-src/.git"
test ! -d "$workspace/engine-src/artifacts"
dotnet_cmd run --project src/Agentic2D.Tools -- workspace validate "$workspace" --output "$root/validate"
dotnet_cmd run --project src/Agentic2D.Tools -- project run "$workspace" --scenario scenario.minimal.smoke --output "$root/run"
echo "workspace-directory-copy-smoke: passed"
