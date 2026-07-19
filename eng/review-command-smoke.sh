#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"

./eng/review-list.sh --milestone M027
./eng/review-show.sh review.m027.authoring-contracts-review-evidence-and-v060-migration
./eng/review-show.sh 1
./eng/review-show.sh review.m025.signal-passage-playable-vertical-slice
./eng/test-filter.sh EngineeringHost

review_check_directory="$(mktemp -d)"
trap 'rm -rf "$review_check_directory"' EXIT
if ! ./eng/review-check.sh --milestone M027 >"$review_check_directory/stdout" 2>"$review_check_directory/stderr"; then
  rg -q "review.m027.authoring-contracts-review-evidence-and-v060-migration.*pending" "$review_check_directory/stderr" || fail "M027 review check failed for a reason other than its pending M027 review"
  ! rg -q "M02[256]" "$review_check_directory/stderr" || fail "M027 review check inspected a historical milestone"
fi
