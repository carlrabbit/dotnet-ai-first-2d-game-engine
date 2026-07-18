#!/usr/bin/env bash
set -euo pipefail
workspace_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
scenario="${1:?scenario ID required}"
exec "$workspace_root/eng/agentic2d.sh" project run "$workspace_root" --scenario "$scenario" --output "$workspace_root/artifacts/runs/$scenario"
