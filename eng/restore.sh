#!/usr/bin/env bash

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

require_file "$solution_path"
dotnet_cmd restore "$solution_path"
