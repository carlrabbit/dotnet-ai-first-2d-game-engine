#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"

for path in \
  docs/specs/shared-asset-home-and-source-registry-contract.md \
  docs/specs/reusable-asset-discovery-profile-contract.md \
  docs/specs/reusable-asset-annotation-and-cleanup-contract.md \
  docs/specs/asset-campaign-and-batch-contract.md \
  docs/artifacts/asset-discovery-and-campaign-review-pack-artifact-contract.md; do
  require_file "$path"
done

output_dir="$repo_root/artifacts/assets/M028/documentation"
mkdir -p "$output_dir"
printf '%s\n' \
  '# M028 documentation diff summary' \
  '' \
  '- Added shared asset-home, discovery-profile, annotation/cleanup, campaign/batch, and review-pack authority.' \
  '- Indexed those authorities in specs, artifacts, content, terminology, engineering, CLI, milestones, decisions, review, and validation-tier documentation.' \
  '- Documented local-only authoring boundaries: no runtime/export dependency, no profile-bundle export, no remote sharing, and no promotion.' \
  '- Deferred broad documentation synchronization remains in the two supplied `.guide-sync/pending/` hints and was intentionally not read or resolved by this implementation task.' \
  > "$output_dir/diff-summary.md"
