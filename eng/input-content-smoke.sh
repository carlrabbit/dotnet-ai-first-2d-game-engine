#!/usr/bin/env bash
set -euo pipefail
"$(dirname "$0")/content-validate.sh" input-maps
"$(dirname "$0")/content-validate.sh" input-sequences
echo "input-content-smoke: passed"
