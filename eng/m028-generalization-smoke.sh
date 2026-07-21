#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"

"${repo_root}/eng/m028-m011-audit.sh"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- asset discovery self-test --output "$repo_root/artifacts/assets/M028/generalization"
for name in discovery-test-corpus-manifest.json image-discovery-results.json audio-discovery-results.json metamorphic-test-results.json annotation-application-results.json unknown-library-acceptance.json two-campaign-isolation.json cleanup-and-rebuild.json discovery-validation-summary.md; do
  cp "$repo_root/artifacts/assets/M028/generalization/$name" "$repo_root/artifacts/assets/M028/$name"
done
require_file "$repo_root/artifacts/assets/M028/m011-capability-audit.json"
require_file "$repo_root/artifacts/assets/M028/generalization/discovery-test-corpus-manifest.json"
require_file "$repo_root/artifacts/assets/M028/generalization/image-discovery-results.json"
require_file "$repo_root/artifacts/assets/M028/generalization/audio-discovery-results.json"
require_file "$repo_root/artifacts/assets/M028/generalization/metamorphic-test-results.json"
require_file "$repo_root/artifacts/assets/M028/generalization/unknown-library-acceptance.json"
grep -q '"status": "passed"' "$repo_root/artifacts/assets/M028/generalization/unknown-library-acceptance.json"
grep -q '"mutationChangesFingerprint": true' "$repo_root/artifacts/assets/M028/generalization/metamorphic-test-results.json"
grep -q '"forbiddenFixtureIdentifierPresent": false' "$repo_root/artifacts/assets/M028/generalization/unknown-library-acceptance.json"
grep -q '"status": "passed"' "$repo_root/artifacts/assets/M028/generalization/annotation-application-results.json"
grep -q '"status": "passed"' "$repo_root/artifacts/assets/M028/generalization/two-campaign-isolation.json"
