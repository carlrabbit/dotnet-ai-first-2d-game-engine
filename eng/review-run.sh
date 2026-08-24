#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
if [[ "$#" -ne 1 ]]; then echo 'usage: ./eng/review-run.sh <review-id-or-alias>' >&2; exit 2; fi
run_dotnet run --no-build --project "$repo_root/src/Agentic2D.Engineering" -- review run "$1"
