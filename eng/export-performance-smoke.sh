#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
./eng/export-equivalence-smoke.sh
out="$repo_root/artifacts/performance/M024"
mkdir -p "$out"
files="$(find "$repo_root/artifacts/smoke/m024-export/game" -type f | wc -l)"
bytes="$(du -sb "$repo_root/artifacts/smoke/m024-export/game" | awk '{print $1}')"
median() { sort -n | sed -n '3p'; }
measure_development() {
  dotnet_cmd build src/Agentic2D.Tools/Agentic2D.Tools.csproj --configuration Release --no-restore >/dev/null
  dotnet_cmd run --configuration Release --no-build --project src/Agentic2D.Tools -- scenario run runtime.smoke --output "$(mktemp -d /tmp/m024-dev-warmup.XXXXXX)" >/dev/null
  for _ in 1 2 3 4 5; do start="$(date +%s%N)"; dotnet_cmd run --configuration Release --no-build --project src/Agentic2D.Tools -- scenario run runtime.smoke --output "$(mktemp -d /tmp/m024-dev-run.XXXXXX)" >/dev/null; end="$(date +%s%N)"; echo $(((end-start)/1000000)); done | median
}
measure_export() {
  game="$repo_root/artifacts/smoke/m024-export/game/agentic2d-game"
  "$game" --headless --scenario runtime.smoke --output "$(mktemp -d /tmp/m024-export-warmup.XXXXXX)" >/dev/null
  for _ in 1 2 3 4 5; do start="$(date +%s%N)"; "$game" --headless --scenario runtime.smoke --output "$(mktemp -d /tmp/m024-export-run.XXXXXX)" >/dev/null; end="$(date +%s%N)"; echo $(((end-start)/1000000)); done | median
}
development_median="$(measure_development)"
export_median="$(measure_export)"
change="$(( (export_median-development_median)*100 / (development_median == 0 ? 1 : development_median) ))"
status="within-noise"
if [ "${change#-}" -ge 5 ]; then
  if [ "$change" -lt 0 ]; then status="improved"; else status="possible-regression"; fi
fi
printf '{"schema":"agentic2d.performance-report.v1","milestone":"M024","status":"%s","comparisonScope":"same-machine Release headless","startupAndIntegratedScenarioMedianMilliseconds":{"development":%s,"export":%s,"changePercent":%s},"allocation":"not comparable across independently hosted processes","deterministicWorkCounters":"compared by semantic-equivalence","exportFileCount":%s,"exportBytes":%s,"limitations":["Five measured iterations after one warm-up; timing is observational.","Changes at or above five percent are classified; changes at or above fifteen percent require follow-up."]}\n' "$status" "$development_median" "$export_median" "$change" "$files" "$bytes" > "$out/performance-report.json"
printf '# Performance report — M024\n\nStatus: `%s`\n\n| Representation | Median ms |\n|---|---:|\n| Development Release | %s |\n| Exported self-contained | %s |\n\nChange: %s%%. Export files: %s; bytes: %s. Allocations are not comparable across separately hosted processes; deterministic work is checked by equivalence.\n' "$status" "$development_median" "$export_median" "$change" "$files" "$bytes" > "$out/performance-report.md"
