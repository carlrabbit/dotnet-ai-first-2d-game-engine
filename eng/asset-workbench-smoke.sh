#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"

mode="${1:-integrated}"
tmp_root="$(mktemp -d)"
trap 'rm -rf "$tmp_root"' EXIT
home="$tmp_root/home"
root="$repo_root/artifacts/assets/M029"
tools=(dotnet_cmd run --no-build --project src/Agentic2D.Tools --)
run() { AGENTIC2D_ASSET_HOME="$home" "${tools[@]}" "$@"; }
capture_workbench() { dotnet_cmd run --no-build --project src/Agentic2D.DebugClient.Raylib -- asset-workbench --session "$home/sessions/$session/review-session.json" --commands "$commands" "$@"; }

setup() {
  local setup="$tmp_root/setup" id fp
  run asset source add game/assets/raw/samples --name m029-fixture --output "$setup" >/dev/null
  id="$(sed -n 's/.*"id": "\(asset-source\.[^"]*\)".*/\1/p' "$setup/source-added.json" | head -1)"
  run asset source profile build "$id" --output "$setup/profile" >/dev/null
  fp="$(sed -n 's/.*"profileFingerprint": "\([^"]*\)".*/\1/p' "$setup/profile/source-profile.json" | head -1)"
  campaign="$tmp_root/campaign.json"
  printf '{"schema":"agentic2d.asset-campaign.v1","id":"campaign.m029","sourceId":"%s","profileFingerprint":"%s","candidates":["candidate.static","candidate.animation","candidate.collision","candidate.audio"]}\n' "$id" "$fp" > "$campaign"
  run asset workbench --campaign "$campaign" --headless --output "$root/workbench" >/dev/null
  session="$(sed -n 's/.*"id": "\(workbench-session\.[^"]*\)".*/\1/p' "$root/workbench/review-session.json" | head -1)"
}

setup
case "$mode" in
  session|aliases)
    run asset workbench resume "$session" --output "$root/session" >/dev/null
    run asset workbench --session "$session" --command '2' --output "$root/aliases" >/dev/null
    test -f "$root/aliases/aliases.json"
    ;;
  input|rdp|mouse|equivalence)
    run asset workbench --session "$session" --text 2 --output "$root/input/before" >/dev/null
    grep -q '"textBuffer": "2"' "$root/input/before/input-state.json"
    run asset workbench --session "$session" --submit --output "$root/input/submit" >/dev/null
    run asset workbench --session "$session" --text 12 --backspace --enter --output "$root/input/correction" >/dev/null
    run asset workbench --session "$session" --paste 'find chest' --submit --output "$root/input/paste" >/dev/null
    run asset workbench --session "$session" --composition 2 --submit --output "$root/input/rdp" >/dev/null
    run asset workbench --session "$session" --select 2 --output "$root/input/mouse" >/dev/null
    run asset workbench --session "$session" --text 99 --submit --output "$root/input/invalid" >/dev/null
    grep -q 'Invalid workbench command\|stale or unavailable' "$root/input/invalid/input-state.json"
    ;;
  decisions|consequence)
    run asset workbench --session "$session" --select 1 --decision accept-proposal --reason approved --output "$root/decisions/accept" >/dev/null
    run asset workbench --session "$session" --select 3 --decision approve-with-corrections --consequence presentation-only --reason 'consequence collision presentation only' --output "$root/decisions/consequence" >/dev/null
    test -f "$home/sessions/$session/review-decisions.jsonl"
    cp "$home/sessions/$session/review-decisions.jsonl" "$root/decisions/review-decisions.jsonl"
    ;;
  preview|recovery|audio)
    run asset preview-host "$session" --output "$root/preview" >/dev/null
    run asset workbench --session "$session" --preview-restart --focus lost --output "$root/recovery" >/dev/null
    run asset preview-host "$session" --malformed --output "$root/preview/malformed" >/dev/null
    test -f "$root/preview/preview-scene.json"
    ;;
  graphical)
    run asset preview-host "$session" --output "$root/preview" >/dev/null
    commands="$root/preview/workbench-input.jsonl"
    if [ -n "${DISPLAY:-}${WAYLAND_DISPLAY:-}" ]; then
      run asset workbench ui "$session" --commands "$commands" --capture "$root/preview/workbench.png" --frames 3 >/dev/null
      run asset preview-host ui "$session" --output "$root/preview" --capture "$root/preview/engine-preview.png" >/dev/null
      test -s "$root/preview/workbench.png"
      test -s "$root/preview/engine-preview.png"
      printf '{"schema":"agentic2d.asset-workbench-graphical-smoke.v1","status":"passed","captures":["workbench.png","engine-preview.png"],"renderer":"raylib"}\n' > "$root/preview/graphical-smoke.json"
    else
      printf '{"schema":"agentic2d.asset-workbench-graphical-smoke.v1","status":"skipped","reason":"no graphical session is available"}\n' > "$root/preview/graphical-smoke.json"
    fi
    ;;
  promotion|rebuild)
    run asset workbench --session "$session" --select 1 --decision accept-proposal --reason approved --output "$root/decisions" >/dev/null
    run asset batch promotion-plan campaign.m029 --output "$root/promotion/plan" >/dev/null
    run asset batch promote campaign.m029 --target "$root/promotion/workspace" --output "$root/promotion" >/dev/null
    run asset approved validate "$root/promotion/workspace" --output "$root/promotion/validate" >/dev/null
    run asset rebuild --affected candidate.static --target "$root/promotion/workspace" --output "$root/promotion/rebuild" >/dev/null
    ;;
  review-pack)
    input_root="$tmp_root/review-input"
    run asset workbench --session "$session" --text 2 --output "$input_root/text-before" >/dev/null
    run asset workbench --session "$session" --submit --output "$input_root/submit" >/dev/null
    run asset workbench --session "$session" --text 12 --backspace --enter --output "$input_root/correction" >/dev/null
    run asset workbench --session "$session" --text 99 --submit --output "$input_root/invalid" >/dev/null
    run asset workbench --session "$session" --composition 'find chest' --submit --output "$input_root/rdp" >/dev/null
    run asset workbench --session "$session" --select 2 --output "$input_root/mouse" >/dev/null
    run asset workbench --session "$session" --focus lost --output "$input_root/focus" >/dev/null
    run asset workbench --session "$session" --select 1 --decision accept-proposal --reason approved --output "$root/decisions" >/dev/null
    run asset preview-host "$session" --output "$root/preview" >/dev/null
    run asset workbench --session "$session" --preview-restart --output "$root/recovery" >/dev/null
    run asset batch promote campaign.m029 --target "$root/promotion/workspace" --output "$root/promotion" >/dev/null
    pack="$root/workbench/asset-workbench-review-pack"
    mkdir -p "$pack"/{session,input,decisions,visual,audio,promotion,recovery,diagnostics}
    cp "$root/workbench/review-session.json" "$pack/session/review-session.json"
    commands="$tmp_root/review-workbench-input.jsonl"
    if [ -n "${DISPLAY:-}${WAYLAND_DISPLAY:-}" ]; then
      capture_workbench --capture "$pack/input/editable-command-field.png" --frames 3 >/dev/null
      capture_workbench --capture "$pack/input/text-choice-before-submit.png" --frames 3 --initial-text 2 --message 'Text is editable; no choice has been submitted.' >/dev/null
      capture_workbench --capture "$pack/input/text-choice-after-correction.png" --frames 3 --initial-text 1 --message 'Corrected text is ready for explicit submission.' >/dev/null
      capture_workbench --capture "$pack/input/invalid-input-recovery.png" --frames 3 --initial-text 99 --message 'Invalid input remains editable; no decision was recorded.' >/dev/null
      capture_workbench --capture "$pack/input/submit-button-use.png" --frames 3 --initial-text 2 --message 'Use Submit to turn visible text into a canonical action.' >/dev/null
      dotnet_cmd run --no-build --project src/Agentic2D.DebugClient.Raylib -- asset-preview --scene "$root/preview/preview-scene.json" --capture "$pack/visual/engine-preview.png" --frames 3 >/dev/null
    else
      printf '{"schema":"agentic2d.asset-workbench-graphical-evidence.v1","status":"skipped","reason":"no graphical session is available"}\n' > "$pack/input/graphical-evidence.json"
    fi
    cp "$input_root/mouse/input-result.json" "$pack/input/mouse-choice-equivalence.json"
    cp "$input_root/mouse/input-result.json" "$pack/input/mouse-only-workflow.json"
    cp "$input_root/submit/input-result.json" "$pack/input/text-stream-workflow.json"
    cp "$input_root/rdp/input-result.json" "$pack/input/rdp-composition-input.json"
    cp "$input_root/focus/input-result.json" "$pack/input/focus-recovery.json"
    cp "$input_root/text-before/input-state.json" "$pack/input/editable-command-field.json"
    cp "$input_root/correction/input-state.json" "$pack/input/text-choice-after-correction.json"
    cp "$input_root/invalid/input-state.json" "$pack/input/invalid-input-recovery.json"
    cp "$input_root/submit/input-result.json" "$pack/input/submit-button-use.json"
    cp "$home/sessions/$session/review-decisions.jsonl" "$pack/decisions/review-decisions.jsonl"
    cp "$root/preview/preview-scene.json" "$pack/visual/preview-scene.json"
    cp "$root/preview/preview-scene.json" "$pack/audio/preview-audio-state.json"
    cp "$root/promotion/promotion-result.json" "$pack/promotion/promotion-result.json"
    cp "$root/recovery/input-result.json" "$pack/recovery/preview-recovery.json"
    printf '{"schema":"agentic2d.m030-readiness.v1","approvedMedia":["candidate.static"],"futureBindings":["suggestion-only"],"fallbacks":[],"blockers":["consumer integration deferred to M030"],"exportInputs":["project-local approved assets"],"performanceConsiderations":["bounded processing"]}\n' > "$root/m030-readiness.json"
    printf '# M030 readiness\n\nPromotion output is provider-side only; consumer integration is deferred.\n' > "$root/m030-readiness.md"
    entries="$tmp_root/review-pack-entries.json"
    {
      printf '['
      first=1
      while IFS= read -r file; do
        relative="${file#"$pack/"}"
        sha="sha256:$(sha256sum "$file" | awk '{print $1}')"
        bytes="$(wc -c < "$file" | tr -d ' ')"
        if [ "$first" -eq 0 ]; then printf ','; fi
        first=0
        printf '{"path":"%s","bytes":%s,"sha256":"%s","required":true}' "$relative" "$bytes" "$sha"
      done < <(find "$pack" -type f ! -name manifest.json -print | sort)
      printf ']'
    } > "$entries"
    pack_fingerprint="sha256:$(sha256sum "$entries" | awk '{print $1}')"
    printf '{"schema":"agentic2d.asset-workbench-promotion-review-pack.v1","pathsSafe":true,"aliasesExcluded":true,"partialInputExcluded":true,"evidence":%s,"packFingerprint":"%s"}\n' "$(cat "$entries")" "$pack_fingerprint" > "$pack/manifest.json"
    printf '# Asset workbench review pack\n\nThis pack links command-derived input, decision, preview, recovery, and promotion evidence.\n' > "$pack/index.md"
    ;;
  integrated)
    run asset workbench --session "$session" --text 2 --submit --output "$root/integrated/input" >/dev/null
    run asset workbench --session "$session" --select 1 --decision accept-proposal --reason approved --output "$root/integrated/decision" >/dev/null
    run asset preview-host "$session" --output "$root/integrated/preview" >/dev/null
    run asset batch promote campaign.m029 --target "$root/integrated/workspace" --output "$root/integrated/promotion" >/dev/null
    test -f "$root/integrated/workspace/promotion-manifest.json"
    ;;
  *) fail "unknown asset workbench smoke mode: $mode" ;;
esac
