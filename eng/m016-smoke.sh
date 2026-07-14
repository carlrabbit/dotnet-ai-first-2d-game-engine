#!/usr/bin/env bash
set -euo pipefail
"$(dirname "$0")/input-content-smoke.sh"
"$(dirname "$0")/input-mapping-smoke.sh"
"$(dirname "$0")/input-runtime-smoke.sh"
"$(dirname "$0")/input-replay-smoke.sh"
echo "m016-smoke: passed"
