#!/usr/bin/env bash

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

tools_project="src/Agentic2D.Tools/Agentic2D.Tools.csproj"
source_metadata="${repo_root}/game/assets/metadata/tile-atlas-smoke.asset.json"
source_review="${repo_root}/game/assets/reviews/tile-atlas-smoke.review.json"
output_root="${repo_root}/artifacts/asset-review"
dry_run_dir="${output_root}/dry-run"
isolation_dir="${output_root}/isolation"
apply_dir="${output_root}/applied"
stale_dir="${output_root}/stale"
validation_dir="${output_root}/validation"

require_file "$tools_project"
require_file "$source_metadata"
require_file "$source_review"

before_status="$(capture_git_status)"
before_fingerprint="$(sha256sum "$source_metadata" | awk '{print $1}')"

dotnet_cmd run --no-build --project "$tools_project" -- asset review apply --decisions game/assets/reviews/tile-atlas-smoke.review.json --dry-run --output "$dry_run_dir"

require_file "${dry_run_dir}/result.json"
require_file "${dry_run_dir}/diagnostics.json"
require_file "${dry_run_dir}/mutation-plan.json"
require_file "${dry_run_dir}/validation-result.json"
require_file "${dry_run_dir}/proposed-metadata.json"

after_dry_run_fingerprint="$(sha256sum "$source_metadata" | awk '{print $1}')"
[ "$before_fingerprint" = "$after_dry_run_fingerprint" ] || fail "dry-run mutated source metadata"

mkdir -p "$isolation_dir"
isolated_metadata_rel="artifacts/asset-review/isolation/tile-atlas-smoke.asset.json"
isolated_review_rel="artifacts/asset-review/isolation/tile-atlas-smoke.review.json"
isolated_stale_review_rel="artifacts/asset-review/isolation/tile-atlas-smoke.stale.review.json"
isolated_metadata="${repo_root}/${isolated_metadata_rel}"
isolated_review="${repo_root}/${isolated_review_rel}"
isolated_stale_review="${repo_root}/${isolated_stale_review_rel}"

cp "$source_metadata" "$isolated_metadata"
cp "$source_review" "$isolated_review"

perl -0pi -e 's/\n  "humanReview": /\n  "unrelatedMetadata": {\n    "preserveMe": "yes"\n  },\n  "humanReview": /' "$isolated_metadata"

isolated_fingerprint="$(sha256sum "$isolated_metadata" | awk '{print $1}')"
perl -0pi -e 's#"metadataPath": ".*?"#"metadataPath": "artifacts/asset-review/isolation/tile-atlas-smoke.asset.json"#' "$isolated_review"
perl -0pi -e 's#"expectedSourceFingerprint": ".*?"#"expectedSourceFingerprint": "sha256:'"$isolated_fingerprint"'"#' "$isolated_review"

dotnet_cmd run --no-build --project "$tools_project" -- asset review apply --decisions "$isolated_review_rel" --output "$apply_dir"

require_file "${apply_dir}/result.json"
require_file "${apply_dir}/diagnostics.json"
require_file "${apply_dir}/mutation-plan.json"
require_file "${apply_dir}/validation-result.json"

grep -q '"walkable"' "$isolated_metadata" || fail "isolated apply did not approve walkable"
grep -q '"unrelatedMetadata"' "$isolated_metadata" || fail "isolated apply did not preserve unrelated metadata"
grep -q '"preserveMe": "yes"' "$isolated_metadata" || fail "isolated apply changed unrelated metadata"

dotnet_cmd run --no-build --project "$tools_project" -- content validate "$isolated_metadata_rel" --output "$validation_dir"
grep -q '"status": "passed"' "${validation_dir}/result.json" || fail "post-apply asset validation did not pass"

cp "$isolated_review" "$isolated_stale_review"
perl -0pi -e 's#"expectedSourceFingerprint": ".*?"#"expectedSourceFingerprint": "sha256:0000000000000000000000000000000000000000000000000000000000000000"#' "$isolated_stale_review"
before_stale_attempt="$(sha256sum "$isolated_metadata" | awk '{print $1}')"
if dotnet_cmd run --no-build --project "$tools_project" -- asset review apply --decisions "$isolated_stale_review_rel" --output "$stale_dir"; then
  fail "stale fingerprint apply unexpectedly succeeded"
fi
require_file "${stale_dir}/result.json"
require_file "${stale_dir}/diagnostics.json"
after_stale_attempt="$(sha256sum "$isolated_metadata" | awk '{print $1}')"
[ "$before_stale_attempt" = "$after_stale_attempt" ] || fail "stale fingerprint rejection still modified metadata"

assert_git_status_unchanged "$before_status"
