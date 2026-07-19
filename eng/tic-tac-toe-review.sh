#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
./eng/tic-tac-toe-smoke.sh
./eng/generated-sound-linkage-smoke.sh
./eng/m026-performance-report.sh
./eng/review-list.sh --milestone M026
