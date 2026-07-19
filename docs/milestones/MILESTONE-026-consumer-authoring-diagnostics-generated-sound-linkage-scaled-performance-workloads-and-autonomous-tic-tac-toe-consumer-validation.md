# Milestone 026 — Consumer Authoring Diagnostics, Generated Sound Linkage, Scaled Performance Workloads, and Autonomous Tic-Tac-Toe Consumer Validation

## Goal

Use a second, structurally different consumer game to improve concrete authoring diagnostics and determine which consumer boundaries deserve later stabilization.

```text
geometry definitions → preview/diagnostics
offline synthesis → WAV/provenance → explicit sound-definition linkage
small fixed workloads → counters + informational timing
scaled real workloads → ordinary elapsed comparison
autonomous tic-tac-toe → deterministic AI, human takeover, presentation, save/resume, export
Signal Passage + tic-tac-toe → consumer-boundary decision report
```

Primary acceptance question:

> Can a second consumer be authored, validated, reviewed, saved, replayed, and exported without broad extension infrastructure, while repeated authoring friction is diagnosed precisely enough to plan M027?

## Repository role and maturity assumptions

```text
role: capability-provider
profiles: artifact-first-agentic-authoring, runtime-tool, game-simulation
maturity: implementation-ready, artifact-first
execution mode: ai-executed-human-reviewed
```

This is bounded mixed provider/consumer dogfood. Provider work is limited to geometry diagnostics, generated-sound linkage, scaled workload policy, and evidence infrastructure. Tic-tac-toe rules, AI policy, participant ownership, presentation choices, and scenarios are consumer truth.

## Execution mode

```text
ai-executed-human-reviewed
```

Implement five coherent focus areas:

1. geometry authoring diagnostics;
2. explicit generated-sound linkage;
3. scaled performance workloads and timing-authority policy;
4. autonomous tic-tac-toe consumer vertical slice;
5. cross-consumer decision report, review, and aggregate validation.

## Scope

### Geometry diagnostics

- project/definition inspection and preview commands;
- normalized geometry inventory, bounds, ordering, fill/outline/opacity, and provenance;
- headless structural preview;
- graphical capture in a documented graphics-capable environment;
- all-supported-shape smoke;
- structural-versus-graphical projection comparison;
- deterministic bounded contrast warnings against a declared preview background;
- precise field-level invalid-geometry diagnostics.

### Generated sound linkage

- explicit stable linkage between synthesis definition, generated WAV/provenance, and ordinary sound definition;
- ordinary sound definition remains runtime authority;
- synthesis definition remains generation source truth;
- missing output, missing definition, stale provenance, hash drift, orphan output, duplicate/incompatible linkage, and wrong mapping validation;
- structured JSON and Markdown linkage report;
- migrate Signal Passage and tic-tac-toe generated cues.

### Performance

- retain existing small fixed workloads for correctness, work counters, allocations, and catastrophic sentinel value;
- elapsed timing below a 10 ms reference median is `not-timing-authoritative`;
- ordinary percentage thresholds do not apply below the floor;
- optional catastrophic advisory requires both after median >= 10 ms and after median >= 4 × before median;
- add scaled runtime, entities, persistent-world, and presentation workloads using real engine operations;
- each scaled workload targets at least a 10 ms median on the current development machine;
- retain one warm-up, five measured iterations, median, same-machine, close-in-time comparison;
- no statistical-significance framework.

### Autonomous tic-tac-toe

- committed workspace under `consumers/autonomous-tic-tac-toe/`;
- standard 3 × 3 board;
- AI X versus AI O by default;
- human can take over or release either participant;
- AI enumerates free cells in stable order and selects through deterministic engine randomness;
- fake thinking delay uses 30–90 simulation ticks, never wall-clock delay;
- win, draw, score, round reset, invalid selection;
- geometric board/highlights/winner line;
- mark fade/scale animation using existing animation capability;
- thinking animation;
- deterministic bounded round-start particle effect using existing particle capability;
- synthesized cues with explicit linkage;
- save/resume during AI thinking, including remaining ticks and no transient replay;
- external-style workspace isolation, Linux export, and development/export equivalence.

### Cross-consumer assessment

