#!/usr/bin/env bash
set -euo pipefail
"$(dirname "$0")/visual-content-smoke.sh"
"$(dirname "$0")/render-projection-smoke.sh"
"$(dirname "$0")/raylib-debug-client-smoke.sh"
echo "m015-smoke: passed (graphics status reported above)"
