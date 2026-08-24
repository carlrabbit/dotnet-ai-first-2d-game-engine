#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
if [[ "$#" -eq 2 && "$1" == "--milestone" ]]; then run_dotnet run --no-build --project "$repo_root/src/Agentic2D.Engineering" -- review run --milestone "$2"
elif [[ "$#" -eq 1 ]]; then run_dotnet run --no-build --project "$repo_root/src/Agentic2D.Engineering" -- review run "$1"
else echo 'usage: ./eng/review-run.sh --milestone <id> | <review-id-or-alias>' >&2; exit 2; fi
