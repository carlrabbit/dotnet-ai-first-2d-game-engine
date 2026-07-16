#!/usr/bin/env bash
set -euo pipefail
"$(dirname "$0")/sound-content-smoke.sh"
"$(dirname "$0")/sound-marker-cue-smoke.sh"
"$(dirname "$0")/sound-loop-ownership-smoke.sh"
"$(dirname "$0")/gameplay-damage-resource-smoke.sh"
"$(dirname "$0")/gameplay-defeat-lifecycle-smoke.sh"
"$(dirname "$0")/gameplay-collection-atomicity-smoke.sh"
"$(dirname "$0")/gameplay-integrated-smoke.sh"
"$(dirname "$0")/gameplay-replay-smoke.sh"
echo "m019-smoke: passed"
