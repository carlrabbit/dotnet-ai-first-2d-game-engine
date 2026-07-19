#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"

./eng/review-migration-smoke.sh
./eng/geometry-review-pack-smoke.sh
./eng/generated-sound-review-pack-smoke.sh
./eng/scenario-diagnostics-smoke.sh
./eng/persistence-diagnostics-smoke.sh

pack="$repo_root/artifacts/review/M027/review-pack"
mkdir -p "$pack/geometry" "$pack/sound-linkage" "$pack/scenarios" "$pack/persistence" "$pack/performance" "$pack/captures"
cp -a "$repo_root/artifacts/geometry/M027/." "$pack/geometry/"
cp -a "$repo_root/artifacts/sound-linkage/M027/." "$pack/sound-linkage/"
cp -a "$repo_root/artifacts/review/M027/scenarios/." "$pack/scenarios/"
cp -a "$repo_root/artifacts/review/M027/persistence/." "$pack/persistence/"
if [[ -f "$repo_root/artifacts/performance/M026/performance-report.json" ]]; then
  cp -a "$repo_root/artifacts/performance/M026/performance-report.json" "$pack/performance/"
else
  jq -n '{status:"omitted",reason:"M027 preserves the M026 timing-authority policy; no new performance report is needed for this evidence pack."}' > "$pack/performance/omission.json"
fi
jq -n '{schema:"agentic2d.capture-status.v1",status:"not-captured",reason:"No supported graphics-capable environment was declared; structural geometry preview remains available."}' > "$pack/captures/capture-status.json"

members_file=$(mktemp "$repo_root/artifacts/review/M027/members.XXXXXX.json")
while IFS= read -r path; do
  relative=${path#"$pack/"}
  size=$(stat -c '%s' "$path")
  hash=$(sha256sum "$path" | awk '{print $1}')
  jq -n --arg path "$relative" --arg hash "$hash" --argjson size "$size" '{path:$path,size:$size,sha256:$hash,required:($path | startswith("captures/") | not),captureStatus:(if $path | startswith("captures/") then "not-captured" else "not-applicable" end)}'
done < <(find "$pack" -type f ! -name manifest.json ! -name index.md -print | sort) | jq -s . > "$members_file"
fingerprint=$(sha256sum "$members_file" | awk '{print "sha256:" $1}')
revision=$(git -C "$repo_root" rev-parse HEAD 2>/dev/null || printf 'working-tree')
jq -n --arg revision "$revision" --arg fingerprint "$fingerprint" --slurpfile members "$members_file" '{schema:"agentic2d.consumer-authoring-review-pack.v1",owningMilestone:"M027",canonicalReviewId:"review.m027.authoring-contracts-review-evidence-and-v060-migration",sourceRevision:$revision,schemaVersions:["agentic2d.geometry-review-pack.v1","agentic2d.generated-sound-review-pack.v1","agentic2d.scenario-diagnostics.v1","agentic2d.persistence-diagnostics.v1"],evidenceEntries:$members[0],omissions:["Graphical pixel capture is explicitly absent in this headless environment."],semanticPackFingerprint:$fingerprint}' > "$pack/manifest.json"
{
  printf '# M027 Consumer Authoring Review Pack\n\n'
  printf 'Canonical review: `review.m027.authoring-contracts-review-evidence-and-v060-migration`\n\n'
  printf 'This bounded pack contains structural geometry and generated-sound evidence, scenario and persistence diagnostics, M026-compatible performance evidence, and explicit capture absence.\n'
} > "$pack/index.md"
require_file "$pack/manifest.json"
require_file "$pack/index.md"
jq -e '.schema == "agentic2d.consumer-authoring-review-pack.v1" and (.evidenceEntries | length > 0) and .semanticPackFingerprint' "$pack/manifest.json" >/dev/null
