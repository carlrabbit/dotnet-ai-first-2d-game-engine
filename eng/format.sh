#!/usr/bin/env bash

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

require_file "$solution_path"

case "${1:-}" in
  "")
    dotnet_cmd format "$solution_path"
    ;;
  --verify)
    dotnet_cmd format "$solution_path" --verify-no-changes
    ;;
  *)
    fail "usage: ./eng/format.sh [--verify]"
    ;;
esac
