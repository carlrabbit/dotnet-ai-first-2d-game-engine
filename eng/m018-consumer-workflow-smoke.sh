#!/usr/bin/env bash
set -euo pipefail

root="$(mktemp -d)"
trap 'rm -rf "$root"' EXIT
dir="$(dirname "$0")"
"$dir/m018-consumer-bootstrap-smoke.sh" "$root"
"$dir/m018-consumer-run-smoke.sh" "$root/minimal-game"
"$dir/m018-consumer-review-smoke.sh" "$root/minimal-game"
echo "m018-consumer-workflow-smoke: passed"
