# Milestone 024 — Standalone Linux Game Export and Development/Export Equivalence

## Goal

Add the smallest useful standalone game export for the current supported environment:

```text
validated game project
→ Release self-contained linux-x64 publish
→ bundled authored content and native dependencies
→ one runnable game directory
→ launch outside the source repository
→ prove development/export semantic equivalence
→ produce the M024 performance-impact report
```

The exported game must run without:

- the engine source repository;
- a workspace provider;
- `dotnet run`;
- repository `eng/` wrappers;
- a separately installed .NET runtime;
- network access.

This milestone validates one Linux development-machine target only. It does not create an SDK, multi-platform distribution system, installer, package manager, or mature release pipeline.

## Repository role and maturity assumptions

```text
role: capability-provider
profiles:
  - artifact-first-agentic-authoring
  - runtime-tool
  - game-simulation
maturity:
  - implementation-ready
  - artifact-first
guide-system metadata:
  - 0.5.1
execution mode:
  - ai-executed-broad
```

Provider responsibilities:

- define and implement the export contract;
- provide the runtime host used by exported games;
- resolve and validate bundled content and native dependencies;
- generate an inspectable export manifest;
- validate isolated launch, semantic equivalence, and performance impact.

Bounded consumer dogfood:

- export one existing reference game/workspace;
- launch the export from an isolated temporary directory;
- run one integrated deterministic reference journey;
- optionally launch the graphical game on the Linux development machine;
- do not add general consumer SDK capabilities.

## Execution mode

```text
ai-executed-broad
```

Implement through six sequential focus areas:

1. exported-game definition and Linux target policy;
2. minimal standalone game host;
3. deterministic export assembly and manifest;
4. isolated black-box launch;
5. development/export semantic equivalence;
6. export performance report, resumable validation, and optional human review.

## Validation execution mode

```text
focused export checks: direct
M024 aggregate suite: resumable-sharded
graphical launch review: recommended, non-blocking
```

Required suite interface:

```bash
./eng/m024-smoke.sh --list
./eng/m024-smoke.sh --plan-json
./eng/m024-smoke.sh --shard <id>
./eng/m024-smoke.sh --verify
./eng/m024-smoke.sh
```

Suggested shards:

```text
export-contracts
game-host
export-build
isolated-headless-launch
semantic-equivalence
performance-report
graphical-review
integrated
```

`graphical-review` may report an explicit supported skip only when no graphical session is available. On the current Linux development machine it should normally run.

Only `--verify` establishes aggregate M024 success.

## Scope

### Export target

1. Runtime identifier `linux-x64`.
2. Release configuration.
3. Self-contained publish.
4. Directory-based export.
5. Untrimmed.
6. Not single-file.
7. No ReadyToRun requirement.
8. One current Linux platform declaration.
9. One exported executable.
10. Bundled native raylib dependency.
11. Bundled authored game content.
12. Bundled required engine assemblies and runtime files.
13. Export manifest.
14. Deterministic file inventory and hashes.
15. Stable export fingerprint excluding machine-specific paths.

### Minimal game host

16. Exported-game startup manifest.
17. Project/content-root resolution relative to executable.
18. Initial game/scenario selection.
19. Headless validation/run mode.
20. Normal graphical mode.
21. Metrics mode integration.
22. Save-directory resolution.
23. Artifact/output-directory resolution for diagnostic runs.
24. Structured startup diagnostics.
25. Clean process exit.

### Export command

26. Project validation before export.
27. Content closure calculation.
28. Publish execution.
29. Native dependency resolution.
30. Content copy.
31. Startup-manifest generation.
32. File-manifest generation.
33. Hash validation.
34. Transactional output replacement.
35. Export inspection.
36. Export validation.

### Isolated execution

37. Copy export to a clean temporary location outside the repository.
38. Remove source/workspace assumptions.
39. Launch executable directly.
40. Run headless deterministic scenario.
41. Validate exit code.
42. Validate generated evidence.
43. Validate content and save paths.
44. Validate no network dependency.
45. Validate no absolute development-machine path dependency.

