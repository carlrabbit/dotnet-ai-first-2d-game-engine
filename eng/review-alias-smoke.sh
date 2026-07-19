#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"

./eng/review-list.sh --milestone M027
./eng/review-show.sh 1
./eng/test-filter.sh EngineeringHost
