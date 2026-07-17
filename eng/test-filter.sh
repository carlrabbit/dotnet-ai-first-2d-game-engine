#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
if [ "$#" -ne 1 ]; then fail "usage: ./eng/test-filter.sh <filter>"; fi
dotnet_cmd test --project "$unit_test_project" --no-build --treenode-filter "/*/*/*/*[contains(@name,'$1')]"