### Equivalence

46. Run the same reference scenario in development representation.
47. Run it in exported representation.
48. Use identical authored project/content fingerprint.
49. Use identical semantic input recording.
50. Use identical seed.
51. Compare authoritative final state.
52. Compare significant commands/events.
53. Compare save/load behavior where exercised.
54. Compare animation, sound, effects, UI, text, prompt, and render structural evidence.
55. Compare deterministic work counters where enabled.
56. Explain intentionally representation-specific artifact differences.

### Performance impact

57. Capture development Release representation.
58. Capture exported Release representation.
59. Same-machine comparison.
60. Startup duration.
61. Reference-scenario elapsed time.
62. Allocated-byte observation where comparable.
63. Deterministic work counters.
64. Export directory file count.
65. Export directory byte size.
66. M024 JSON and Markdown performance report.

## Non-goals

Do not implement:

### Platforms and publishing

- Windows;
- macOS;
- ARM targets;
- multiple Linux distributions as separately certified targets;
- AppImage;
- Flatpak;
- Snap;
- Debian/RPM packages;
- Steam integration;
- installers;
- launchers;
- desktop entries;
- icons/theme integration;
- code signing;
- checksums for public release distribution;
- release upload;
- GitHub Releases;
- CI publishing workflows;
- auto-update.

### Publish modes

- framework-dependent export;
- single-file publish;
- trimming;
- ReadyToRun as a requirement;
- NativeAOT;
- compression;
- encrypted content;
- binary content packs;
- delta export;
- patch generation;
- deterministic byte-for-byte `dotnet publish` output across machines.

### Product capability

- SDK;
- NuGet package consumption;
- source-provider replacement;
- workspace update/migration;
- game templates beyond the existing bounded reference fixture;
- editor;
- export UI;
- export profiles;
- plugin discovery;
- mods;
- scripting;
- downloadable content;
- arbitrary command-line customization;
- multiple game executables in one export;
- dedicated server;
- headless product as a separately distributed deliverable.

### Release quality

- broad compatibility matrix;
- long-running soak testing;
- formal security hardening;
- sandboxing;
- crash reporter;
- telemetry;
- localization packaging;
- license-compliance automation beyond an explicit current dependency inventory;
- professional performance budgets.

## Locked design decisions

### One export shape

The initial export is:

```text
target: linux-x64
configuration: Release
deployment: self-contained
layout: directory
trimmed: false
single-file: false
ready-to-run: optional false/default
network-required: false
```

Do not expose unused mode-selection complexity in M024.

### Game host versus development tools

The exported executable is a minimal game host.

It is not:

- `Agentic2D.Tools`;
- the engineering command host;
- a source-workspace wrapper;
- a development-only debug shell.

The host may reuse existing backend-neutral runtime, rendering, input, presentation, persistence, and raylib adapter components, but startup behavior and dependencies must be appropriate for a standalone game.

### Content closure

Export includes only the selected project's required authored runtime content and raw assets plus required runtime configuration.

Do not copy:

- source code;
- tests;
- `docs/`;
- `.git/`;
- `.guide-profile.json`;
- `.guide-sync/`;
- `.review/`;
- repository `eng/`;
- development artifacts;
- workspaces unrelated to the selected game;
- curation/research inputs not required at runtime;
- performance capture directories.

A conservative full project runtime-content copy is acceptable if precise reachability closure would add disproportionate complexity. The policy must be explicit and validated.

### Runtime paths

All bundled paths resolve relative to:

```text
export root
or
executable directory
```

No semantic dependency on creator-machine absolute paths is permitted.

Writable data must not be written into immutable bundled content by default.

Default writable roots:

```text
<current-user-data-root>/<game-id>/saves/
<current-user-data-root>/<game-id>/logs-or-artifacts/
```

For headless smoke validation, explicit temporary output paths may override these locations.

