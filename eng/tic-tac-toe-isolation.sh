#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
root="$(mktemp -d /tmp/autonomous-tic-tac-toe-isolation.XXXXXX)"
workspace="$root/autonomous-tic-tac-toe"
cp -a "$repo_root/consumers/autonomous-tic-tac-toe" "$workspace"
cp -a "$repo_root" "$root/engine-src"
sed -i 's#"source": "../.."#"path": "../engine-src", "source": "../engine-src"#; s#"root": "../.."#"root": "../engine-src"#' "$workspace/agentic2d.workspace.json"
sed -i 's#ENGINE_PATH="../../.."#ENGINE_PATH="../../engine-src"#' "$workspace/eng/engine-bootstrap.env"
dotnet_cmd run --project "$root/engine-src/src/Agentic2D.Tools" -- workspace validate "$workspace" --output "$workspace/artifacts/isolation-validation"
dotnet_cmd run --project "$root/engine-src/src/Agentic2D.Tools" -- project run "$workspace" --scenario tic-tac-toe.workspace-smoke --output "$workspace/artifacts/isolation-run"
mkdir -p "$repo_root/artifacts/tic-tac-toe/isolation"
cp "$workspace/artifacts/isolation-validation/workspace-validation.json" "$repo_root/artifacts/tic-tac-toe/isolation/workspace-validation.json"
cp "$workspace/artifacts/isolation-run/run-manifest.json" "$repo_root/artifacts/tic-tac-toe/isolation/run-manifest.json"