Compare Signal Passage and tic-tac-toe for deterministic randomness, delayed behavior, UI/HUD, prompts, persistence, scenario assertions, behavior restrictions, registration/serialization boilerplate, export, geometry, and sound linkage. Decide `formalize-next`, `retain-existing`, `improve-diagnostics-only`, `observe-more`, `consumer-specific`, or `reject` for every required candidate.

## Non-goals

Do not implement:

- SDK, NuGet distribution, stable plugin API, mods, dynamic discovery;
- general board-game, objective, AI, or turn-scheduler frameworks;
- registration source generation;
- broad persistence, UI, prompt, or scenario contracts without two-consumer evidence;
- minimax, difficulty levels, multiplayer, arbitrary boards, tournaments;
- new animation, particle, rendering, or runtime synthesis systems;
- BenchmarkDotNet migration, significance testing, confidence intervals, benchmark service, cross-machine baselines, or soak tests;
- new export targets, installers, release upload, permanent external consumer repository;
- TBPs, issue templates, copied guides, or guide migration.

## Focus area 1 — Geometry authoring diagnostics

Required product command shape, adapted to current CLI conventions:

```bash
agentic2d geometry inspect <project-or-definition> --output <directory>
agentic2d geometry preview <project-or-definition> --output <directory>
```

Required artifacts:

```text
geometry-inspection.json
geometry-preview.json
geometry-diagnostics.json
geometry-projection-comparison.json
```

Headless evidence is mandatory. Graphical capture is adapter-owned. Contrast checks are simple warnings/errors according to explicit project policy and must not claim accessibility or aesthetic certification. No automatic repair is permitted.

## Focus area 2 — Explicit generated-sound linkage

Conceptual relationship:

```text
synthesis definition → WAV + provenance → explicit linkage → ordinary sound definition → runtime cue
```

Required command shape:

```bash
agentic2d sound linkage inspect <project> --output <directory>
agentic2d sound linkage validate <project> --output <directory>
```

Required artifacts:

```text
generated-sound-linkage-report.json
generated-sound-linkage-report.md
```

Filename convention alone is insufficient. Runtime cue selection and playback remain unchanged.

## Focus area 3 — Scaled performance workloads

Small workloads record `timingAuthority`, `timingAuthorityReason`, and reference median. Below 10 ms they report `not-timing-authoritative`; counters and allocations remain useful.

Add:

```text
performance.runtime-reference-scaled
performance.entities-reference-scaled
performance.persistent-world-reference-scaled
performance.presentation-reference-scaled
```

Use fixed seed and real engine work. No busy loops. If a scaled workload remains below 10 ms, increase its bounded real workload rather than assigning misleading percentage authority.

Produce:

```text
artifacts/performance/M026/performance-report.json
artifacts/performance/M026/performance-report.md
```

## Focus area 4 — Autonomous tic-tac-toe consumer

Create:

```text
consumers/autonomous-tic-tac-toe/
├─ agentic2d.workspace.json
├─ agentic2d.project.json
├─ game-src/AutonomousTicTacToe.Game/
├─ game-content/
├─ game-scenarios/
└─ artifacts/                 # generated/ignored
```

Consumer state:

```text
cells[9]
currentMark
xController
oController
phase
thinkingTicksRemaining
winner
roundNumber
scoreX
scoreO
drawCount
```

Controllers: `ai`, `human`.

Phases: `round-starting`, `thinking`, `awaiting-human-input`, `placing-mark`, `round-complete`, `resetting`.

Semantic actions:

```text
participant.take-x
participant.take-o
participant.release-x
participant.release-o
board.select-cell
round.restart
```

Persist board, turn, controllers, phase, remaining thinking ticks, winner, round, scores, and draw count. Do not persist sounds, active particles, prompt instances, or completed animation playback.

Required cues:

```text
round-start
thinking-start
mark-x
mark-o
invalid-selection
win
draw
human-takeover
```

Required scenarios:

```text
tic-tac-toe.workspace-smoke
tic-tac-toe.ai-vs-ai-smoke
tic-tac-toe.deterministic-random-choice
tic-tac-toe.human-takes-x
tic-tac-toe.human-takes-o
tic-tac-toe.release-control
tic-tac-toe.invalid-cell-rejected
tic-tac-toe.x-wins
tic-tac-toe.o-wins
tic-tac-toe.draw
tic-tac-toe.round-reset
tic-tac-toe.save-during-thinking
tic-tac-toe.presentation-smoke
tic-tac-toe.generated-sound-linkage
tic-tac-toe.external-workspace-smoke
tic-tac-toe.linux-export-smoke
```

