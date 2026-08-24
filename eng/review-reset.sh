#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
if [[ "$#" -ne 2 || "$1" != "--milestone" ]]; then echo 'usage: ./eng/review-reset.sh --milestone <id>' >&2; exit 2; fi
run_dotnet run --no-build --project "$repo_root/src/Agentic2D.Engineering" -- review reset --milestone "$2"
