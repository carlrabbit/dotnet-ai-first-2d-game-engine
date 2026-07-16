#!/usr/bin/env bash
set -euo pipefail
"$(dirname "$0")/save-canonical-roundtrip-smoke.sh"
"$(dirname "$0")/save-incompatibility-smoke.sh"
"$(dirname "$0")/save-resume-equivalence-smoke.sh"
"$(dirname "$0")/state-flag-condition-smoke.sh"
"$(dirname "$0")/state-switch-activation-smoke.sh"
"$(dirname "$0")/state-door-collision-smoke.sh"
"$(dirname "$0")/persistent-world-integrated-smoke.sh"
"$(dirname "$0")/persistent-world-review-smoke.sh"
echo "m020-smoke: passed"