Use an existing repository/platform abstraction where available. Do not build a general cross-platform path layer for unsupported targets.

### Export identity

Semantic export fingerprint includes:

- export schema;
- game/project ID;
- game/project fingerprint;
- engine revision or capability fingerprint;
- target RID;
- publish mode;
- startup manifest;
- runtime content inventory and hashes;
- native dependency inventory and hashes.

Exclude:

- output root absolute path;
- timestamps;
- process IDs;
- temporary directories;
- machine-local publish-cache paths.

The complete export directory need not be byte-identical across separate machines.

### Graphical dependency

The normal exported game includes the current raylib native dependency for Linux.

The isolated headless smoke must remain runnable in an ordinary headless environment without requiring a display server.

The implementation may use:

- a headless flag in the game host; or
- a startup mode that skips graphical initialization while exercising the same runtime/project content.

Do not create a second independently implemented headless runtime.

## Focus Area 1 — Export definition and Linux policy

### Export content roots

Recommended output:

```text
artifacts/exports/<game-id>/linux-x64/
```

Conceptual export layout:

```text
<game-id>/
├─ <game-executable>
├─ agentic2d.export.json
├─ runtime/
├─ content/
├─ native/
├─ licenses/
└─ diagnostics/
```

Exact publish-root layout may remain flatter when dictated by `.NET` publish conventions. The manifest must identify executable, content root, native dependencies, and startup configuration.

### Startup manifest

Required fields:

- schema;
- export ID;
- game/project ID;
- display name;
- target RID;
- executable relative path;
- content root;
- startup scenario/world ID;
- default metrics mode;
- default graphical/headless behavior;
- save-root policy;
- artifact-root policy;
- engine revision/capability fingerprint;
- project/content fingerprint;
- file-manifest reference;
- export fingerprint.

Do not add a general launcher configuration language.

## Focus Area 2 — Minimal standalone game host

Recommended project:

```text
src/Agentic2D.GameHost
```

The host must:

1. locate the export root;
2. parse and validate `agentic2d.export.json`;
3. resolve bundled content;
4. validate expected project/content fingerprints;
5. initialize existing game/runtime services;
6. run graphical mode by default;
7. support explicit headless smoke/validation mode;
8. support existing semantic input replay where supplied by internal smoke validation;
9. expose metrics summary using M023;
10. resolve saves and diagnostics to writable paths;
11. shut down cleanly.

The host is intentionally small. It should delegate engine behavior to existing libraries.

Do not copy product CLI command routing into the host.

### Minimal command-line surface

Allowed initial options:

```text
--headless
--scenario <id>
--recording <path>
--ticks <count-or-final>
--metrics off|summary|per-tick
--output <path>
--help
--version
```

Normal users may launch with no arguments.

Do not expose export/build commands from the game host.

## Focus Area 3 — Export assembly and manifest

### Product CLI commands

Add command shapes consistent with current product CLI:

```bash
agentic2d project export <project-or-workspace> \
  --target linux-x64 \
  --output <directory>

agentic2d export inspect <export-directory> \
  --output <directory>

agentic2d export validate <export-directory> \
  --output <directory>
```

Because only one target exists, `--target` may be optional with `linux-x64` as the sole accepted/default value. Do not create an extensible target registry unless necessary for clean code.

### Export sequence

```text
resolve project/workspace
→ validate game project
→ validate runtime content
→ determine runtime-content copy set
→ publish GameHost Release self-contained linux-x64
→ copy content/native dependencies
→ generate startup manifest
→ generate file inventory/hashes
→ validate assembled export
→ atomically replace requested output
```

Do not leave a valid-looking partial export at the final path.

### File manifest

Each declared file records:

- relative path;
- category;
- byte length;
- cryptographic hash;
- executable bit expectation where relevant.

Categories:

```text
host
managed-runtime
native-runtime
game-content
raw-asset
license
configuration
```

Unknown undeclared files cause validation failure, except explicitly allowed transient files created after launch under designated writable directories outside bundled content.

