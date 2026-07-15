#!/usr/bin/env bash
set -euo pipefail
"$(dirname "$0")/m018-directory-reference-smoke.sh"
"$(dirname "$0")/m018-directory-copy-smoke.sh"
"$(dirname "$0")/m018-local-git-smoke.sh"
"$(dirname "$0")/m018-consumer-workflow-smoke.sh"
echo "m018-smoke: passed"
