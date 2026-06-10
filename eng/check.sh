#!/usr/bin/env bash

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

"${repo_root}/eng/restore.sh"
"${repo_root}/eng/build.sh"
"${repo_root}/eng/test.sh"
"${repo_root}/eng/format.sh" --verify