## Focus Area 4 — Isolated black-box launch

### Required scenario

Use the current integrated player-facing persistent-world reference scenario or its accepted successor.

Export smoke flow:

```text
build export
→ copy export to isolated temporary directory outside repository
→ verify repository path is unavailable/unreferenced
→ execute game binary directly with --headless
→ run deterministic recording to completion
→ emit run artifacts
→ validate exit status and manifest
```

The test must not invoke:

- `dotnet run`;
- `Agentic2D.Tools`;
- repository `eng/` wrappers from inside the export;
- engine source;
- workspace source acquisition;
- network access.

Engineering wrappers may orchestrate the external process from the repository test harness.

### Negative checks

Validate rejection for:

- missing startup manifest;
- modified content file;
- missing native or managed required file;
- absolute content path injection;
- incompatible project/content fingerprint;
- unsupported target value;
- non-empty output replacement without explicit safe transactional behavior.

## Focus Area 5 — Development/export equivalence

### Representations

Development representation:

```text
Release build
current product/project run path
same project/content
same scenario
same recording
same seed
metrics summary enabled
```

Exported representation:

```text
Release self-contained linux-x64 game host
same project/content fingerprint
same scenario
same recording
same seed
metrics summary enabled
```

### Strict comparisons

Compare:

- project/content identity;
- accepted semantic inputs;
- accepted/rejected commands;
- significant domain events;
- final authoritative entity/component state;
- health/lifecycle/inventory;
- flags/switches/doors;
- save/load result when included;
- deterministic work counters;
- animation selections;
- sound cue commands;
- effect/particle structural records;
- camera states excluding observational timing;
- UI bindings/layout;
- text commands;
- interaction prompts;
- final structural render fingerprint.

### Allowed differences

Representation-specific differences may include:

- executable/process identity;
- runtime file paths;
- writable output paths;
- wall-clock timings;
- allocation observations;
- host startup diagnostics;
- physical audio/graphics adapter details;
- export manifest and file inventory.

All allowed differences must be documented, not silently ignored through broad comparison exclusions.

## Focus Area 6 — Performance impact and completion

### Required M024 performance report

Use the M023 workflow.

Required comparison:

```text
before/development representation
versus
after/exported representation
```

This comparison is descriptive rather than an optimization pass. The exported host may be faster or slower.

Required metrics:

- process startup to validated host initialization;
- integrated reference-scenario elapsed median;
- allocations when captured by a comparable method;
- deterministic work counters;
- export total bytes;
- export file count.

Required outputs:

```text
artifacts/performance/M024/performance-report.json
artifacts/performance/M024/performance-report.md
```

Expected overall statuses:

```text
within-noise
improved
expected-cost
possible-regression
```

`not-applicable` is not allowed because export performance is directly relevant.

A material difference must be explained. M024 does not require optimization unless the difference indicates an architectural defect such as repeated content loading, duplicated runtime work, or accidental diagnostic artifact writing inside the measured region.

### Optional graphical review

On the Linux development machine:

```text
launch exported executable directly
→ render the integrated reference game
→ verify input, camera, UI, text, effects, animation, and sound
→ confirm save location is writable
→ exit cleanly
```

Review is recommended and non-blocking unless automated validation cannot establish native dependency inclusion or launchability.

## Required artifacts

### Export

```text
export-result.json
export-manifest.json
export-files.json
export-validation.json
export-diagnostics.json
```

The assembled export contains `agentic2d.export.json`. Engineering artifacts may normalize or copy its semantic information into the listed artifact family.

### Isolated launch

```text
isolated-launch-result.json
isolated-launch-process.json
isolated-launch-run-manifest.json
isolated-launch-diagnostics.json
```

### Equivalence

```text
development-export-equivalence.json
development-export-differences.json
development-export-diagnostics.json
```

### Performance

```text
performance-report.json
performance-report.md
```

Do not emit empty placeholders for unexercised capabilities.

## Required engineering wrappers

