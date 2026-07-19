#!/usr/bin/env bash
set -euo pipefail
workspace_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source "$workspace_root/eng/engine-bootstrap.env"
engine_root="$(cd "$workspace_root/eng/$ENGINE_PATH" && pwd)"
exec dotnet run --no-build --project "$engine_root/$ENGINE_TOOLS_PROJECT" -- "$@"
