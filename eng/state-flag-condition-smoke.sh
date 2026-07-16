#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/m020-flags"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- content validate flags --output "$out"
require_file "$out/validated-items.json"

