#!/usr/bin/env bash

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

require_file "$unit_test_project"
dotnet_cmd test --project "$unit_test_project" --no-build