```bash
./eng/export-linux-smoke.sh
./eng/export-isolated-launch-smoke.sh
./eng/export-equivalence-smoke.sh
./eng/export-performance-smoke.sh
./eng/export-graphical-smoke.sh
./eng/m024-smoke.sh
```

All required semantic validation is headless except the explicit graphical smoke.

## Required authority documents

Read only:

```text
README.md
AGENTS.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/SCENARIOS.md
docs/CONTENT.md
docs/ARTIFACTS.md
docs/HUMAN-REVIEW.md
docs/ENGINEERING.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/validation-tiers.md
docs/engineering/constrained-validation-execution.md
docs/engineering/human-review-workflow.md
docs/engineering/performance-measurement.md
docs/engineering/future-dotnet-solution.md
docs/specs/runtime-principles.md
docs/specs/minimal-deterministic-runtime.md
docs/specs/product-cli-contract.md
docs/specs/scenario-runner-contract.md
docs/specs/content-validation-contract.md
docs/specs/runtime-inspection-contract.md
docs/specs/game-project-manifest-contract.md
docs/specs/game-workspace-manifest-contract.md
docs/specs/unified-agent-execution-workflow-contract.md
docs/specs/visual-definition-contract.md
docs/specs/render-projection-contract.md
docs/specs/raylib-debug-client-contract.md
docs/specs/semantic-input-recording-and-replay-contract.md
docs/specs/save-load-and-resume-contract.md
docs/specs/lightweight-runtime-metrics-contract.md
docs/specs/comparative-performance-check-contract.md
docs/specs/standalone-linux-game-export-contract.md
docs/specs/exported-game-host-contract.md
docs/artifacts/unified-run-artifact-contract.md
docs/artifacts/milestone-performance-report-artifact-contract.md
docs/artifacts/standalone-game-export-artifact-contract.md
docs/decisions/ADR-0018-rendering-is-read-only-and-raylib-is-an-isolated-adapter.md
docs/decisions/ADR-0024-saves-contain-canonical-authoritative-state.md
docs/decisions/ADR-0030-runtime-metrics-are-diagnostic-and-non-authoritative.md
docs/decisions/ADR-0031-performance-comparison-is-local-and-milestone-scoped.md
docs/decisions/ADR-0032-first-export-is-self-contained-linux-x64-directory.md
docs/decisions/ADR-0033-exported-games-use-a-minimal-dedicated-host.md
docs/milestones/MILESTONE-023-lightweight-runtime-metrics-comparative-performance-checks-and-milestone-performance-reporting.md
docs/milestones/MILESTONE-024-standalone-linux-game-export-and-development-export-equivalence.md
```

Ordinary implementation agents do not read `.guide-profile.json`, `.guide-sync/`, external guides, or prompt templates.

## Files or areas likely affected

Recommended new project:

```text
src/Agentic2D.GameHost
```

Likely affected:

```text
dotnet-ai-first-2d-game-engine.slnx
src/Agentic2D.Contracts
src/Agentic2D.Engine
src/Agentic2D.ScenarioRunner
src/Agentic2D.Validation
src/Agentic2D.Rendering
src/Agentic2D.Input
src/Agentic2D.Animation
src/Agentic2D.Sound
src/Agentic2D.Gameplay
src/Agentic2D.Presentation
src/Agentic2D.UI
src/Agentic2D.Persistence
src/Agentic2D.Workspaces
src/Agentic2D.Tools
src/Agentic2D.DebugClient.Raylib
src/Agentic2D.Engineering
tests/unit/Agentic2D.Tests.Unit
eng/
game/
```

Generated outputs:

```text
artifacts/exports/
artifacts/export-inspect/
artifacts/export-validation/
artifacts/performance/M024/
artifacts/validation/m024-smoke/
```

## Validation tiers and concrete commands

### Tier 1 — Focused implementation

```bash
./eng/build.sh
./eng/test-filter.sh Export
./eng/test-filter.sh GameHost
./eng/export-linux-smoke.sh
```

