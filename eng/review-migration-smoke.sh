#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"

./eng/review-command-smoke.sh
./eng/review-alias-smoke.sh
rg -q './eng/review-show.sh <review-id-or-alias>' docs/engineering/human-review-workflow.md
rg -q './eng/review-reopen.sh <review-id-or-alias>' docs/engineering/human-review-workflow.md
rg -q 'review-record.sh <review-id-or-alias> <decision>' docs/engineering/human-review-workflow.md
dotnet_cmd run --no-build --project src/Agentic2D.Engineering -- review migration-report --milestone M027
require_file "$repo_root/artifacts/review-migration/M027/review-migration-report.json"
require_file "$repo_root/artifacts/review-migration/M027/review-migration-report.md"
jq -e '.schema == "agentic2d.review-migration-report.v1" and any(.entries[]; .id == "review.m027.authoring-contracts-review-evidence-and-v060-migration" and (.classification == "active-owned" or .classification == "historical-completed"))' "$repo_root/artifacts/review-migration/M027/review-migration-report.json" >/dev/null
