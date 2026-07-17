#!/usr/bin/env bash
set -euo pipefail
workspace_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
exec "$workspace_root/eng/agentic2d.sh" workspace validate "$workspace_root" --output "$workspace_root/artifacts/validation"
