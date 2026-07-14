#!/usr/bin/env bash
set -euo pipefail
"$(dirname "$0")/animation-content-smoke.sh"
"$(dirname "$0")/animation-sampling-smoke.sh"
"$(dirname "$0")/animation-marker-smoke.sh"
"$(dirname "$0")/animated-render-smoke.sh"
"$(dirname "$0")/animation-replay-smoke.sh"
echo "m017-smoke: passed"
