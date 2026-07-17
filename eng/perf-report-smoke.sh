#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
base="artifacts/performance/m023"
rm -rf "$base"
./eng/perf-capture.sh --label before --output "$base/before"
./eng/perf-capture.sh --label after --output "$base/after"
./eng/perf-compare.sh "$base/before" "$base/after" --output "$base/comparison"
./eng/perf-report.sh --milestone M023 --before "$base/before" --after "$base/after" --output "$base"
require_file "$base/performance-report.json"
require_file "$base/performance-report.md"
