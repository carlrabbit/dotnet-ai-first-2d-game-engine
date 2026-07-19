#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
./eng/signal-passage-smoke.sh
./eng/signal-passage-export.sh
./eng/review-list.sh --milestone M025