At least one scenario proves identical seed and semantic inputs produce identical AI choices and final state.

## Focus area 5 — Consumer-boundary decision report

Produce:

```text
artifacts/consumer-boundaries/M026/consumer-boundary-decision-report.json
artifacts/consumer-boundaries/M026/consumer-boundary-decision-report.md
```

Required candidates:

- deterministic randomness access;
- delayed behavior;
- UI/HUD bindings;
- prompts;
- persistence participation;
- scenario assertions;
- behavior guardrails;
- registration generation;
- generated-sound linkage;
- geometry diagnostics;
- external consumer repository split.

Broad contracts belong to a later milestone unless both consumers prove the need.

## Implementation constraints

- consumer code remains separate from provider source;
- engine-provider workspace areas remain read-only by default;
- tic-tac-toe rules remain consumer truth;
- AI uses deterministic randomness and stable free-cell ordering;
- thinking uses simulation ticks, not `Task.Delay`, sleep, wall-clock timers, or `Random.Shared`;
- rendering remains read-only and geometry presentation-only;
- sound synthesis remains offline and sound definitions runtime authority;
- reuse existing animation and particle capabilities;
- small timing percentages are not ordinary performance authority;
- scaled workloads use real engine work;
- no broad extension contract without two-consumer evidence;
- human review cannot replace failed semantic validation;
- ordinary implementation agents do not read `.guide-profile.json`, `.guide-sync/`, external guides, copied research guides, prompt templates, TBPs, or issue templates.

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
docs/specs/runtime-principles.md
docs/specs/product-cli-contract.md
docs/specs/scenario-runner-contract.md
docs/specs/content-validation-contract.md
docs/specs/game-project-manifest-contract.md
docs/specs/game-workspace-manifest-contract.md
docs/specs/unified-agent-execution-workflow-contract.md
docs/specs/deterministic-behavior-runtime-contract.md
docs/specs/semantic-input-action-contract.md
docs/specs/input-mapping-and-replay-contract.md
docs/specs/visual-definition-contract.md
docs/specs/geometric-visual-definition-and-projection-contract.md
docs/specs/geometry-authoring-diagnostics-contract.md
docs/specs/render-projection-contract.md
docs/specs/animation-definition-and-sampling-contract.md
docs/specs/effect-particle-and-camera-command-contract.md
docs/specs/sound-definition-and-command-contract.md
docs/specs/deterministic-offline-sound-synthesis-contract.md
docs/specs/generated-sound-linkage-contract.md
docs/specs/gameplay-presentation-event-contract.md
docs/specs/canonical-save-snapshot-contract.md
docs/specs/persistence-contributor-contract.md
docs/specs/save-load-and-resume-contract.md
docs/specs/declarative-ui-and-binding-contract.md
docs/specs/semantic-interaction-surface-contract.md
docs/specs/lightweight-runtime-metrics-contract.md
docs/specs/comparative-performance-check-contract.md
docs/specs/scaled-performance-workload-contract.md
docs/specs/standalone-linux-game-export-contract.md
docs/specs/autonomous-tic-tac-toe-consumer-contract.md
docs/artifacts/unified-run-artifact-contract.md
docs/artifacts/milestone-performance-report-artifact-contract.md
docs/artifacts/consumer-boundary-decision-report-artifact-contract.md
docs/decisions/ADR-0036-second-consumer-is-autonomous-tic-tac-toe.md
docs/decisions/ADR-0037-small-workload-timing-is-informational-and-scaled-workloads-carry-comparison-authority.md
docs/decisions/ADR-0038-generated-sound-linkage-is-explicit-and-sound-definitions-remain-runtime-authority.md
docs/milestones/MILESTONE-023-lightweight-runtime-metrics-comparative-performance-checks-and-milestone-performance-reporting.md
docs/milestones/MILESTONE-024-standalone-linux-game-export-and-development-export-equivalence.md
docs/milestones/MILESTONE-025-reference-game-workspace-procedural-presentation-synthesized-sound-and-extension-discovery.md
docs/milestones/MILESTONE-026-consumer-authoring-diagnostics-generated-sound-linkage-scaled-performance-workloads-and-autonomous-tic-tac-toe-consumer-validation.md
```

Read `.review/` only for the required M026 review workflow. M025 handoff artifacts are planning evidence, not ordinary implementation authority.

## Files or areas likely affected

```text
consumers/autonomous-tic-tac-toe/
consumers/signal-passage/game-content/sounds/
consumers/signal-passage/game-content/sound-synthesis/
src/Agentic2D.Contracts
src/Agentic2D.Rendering
src/Agentic2D.Validation
src/Agentic2D.Sound
src/Agentic2D.Tools
src/Agentic2D.Engine
src/Agentic2D.Behaviors
src/Agentic2D.Input
src/Agentic2D.Animation
src/Agentic2D.Presentation
src/Agentic2D.Persistence
src/Agentic2D.ScenarioRunner
src/Agentic2D.Workspaces
src/Agentic2D.GameHost
src/Agentic2D.DebugClient.Raylib
src/Agentic2D.Engineering
tests/unit/Agentic2D.Tests.Unit
```

Use actual current project ownership. Do not create empty projects.

Generated evidence:

```text
artifacts/geometry/
artifacts/sound-linkage/
artifacts/performance/M026/
artifacts/consumer-boundaries/M026/
artifacts/validation/m026-smoke/
artifacts/review/M026/
```

## Validation tiers and concrete commands

Tier 1 provider:

```bash
./eng/build.sh
./eng/test-filter.sh Geometry
./eng/test-filter.sh SoundLinkage
./eng/content-validate.sh visuals
./eng/content-validate.sh sounds
./eng/geometry-diagnostics-smoke.sh
./eng/generated-sound-linkage-smoke.sh
./eng/perf-smoke.sh
./eng/scaled-performance-smoke.sh
./eng/m026-performance-report.sh
```

Tier 1 consumer:

```bash
./eng/tic-tac-toe-validate.sh
./eng/tic-tac-toe-smoke.sh
./eng/tic-tac-toe-isolation.sh
./eng/tic-tac-toe-export.sh
```

Tier 2 integrated:

```bash
./eng/check.sh
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/signal-passage-smoke.sh
```

Human review:

```bash
./eng/tic-tac-toe-review.sh
./eng/review-list.sh
./eng/review-check.sh
```

Resumable aggregate:

```bash
./eng/m026-smoke.sh --plan-json
./eng/m026-smoke.sh --shard geometry-diagnostics
./eng/m026-smoke.sh --shard sound-linkage
./eng/m026-smoke.sh --shard scaled-performance
./eng/m026-smoke.sh --shard tic-tac-toe-core
./eng/m026-smoke.sh --shard tic-tac-toe-presentation
./eng/m026-smoke.sh --shard tic-tac-toe-persistence
./eng/m026-smoke.sh --shard workspace-isolation
./eng/m026-smoke.sh --shard linux-export
./eng/m026-smoke.sh --shard consumer-boundary-report
./eng/m026-smoke.sh --shard human-review
./eng/m026-smoke.sh --shard integrated
./eng/m026-smoke.sh --verify
```

Run M019–M025 through each suite's current plan/shard/verify interface. Do not infer aggregate success from partial output.

## Validation execution mode

```text
focused checks: direct
M026 aggregate: resumable-sharded
geometry/game usability: human-review, required and blocking
graphics-dependent capture: documented graphics-capable environment
```

Suite: `./eng/m026-smoke.sh`

Plan: `./eng/m026-smoke.sh --plan-json`

Receipt root: `artifacts/validation/m026-smoke/`

Fingerprint geometry diagnostics, sound linkage/provenance, workload definitions/policy, both consumer inputs, boundary report, review record, and export/equivalence evidence. Exclude observational timing values, graphics/audio device identity, screenshot timestamps, and absolute temporary paths.

Only `./eng/m026-smoke.sh --verify` establishes aggregate completion.

## Acceptance criteria

### Geometry

1. Inspection emits normalized structural artifacts.
2. Every supported shape is covered.
3. Structural and graphical projection evidence can be related.
4. Contrast warnings are deterministic, bounded, and non-certifying.
5. Invalid fields produce actionable diagnostics.
6. Signal Passage and tic-tac-toe both pass.

### Sound linkage

7. Linkage is explicit.
8. Sound definitions remain runtime authority.
9. Synthesis definitions remain generation source truth.
10. Missing, stale, drifted, orphaned, duplicated, and incompatible mappings are detected.
11. Signal Passage migrates.
12. Tic-tac-toe cues validate.
13. Runtime sound commands remain unchanged.

### Performance

14. Existing small workloads remain available.
15. Below-10-ms timing is non-authoritative.
16. Counters and allocations remain comparable.
17. Four scaled real workloads exist.
18. Each targets at least 10 ms median.
19. No busy loops or heavy statistics are introduced.
20. Reports distinguish timing authority and expected cost.
21. M026 JSON/Markdown report exists.

### Tic-tac-toe

22. Workspace validates and consumer code is separate.
23. AI-vs-AI completes rounds autonomously.
24. Fixed seed yields deterministic choices.
25. Thinking uses simulation ticks.
26. Human can take/release X and O.
27. Invalid cells reject explicitly.
28. X win, O win, draw, reset, and score updates pass.
29. Board, ownership, thinking, placement, and result are readable.
30. Existing animation and particle capabilities are reused.
31. Required linked cues emit.
32. No board-game framework is introduced.

### Persistence/export

33. Save during thinking restores board, turn, controllers, phase, remaining ticks, and scores.
34. Old sounds, particles, and mark animations do not replay.
35. Continued deterministic AI policy is preserved.
36. External-style relocation passes.
37. Linux export and development/export equivalence pass.
38. Linkage remains valid in export.

### Boundary decisions/review

39. Both consumers are compared.
40. Every required candidate receives an explicit decision.
41. Repeated and one-off friction are distinguished.
42. Unsupported broad APIs are rejected or deferred.
43. Required review request exists.
44. Review record is approved and current.
45. All required shard receipts are atomic/current.
46. Only verifier success establishes completion.
47. M019–M025 regression gates remain valid.
48. No SDK, plugin platform, heavy benchmark program, new platform, TBP, or issue-template dependency is introduced.

## Direct documentation impact

Update only when active truth changes:

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
docs/engineering/performance-measurement.md
docs/specs/visual-definition-contract.md
docs/specs/geometric-visual-definition-and-projection-contract.md
docs/specs/sound-definition-and-command-contract.md
docs/specs/deterministic-offline-sound-synthesis-contract.md
docs/specs/comparative-performance-check-contract.md
docs/specs/game-workspace-manifest-contract.md
docs/specs/unified-agent-execution-workflow-contract.md
docs/artifacts/milestone-performance-report-artifact-contract.md
```

