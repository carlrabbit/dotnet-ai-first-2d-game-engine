#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
solution_path="${repo_root}/dotnet-ai-first-2d-game-engine.slnx"
unit_test_project="${repo_root}/tests/unit/Agentic2D.Tests.Unit/Agentic2D.Tests.Unit.csproj"

fail() {
  printf 'error: %s\n' "$*" >&2
  exit 1
}

require_command() {
  command -v "$1" >/dev/null 2>&1 || fail "required command not found: $1"
}

require_file() {
  [ -f "$1" ] || fail "required file not found: $1"
}

dotnet_cmd() {
  require_command dotnet
  DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/tmp/dotnet-home}" dotnet "$@"
}

cd "$repo_root"
