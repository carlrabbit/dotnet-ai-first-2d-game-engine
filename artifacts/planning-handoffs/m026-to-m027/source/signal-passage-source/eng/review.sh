#!/usr/bin/env bash
set -euo pipefail
workspace_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
exec "$workspace_root/eng/agentic2d.sh" run review "${1:?run directory required}" --output "${1:?run directory required}/review"
