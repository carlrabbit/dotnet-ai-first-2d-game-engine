#!/usr/bin/env bash
set -euo pipefail
for script in presentation-effect-smoke presentation-particle-smoke presentation-camera-smoke presentation-ui-text-smoke presentation-interaction-surface-smoke presentation-integrated-smoke presentation-replay-smoke presentation-post-load-smoke presentation-review-smoke; do "$(dirname "$0")/$script.sh"; done
echo "m021-smoke: passed"
