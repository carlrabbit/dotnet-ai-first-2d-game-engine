#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"

if [[ $# -ne 1 ]]; then
  echo "usage: $0 <temporary-root>" >&2
  exit 2
fi

root="$1"
workspace="$root/minimal-game"
mkdir -p "$root"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- workspace create "$workspace" --template minimal-game --engine-directory "$repo_root" --engine-placement reference --output "$root/create"
"$workspace/eng/validate.sh"
test -f "$workspace/eng/engine-bootstrap.env"
test ! -x "$workspace/eng/engine-bootstrap.env"
! grep -R -E "python|python3|jq|node|bun|ruby|perl" "$workspace/eng"
echo "m018-consumer-bootstrap-smoke: passed"
