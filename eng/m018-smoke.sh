#!/usr/bin/env bash
set -euo pipefail
"$(dirname "$0")/workspace-directory-reference-smoke.sh"
"$(dirname "$0")/workspace-directory-copy-smoke.sh"
"$(dirname "$0")/workspace-local-git-smoke.sh"
"$(dirname "$0")/workspace-minimal-game-run-smoke.sh"
echo "m018-smoke: passed"