Use the closest current focused-test command if exact syntax differs.

### Tier 2 — Isolated product checks

```bash
./eng/export-isolated-launch-smoke.sh
./eng/export-equivalence-smoke.sh
./eng/export-performance-smoke.sh
```

### Expected product commands

```bash
dotnet run --project src/Agentic2D.Tools -- \
  project export . \
  --target linux-x64 \
  --output artifacts/exports/reference-game/linux-x64

dotnet run --project src/Agentic2D.Tools -- \
  export inspect artifacts/exports/reference-game/linux-x64 \
  --output artifacts/export-inspect/reference-game

dotnet run --project src/Agentic2D.Tools -- \
  export validate artifacts/exports/reference-game/linux-x64 \
  --output artifacts/export-validation/reference-game

artifacts/exports/reference-game/linux-x64/<game-executable> \
  --headless \
  --scenario presentation.persistent-world-player-facing-smoke \
  --recording <bundled-or-test-recording> \
  --metrics summary \
  --output /tmp/agentic2d-m024-export-run
```

Use exact accepted scenario and CLI names from current project truth.

### Tier 2 — Resumable aggregate

```bash
./eng/m024-smoke.sh --plan-json
./eng/m024-smoke.sh --shard export-contracts
./eng/m024-smoke.sh --shard game-host
./eng/m024-smoke.sh --shard export-build
./eng/m024-smoke.sh --shard isolated-headless-launch
./eng/m024-smoke.sh --shard semantic-equivalence
./eng/m024-smoke.sh --shard performance-report
./eng/m024-smoke.sh --shard graphical-review
./eng/m024-smoke.sh --shard integrated
./eng/m024-smoke.sh --verify
```

### Regression validation

Run current direct gates:

```bash
./eng/check.sh
./eng/cli-smoke.sh
./eng/product-validate.sh
```

Run M019–M023 through current plan/shard/verify interfaces. Execute each required shard separately in constrained environments.

## Acceptance criteria

### Export contract

1. The only supported target is `linux-x64`.
2. Export is Release, self-contained, directory-based, untrimmed, and not single-file.
3. One exported executable exists and is marked executable.
4. Required managed runtime files are present.
5. Required Linux native raylib dependency is present.
6. Required authored runtime content and assets are present.
7. Source, tests, docs, guide metadata, review state, and engineering scripts are absent.
8. Startup manifest validates.
9. File inventory and hashes validate.
10. Export fingerprint excludes machine-specific output paths.
11. Partial failure does not leave a valid final export.

### Game host

12. Host resolves content relative to export root.
13. Host launches with no arguments in graphical mode.
14. Host supports explicit headless mode.
15. Host supports existing semantic replay needed for validation.
16. Host can emit metrics summary.
17. Host uses writable save/diagnostic paths.
18. Host does not depend on product CLI or engineering host at runtime.
19. Host exits cleanly.
20. Missing or invalid startup configuration fails clearly.

### Isolated launch

21. Export runs after copying outside the repository.
22. Direct executable launch works without `dotnet`.
23. No network access is required.
24. No source/workspace acquisition is required.
25. No absolute creator-machine path is required.
26. Headless scenario completes.
27. Required run evidence is produced.
28. Tampered or missing required files are rejected.

### Equivalence

29. Development and export use identical project/content identity.
30. Inputs, seed, and scenario match.
31. Significant commands and events match.
32. Final authoritative state matches.
33. Persistence outcomes match where exercised.
34. Deterministic work counters match.
35. Animation, sound, effect, camera, UI, text, prompt, and render structural evidence match.
36. Allowed representation-specific differences are explicitly enumerated.
37. No broad comparison exclusion masks semantic divergence.

### Performance impact

38. M024 JSON and Markdown reports are produced.
39. Development/export startup and integrated-run measurements are present.
40. Export file count and total bytes are present.
41. Same-machine limitations are explicit.
42. Material differences are explained.
43. The report does not claim a cross-machine budget.
44. No unnecessary optimization scope is introduced.

