#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"

mode="${1:?usage: m028-provider-smoke.sh <mode>}"
tmp_root="$(mktemp -d)"
trap 'rm -rf "$tmp_root"' EXIT
asset_home="$tmp_root/home"
out="$repo_root/artifacts/assets/M028/$mode"
tools=(dotnet_cmd run --no-build --project src/Agentic2D.Tools --)
run() { AGENTIC2D_ASSET_HOME="$asset_home" "${tools[@]}" "$@"; }
source_id() { sed -n 's/.*"id": "\(asset-source\.[^"]*\)".*/\1/p' "$out/source-added.json" | head -n1; }
add() { run asset source add game/assets/raw/samples --name m028-fixture --output "$out" >/dev/null; }
profile() { add; run asset source profile build "$(source_id)" --output "$out/profile" >/dev/null; }

case "$mode" in
  home)
    run asset home inspect --output "$out" >/dev/null
    test -f "$out/asset-home.json"
    run asset home clean --stale --output "$out" >/dev/null
    test -f "$out/asset-home-clean.json"
    ;;
  registry)
    add; id="$(source_id)"
    run asset source list --output "$out/list" >/dev/null
    run asset source show "$id" --output "$out/show" >/dev/null
    run asset source refresh "$id" --output "$out/refresh" >/dev/null
    test -f "$out/refresh/source-profile.json"
    ;;
  discovery|audio)
    profile
    for f in source-profile.json source-files.jsonl image-observations.jsonl audio-observations.jsonl region-candidates.jsonl duplicate-groups.json animation-candidates.json license-observations.json discovery-diagnostics.json; do test -f "$out/profile/$f"; done
    grep -q 'asset-image-observation.v1' "$out/profile/image-observations.jsonl"
    # The legacy M011 sample WAVs are intentionally diagnosed when their bytes
    # are not RIFF/WAVE; valid arbitrary WAV discovery is proven by the corpus.
    ;;
  annotations)
    profile; id="$(source_id)"
    decisions="$tmp_root/decisions.json"
    printf '%s\n' '[{"action":"correct-grid","target":{"file":"tile-atlas-smoke.png"},"reason":"verified tile cadence"}]' > "$decisions"
    run asset source annotation apply "$id" --decisions "$decisions" --output "$out/annotations" >/dev/null
    run asset source clean "$id" --generated-only --output "$out/clean" >/dev/null
    run asset source annotation list "$id" --output "$out/list" >/dev/null
    grep -q 'correct-grid' "$out/list/annotations.json"
    ;;
  campaign|batch|review-pack)
    profile; id="$(source_id)"; fp="$(sed -n 's/.*"profileFingerprint": "\([^"]*\)".*/\1/p' "$out/profile/source-profile.json" | head -n1)"
    campaign="$tmp_root/campaign.json"
    printf '{"schema":"agentic2d.asset-campaign.v1","id":"campaign.m028-a","sourceId":"%s","profileFingerprint":"%s","candidates":["region.a","region.b"]}\n' "$id" "$fp" > "$campaign"
    run asset campaign validate "$campaign" --output "$out/campaign/validate" >/dev/null
    run asset campaign status "$campaign" --output "$out/campaign/status" >/dev/null
    run asset campaign propose "$campaign" --output "$out/campaign/propose" >/dev/null
    test -f "$out/campaign/propose/unresolved-decisions.json"
    campaign_b="$tmp_root/campaign-b.json"
    printf '{"schema":"agentic2d.asset-campaign.v1","id":"campaign.m028-b","sourceId":"%s","profileFingerprint":"%s","candidates":["region.b","region.a"]}\n' "$id" "$fp" > "$campaign_b"
    before_profile="$(sha256sum "$out/profile/source-profile.json" | awk '{print $1}')"
    run asset campaign propose "$campaign_b" --output "$out/campaign-b/propose" >/dev/null
    test "$before_profile" = "$(sha256sum "$out/profile/source-profile.json" | awk '{print $1}')"
    if [ "$mode" = batch ]; then
      batch="$tmp_root/batch.json"; printf '{"schema":"agentic2d.asset-batch.v1","id":"batch.m028-a","candidates":["region.a"]}\n' > "$batch"
      run asset batch inventory "$batch" --output "$out/batch/inventory" >/dev/null
      run asset batch propose "$batch" --output "$out/batch/propose" >/dev/null
      run asset batch validate "$batch" --output "$out/batch/validate" >/dev/null
      run asset batch review-pack "$batch" --output "$out/batch/review" >/dev/null
      test -f "$out/batch/review/asset-review-pack/manifest.json"
    fi
    if [ "$mode" = review-pack ]; then
      run asset batch review-pack "$campaign" --output "$out/review" >/dev/null
      test -f "$out/review/asset-review-pack/images/source-preview.png"
      test -f "$out/review/asset-review-pack/audio/raw-preview.wav"
      test -f "$out/review/asset-review-pack/diagnostics/m029-readiness.md"
      mkdir -p "$repo_root/artifacts/assets/M028/m029-readiness"
      cp "$out/review/asset-review-pack/diagnostics/m029-readiness.md" "$repo_root/artifacts/assets/M028/m029-readiness/M029-readiness.md"
    fi
    ;;
  *) fail "unknown M028 smoke mode: $mode" ;;
esac

# The review request names durable evidence folders independently of the focused
# smoke modes.  Copy only generated evidence; the temporary asset home remains
# outside the repository and is removed by the trap above.
case "$mode" in
  home|registry) review_evidence="asset-home" ;;
  discovery|audio) review_evidence="discovery" ;;
  annotations) review_evidence="annotations-cleanup" ;;
  campaign) review_evidence="campaign-reuse" ;;
  batch) review_evidence="batch-proposals" ;;
  review-pack) review_evidence="review-pack" ;;
esac
mkdir -p "$repo_root/artifacts/assets/M028/$review_evidence"
if [ "$out" != "$repo_root/artifacts/assets/M028/$review_evidence" ]; then
  cp -R "$out"/. "$repo_root/artifacts/assets/M028/$review_evidence/"
fi
