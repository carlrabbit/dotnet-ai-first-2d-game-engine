#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
base="$repo_root/artifacts/performance/M025"
./eng/perf-capture.sh --label m025-before --output "$base/before"
./eng/perf-capture.sh --label m025-after --output "$base/after"
./eng/perf-report.sh --milestone M025 --before "$base/before" --after "$base/after" --output "$base"
printf '\nSignal Passage expected cost: more entities, behavior executions, spatial work, geometry render items, HUD text, synthesized content bytes, and export bytes. These are expected workload increases, not same-work regressions.\n' >> "$base/performance-report.md"