### Constrained validation

45. M024 exposes list, plan, shard, verify, and no-argument modes.
46. Receipts are atomic, current, and fingerprinted.
47. Failed/interrupted shards leave no valid pass receipt.
48. Graphical review skip is explicit and supported only when the environment lacks a graphical session.
49. Only successful `--verify` establishes M024 completion.

### Scope protection

50. No SDK, Windows/macOS, installer, package format, single-file, trimming, NativeAOT, public release workflow, or auto-update is implemented.
51. Existing M019–M023 regression gates remain passing.

## Direct documentation impact

Update only where active truth changes:

```text
README.md
AGENTS.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/SCENARIOS.md
docs/CONTENT.md
docs/ARTIFACTS.md
docs/HUMAN-REVIEW.md
docs/ENGINEERING.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/validation-tiers.md
docs/engineering/constrained-validation-execution.md
docs/engineering/performance-measurement.md
docs/engineering/future-dotnet-solution.md
docs/specs/product-cli-contract.md
docs/specs/game-project-manifest-contract.md
docs/specs/unified-agent-execution-workflow-contract.md
docs/specs/raylib-debug-client-contract.md
docs/artifacts/unified-run-artifact-contract.md
```

Do not perform unrelated synchronization.

## Deferred documentation synchronization hints

The package adds:

```text
.guide-sync/pending/2026-07-17-m024-export-index-roadmap-and-crosslink-sync.md
.guide-sync/pending/2026-07-17-m024-export-platform-and-distribution-followup.md
```

Ordinary implementation agents do not read these files.

## Human-review requirements

```text
applicability: recommended
blocking: no
classes:
  - artifact quality
  - UX
  - release
```

Recommended subjects:

1. export directory is understandable and minimal;
2. direct executable launch feels like a game rather than a development tool;
3. graphical output matches the development representation;
4. input, sound, UI, text, effects, and save path behave correctly;
5. diagnostics are useful when bundled files are damaged;
6. export size and startup observations are reasonable for the current unoptimized form.

Recommended evidence:

- export file inventory;
- export inspection and validation reports;
- isolated headless launch evidence;
- development/export equivalence report;
- M024 performance report;
- optional graphical screenshot or short review notes;
- M024 verifier result.

No `.review/pending/` request is created because automated semantic acceptance is sufficient and graphical review is recommended rather than blocking.

Re-review triggers:

- target RID or publish mode changes;
- executable host changes materially;
- native dependency packaging changes;
- content-root or writable-data policy changes;
- equivalence comparison changes;
- trimming/single-file or another platform is introduced later.

## Performance impact validation

Affected workloads:

```text
performance.persistent-world-reference
performance.presentation-reference
```

Additional M024 measurements:

```text
export startup
export integrated reference run
export directory bytes
export file count
```

Required report:

```text
artifacts/performance/M024/performance-report.json
artifacts/performance/M024/performance-report.md
```

Completion policy:

- explain changes of 5% or more;
- investigate or explicitly classify changes of 15% or more;
- distinguish representation startup cost from engine workload cost;
- compare deterministic work counters;
- do not optimize unless evidence identifies a clear defect or the milestone acceptance criteria fail.

## Constrained-runtime handling

In constrained environments:

1. run `./eng/m024-smoke.sh --plan-json`;
2. execute each required shard in a separate invocation;
3. stop on real failure;
4. do not use backgrounding, `nohup`, `setsid`, timeout inflation, or partial-log inference;
5. run `./eng/m024-smoke.sh --verify`;
6. report the verifier result.

Export build and isolated launch may be separate shards because self-contained publish and native file assembly can exceed a short invocation.

## Out-of-scope guide migration work

No guide migration is included.

Do not:

- modify `.guide-profile.json`;
- copy or reference external guide documents as implementation authority;
- introduce TBPs or issue templates;
- require ordinary agents to read `.guide-sync/`;
- add generic distribution guidance to the target repository beyond current Linux project truth.