Do not perform unrelated synchronization.

## Deferred documentation synchronization hints

```text
.guide-sync/pending/2026-07-18-m026-capability-index-roadmap-and-consumer-sync.md
.guide-sync/pending/2026-07-18-m026-boundary-decisions-and-m027-planning-sync.md
```

Ordinary implementation agents do not read them.

## Human-review requirements

```text
applicability: required
blocking: yes
classes: visual, UX, artifact-quality
```

Request:

```text
.review/pending/M026-geometry-diagnostics-and-autonomous-tic-tac-toe.md
```

Required evidence includes geometry inspection/preview, all-shape capture, Signal Passage diagnostic run, tic-tac-toe board/thinking/mark/takeover/win/draw/reset captures, linkage report, cue inventory, deterministic AI result, save-during-thinking result, Linux export launch, performance report, boundary report, and current non-review receipts.

Approval requires understandable diagnostics, readable board/controller state, comprehensible takeover, distinguishable presentation states, supportive sounds/particles, and evidence-aligned boundary decisions.

Re-review triggers: diagnostic format/severity, contrast policy, board mapping, controller interaction, thinking/mark/particle presentation, linkage semantics, boundary classifications, or evidence fingerprints.

Completion:

```text
automated validation + approved current review + M026 verifier = complete
```

## Constrained-runtime handling

1. Run `./eng/m026-smoke.sh --plan-json`.
2. Execute each non-review shard separately.
3. Stop on real failure.
4. Generate review evidence and request.
5. If review is pending, report the exact blocking review ID and do not claim completion.
6. After approval, run/refresh `human-review`.
7. Run `./eng/m026-smoke.sh --verify`.
8. Report the verifier result.

Do not use backgrounding, detached processes, timeout inflation, or partial child output as proof.

## Out-of-scope guide migration work

No guide migration is included. Do not modify `.guide-profile.json`, copy guides/prompts, reference external guides as implementation authority, introduce TBPs/issues, require `.guide-sync/` reading, or perform release-readiness/broad synchronization work.
