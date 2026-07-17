# Milestone 025 — Reference Game Workspace, Procedural Presentation, Synthesized Sound, and Extension Discovery

## Goal

Create the first coherent consumer-authored game vertical slice through an isolated in-repository workspace.

The milestone must prove:

```text
consumer workspace
→ consumer-owned code and authored content
→ procedural geometric presentation
→ deterministic synthesized sound assets
→ complete playable objective journey
→ save/resume
→ structured validation and human review
→ standalone linux-x64 export
→ consumer-extension boundary report
→ M025 performance-impact report
```

Working game identity:

```text
game ID: game.signal-passage
display name: Signal Passage
tone: abstract, calm, mechanical
session length: approximately 3–5 minutes
violence: abstract damage only
```

The primary acceptance question is:

> Can an implementation agent build, validate, review, save, resume, and export a coherent game primarily through consumer-owned code and content, while every engine-source change is explicitly classified?

## Repository role and maturity assumptions

```text
repository role: capability-provider
profiles:
  - artifact-first-agentic-authoring
  - runtime-tool
  - game-simulation
maturity:
  - implementation-ready
  - artifact-first
execution mode:
  - ai-executed-human-reviewed
guide-system metadata:
  - 0.5.1
```

This milestone is mixed provider/consumer dogfood with an explicit boundary.

Provider responsibilities:

- reusable geometric visual definitions and rendering projection;
- deterministic offline sound synthesis into ordinary WAV assets;
- validation, provenance, preview, and artifact contracts;
- workspace isolation and extension-boundary evidence support.

Consumer responsibilities:

- the Signal Passage workspace;
- shape/color assignments;
- synthesis definitions and cue meaning;
- game-specific components and behavior;
- objective journey;
- consumer scenarios;
- save/resume data;
- Linux export;
- performance report.

The consumer game does not establish stable public extension APIs. It discovers extension requirements for M026.

## Execution mode

```text
ai-executed-human-reviewed
```

Implement as one coherent vertical transformation through eight sequential focus areas:

1. isolated consumer workspace;
2. bounded geometric visual capability;
3. deterministic offline sound synthesis;
4. consumer-owned behavior and objective state;
5. complete playable journey and persistence;
6. consumer scenarios, Linux export, and performance report;
7. extension-boundary discovery report;
8. required human review and resumable aggregate verification.

Do not split the game into tiny feature tasks. Each focus area must leave the vertical slice executable or produce an explicitly transitional artifact.

## Scope

### Consumer workspace

1. Add `consumers/signal-passage/`.
2. Add `agentic2d.workspace.json`.
3. Add `agentic2d.project.json`.
4. Add separate consumer game-code project.
5. Add authored consumer content roots.
6. Add consumer-owned scenarios.
7. Use current workspace provider semantics.
8. Keep the engine-provider area read-only by default.
9. Prove workspace operation after relocation outside the repository.
10. Prove no accidental repository-relative engine-internal dependency.

### Procedural geometric presentation

11. Geometric visual parts.
12. Circle.
13. Rectangle.
14. Triangle.
15. Diamond.
16. Regular polygon with bounded side count.
17. Ring.
18. Line.
19. Fill color.
20. Optional outline color and width.
21. Finite size.
22. Rotation.
23. Opacity.
24. Anchor.
25. Layer and stable ordering.
26. Backend-neutral geometric render commands.
27. Raylib graphical projection.
28. Headless structural evidence.
29. Preview and review artifacts.

### Deterministic sound synthesis

30. Authored synthesis definitions.
31. Sine oscillator.
32. Square oscillator.
33. Triangle oscillator.
34. Deterministic noise oscillator.
35. Attack/decay/sustain/release envelope.
36. Start frequency.
37. Optional end frequency sweep.
38. Duration.
39. Gain.
40. Fixed supported sample rates.
41. Mono PCM output.
42. Deterministic WAV generation.
43. Provenance and source fingerprint.
44. Validation.
45. Preview/inspection.
46. Integration with existing sound definitions.
47. Generated-sound rebuild and verify.

### Signal Passage game

48. Player represented by a cyan circle.
49. Containers represented by orange diamonds.
50. Hazards represented by red triangles.
51. Energy fragments represented by yellow regular polygons.
52. Switch represented by a violet square/circle composition or bounded equivalent.
53. Exit represented by a green outlined gate/rectangles.
54. Dark blue-gray world background.
55. Walls and obstacle geometry.
56. Objective and HUD text.
57. Interaction prompts.
58. Health.
59. Fragment collection.
60. Containers.
61. Hazards.
62. Mechanism activation.
63. Exit opening.
64. Destination/objective zone.
65. Completion state.
66. Save and resume.
67. Synthesized collection, container, damage, switch, door, and completion cues.
68. Existing animation/effect/camera/UI/prompt capabilities where applicable.

### Consumer extension discovery

69. Track every game feature's implementation location.
70. Track engine-source changes.
71. Track internal API access.
72. Track boilerplate.
73. Track missing validation.
74. Track temporary workarounds.
75. Produce prioritized M026 extension candidates.

## Non-goals

Do not implement:

### Art and audio breadth

- PNG/sprite art for the reference game;
- external asset packs;
- SVG;
- arbitrary vector paths;
- Bézier curves;
- gradients;
- procedural materials;
- shaders;
- vector editor;
- runtime waveform synthesis;
- streaming synthesis;
- music;
- MIDI;
- polyphonic instruments;
- filters;
- reverb;
- dynamic mixing graph;
- audio effects processor;
- compressed audio generation;
- stereo synthesis.

### Game-system breadth

- full dialogue framework;
- quest framework;
- combat framework;
- equipment;
- abilities;
- pathfinding;
- NPC schedules;
- scene graph;
- cutscene system;
- procedural level generation;
- multiple game chapters;
- polished narrative;
- complex menus;
- localization;
- accessibility framework;
- multiplayer.

### Extension stabilization

- public SDK;
- NuGet packages;
- stable plugin API;
- general mod system;
- reflection-based plugin discovery;
- final consumer API compatibility promise;
- permanent separate game repository;
- source-provider changes;
- workspace migration/update support.

### Distribution and platform work

- another export target;
- installer;
- AppImage/Flatpak/Snap;
- release upload;
- signing;
- public release process;
- export optimization;
- trimming;
- single-file;
- NativeAOT.

## Focus Area 1 — Isolated consumer workspace

Create:

```text
consumers/signal-passage/
├─ agentic2d.workspace.json
├─ agentic2d.project.json
├─ game-src/
│  └─ SignalPassage.Game/
├─ game-content/
├─ game-scenarios/
└─ artifacts/                 # generated/ignored
```

Use exact current workspace manifest fields and area roles.

Required area roles:

```text
engine-provider
game-code
authored-content
generated-artifacts
tooling
```

The canonical in-repository workspace may use `directory-reference` for rapid development.

Add an isolation scenario that materializes an equivalent temporary workspace outside the repository using an existing supported provider such as deterministic `directory-copy`. The relocated workspace must build and run without relying on the original consumer path.

The game-code project must be a separate consumer assembly. Do not place Signal Passage behavior in engine projects unless the behavior is first classified as a generally reusable missing engine capability.

## Focus Area 2 — Geometric visual definitions and projection

Add a bounded geometric source variant to the visual-definition model.

A visual part uses exactly one source kind:

```text
asset-region
geometry
```

Initial geometry kinds:

```text
circle
rectangle
triangle
diamond
regular-polygon
ring
line
```

Required common fields:

- stable part ID;
- geometry kind;
- anchor;
- finite offset;
- finite positive world size;
- rotation;
- fill color where applicable;
- optional outline;
- opacity;
- layer;
- integer order;
- sort mode;
- provenance.

Additional bounded fields:

- polygon side count in a validated small range;
- outline width;
- ring inner ratio or thickness;
- line start/end or bounded length/width policy.

Geometric definitions remain presentation-only. They cannot define collision, entity identity, interaction, or gameplay.

Backend-neutral render artifacts must retain geometry kind and normalized parameters. Raylib translates those commands physically.

Do not rasterize geometry into generated PNGs as the authoritative representation.

### Signal Passage visual language

Lock these consumer choices:

```text
background: dark blue-gray
player: cyan circle
container/chest: orange diamond
hazard: red triangle
energy fragment: yellow hexagon or bounded regular polygon
switch inactive: violet square
switch active: violet square with bright inner circle
closed exit: solid green-tinted gate
open exit: two separated narrow green rectangles or outlined opening
objective zone: green ring
walls: muted blue-gray rectangles
```

Exact color values may be selected during implementation but must be centralized in consumer content and reviewed.

## Focus Area 3 — Deterministic offline WAV synthesis

Add authored synthesis definitions under an appropriate project-content root, recommended:

```text
game/sound-synthesis/
```

For the consumer workspace:

```text
consumers/signal-passage/game-content/sound-synthesis/
```

Each definition includes:

- schema;
- stable synthesis ID;
- output sound asset ID/path;
- oscillator;
- start frequency;
- optional end frequency;
- duration;
- gain;
- ADSR envelope;
- sample rate;
- deterministic noise seed where applicable;
- tags;
- provenance.

Supported oscillators:

```text
sine
square
triangle
noise
```

Generated output:

```text
mono
16-bit PCM WAV
bounded duration
validated sample rate
```

Recommended supported sample rates:

```text
22050
44100
```

Synthesis occurs during explicit content rebuild, not on the runtime audio thread.

Required commands, following current CLI conventions:

```bash
agentic2d sound synthesize <definition-or-id> --output <directory>
agentic2d sound synthesis inspect <definition-or-id> --output <directory>
agentic2d sound synthesis validate <scope> --output <directory>
```

Add a repository engineering wrapper to rebuild and verify generated sounds.

Generated WAV files must carry provenance through metadata and be consumed through existing `sound-definition` asset references and existing sound-cue projection.

### Required Signal Passage cues

```text
sound.signal-passage.fragment-collected
sound.signal-passage.container-opened
sound.signal-passage.player-damaged
sound.signal-passage.switch-activated
sound.signal-passage.exit-opened
sound.signal-passage.objective-completed
```

Suggested sound character:

```text
fragment: short rising sine/triangle
container: two-note ascending tone
damage: short descending square plus bounded noise
switch: firm tonal pulse
exit: low sweep with short noise component
completion: three-note ascending sequence
```

A multi-note cue may be represented as a bounded sequence of synthesis segments. Do not introduce a general music sequencer.

Equivalent definitions must generate byte-identical WAV data under the same implementation/runtime version and synthesis schema.

## Focus Area 4 — Consumer-owned behavior and objective state

Implement Signal Passage-specific code in the consumer assembly wherever current extension surfaces permit.

Required consumer concepts:

```text
EnergyFragmentComponent
ContainerComponent
HazardComponent
SignalPassageObjectiveComponent
```

Required behaviors or bounded equivalents:

```text
container interaction
fragment collection integration
hazard damage
objective progress
mechanism activation requirement
exit completion
```

Reuse engine-provided health, inventory, interaction, flags, switches, doors, persistence, UI, effects, sound, and presentation where possible.

The objective is:

```text
collect three fragments
→ activate mechanism
→ open exit
→ enter objective zone
→ complete run
```

If a consumer concept cannot be implemented without engine-source modification, record the need before implementing the smallest generally useful capability or temporary workaround.

Do not design the final M026 extension API during M025.

## Focus Area 5 — Playable journey, save/resume, and presentation

Required initial state:

- player with health;
- zero collected fragments;
- at least two containers;
- three fragments distributed through direct collection and/or containers;
- at least two hazards;
- inactive mechanism;
- locked/closed exit;
- destination zone behind the exit;
- HUD showing health and fragment progress;
- player-follow camera;
- required prompts and synthesized sound definitions.

Required journey:

```text
launch game
→ inspect objective
→ move through geometric world
→ open container
→ collect fragments
→ receive collection presentation and synthesized cue
→ enter or avoid hazard
→ receive damage presentation and synthesized cue
→ fail mechanism interaction before requirements
→ collect remaining fragments
→ activate mechanism
→ open exit
→ save before or after exit opening
→ destroy runtime
→ load fresh runtime
→ reconstruct persistent world and UI
→ do not replay transient feedback
→ enter objective zone
→ complete run
→ receive completion presentation and synthesized cue
```

Manual completion should take approximately 3–5 minutes on the development machine.

The vertical slice must remain understandable without debug overlays.

## Focus Area 6 — Consumer scenarios, export, and performance

Required consumer scenarios:

```text
signal-passage.workspace-smoke
signal-passage.geometry-presentation-smoke
signal-passage.sound-synthesis-smoke
signal-passage.objective-smoke
signal-passage.save-resume-smoke
signal-passage.complete-journey
signal-passage.external-workspace-smoke
signal-passage.linux-export-smoke
```

Required consumer wrappers or generic command invocations:

```bash
./eng/signal-passage-validate.sh
./eng/signal-passage-smoke.sh
./eng/signal-passage-review.sh
./eng/signal-passage-export.sh
```

Wrappers remain repository engineering conveniences. Consumer truth stays in workspace/project/scenario files.

Export the workspace through the M024 `linux-x64`, Release, self-contained directory export. Run isolated headless equivalence and direct graphical launch.

### Performance impact validation

Affected workloads:

```text
performance.entities-reference
performance.persistent-world-reference
performance.presentation-reference
signal-passage.complete-journey
```

Expected work-count increases:

- more entities and components;
- more behavior executions;
- more spatial queries/collision checks;
- more presentation commands;
- more UI/text commands;
- synthesized WAV files increase content and export size.

Required reports:

```text
artifacts/performance/M025/performance-report.json
artifacts/performance/M025/performance-report.md
```

Completion policy:

- explain elapsed changes of 5% or more;
- investigate or classify changes of 15% or more;
- distinguish expected workload growth from same-work regression;
- do not optimize unless evidence identifies a defect or acceptance failure.

## Focus Area 7 — Extension-boundary discovery

Produce:

```text
consumer-extension-report.json
consumer-extension-report.md
```

Every consumer feature must be classified as one of:

```text
engine-capability-reused
consumer-only
supported-consumer-extension
new-general-engine-capability
temporary-engine-workaround
boundary-violation
```

Each report entry includes:

- feature ID;
- consumer requirement;
- implementation location;
- engine files changed;
- engine internals accessed;
- current classification;
- rationale;
- validation evidence;
- consumer boilerplate;
- missing diagnostics/validation;
- recommended M026 action;
- priority.

The summary must answer:

1. Which ordinary game changes required engine modification?
2. Which internal APIs leaked into consumer code?
3. Which registration or serialization plumbing was repeated?
4. Which consumer capabilities should M026 formalize?
5. Which proposed extension points are not yet justified?
6. Is an external repository technically premature, viable with caveats, or ready?

M025 does not permanently move the game into another repository.

## Focus Area 8 — Human review and completion

Automated validation decides:

- schema validity;
- deterministic synthesis output;
- geometry command structure;
- gameplay state;
- save/resume;
- workspace isolation;
- export;
- equivalence;
- extension-report completeness;
- performance-report completeness.

Human judgment decides:

- visual object distinction;
- readability without debug overlays;
- color contrast;
- sound recognizability;
- sound comfort;
- game-loop comprehensibility;
- basic gameplay feel;
- whether the result is recognizably a game rather than a technical fixture.

Create a required review request under:

```text
.review/pending/M025-signal-passage-playable-vertical-slice.md
```

Completion is blocked until an approved, current record exists under:

```text
.review/records/
```

## Implementation constraints

- Consumer code and content remain separate from engine-owned fixtures.
- The engine-provider workspace area remains read-only unless an engine capability change is explicitly classified.
- Geometry and sound synthesis are reusable provider capabilities; Signal Passage meaning remains consumer truth.
- Geometry is presentation-only.
- Sound synthesis produces ordinary WAV assets offline.
- Runtime sound projection remains unchanged.
- No runtime-time synthesis affects simulation or physical audio timing.
- Generated WAV files are not hand-edited.
- Synthesized audio definitions and outputs retain provenance.
- The consumer game must function headlessly and graphically.
- Human review cannot substitute for failed semantic validation.
- Automated validation cannot approve visual/audio/game-feel acceptance.
- M025 may add generally reusable engine capability only when required by the vertical slice and documented in the extension report.
- Ordinary implementation agents do not read `.guide-profile.json`, `.guide-sync/`, external guides, copied research guides, prompt templates, TBPs, or issue templates.

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
docs/specs/entity-component-runtime-contract.md
docs/specs/entity-definition-and-instantiation-contract.md
docs/specs/deterministic-behavior-runtime-contract.md
docs/specs/spatial-query-and-trigger-contract.md
docs/specs/interaction-runtime-contract.md
docs/specs/visual-definition-contract.md
docs/specs/geometric-visual-definition-and-projection-contract.md
docs/specs/render-projection-contract.md
docs/specs/sound-definition-and-command-contract.md
docs/specs/sound-cue-projection-contract.md
docs/specs/deterministic-offline-sound-synthesis-contract.md
docs/specs/resource-damage-and-lifecycle-contract.md
docs/specs/item-inventory-and-collection-contract.md
docs/specs/gameplay-presentation-event-contract.md
docs/specs/canonical-save-snapshot-contract.md
docs/specs/persistence-contributor-contract.md
docs/specs/save-load-and-resume-contract.md
docs/specs/persistent-flag-and-condition-contract.md
docs/specs/stateful-world-entity-contract.md
docs/specs/mutable-world-projection-contract.md
docs/specs/declarative-ui-and-binding-contract.md
docs/specs/text-and-font-resource-contract.md
docs/specs/semantic-interaction-surface-contract.md
docs/specs/player-facing-presentation-composition-contract.md
docs/specs/lightweight-runtime-metrics-contract.md
docs/specs/comparative-performance-check-contract.md
docs/specs/standalone-linux-game-export-contract.md
docs/specs/exported-game-host-contract.md
docs/specs/signal-passage-consumer-vertical-slice-contract.md
docs/artifacts/unified-run-artifact-contract.md
docs/artifacts/milestone-performance-report-artifact-contract.md
docs/artifacts/standalone-game-export-artifact-contract.md
docs/artifacts/consumer-extension-discovery-artifact-contract.md
docs/decisions/ADR-0034-first-reference-game-is-an-isolated-in-repository-consumer-workspace.md
docs/decisions/ADR-0035-placeholder-presentation-uses-geometric-visuals-and-offline-synthesized-wav-assets.md
docs/milestones/MILESTONE-023-lightweight-runtime-metrics-comparative-performance-checks-and-milestone-performance-reporting.md
docs/milestones/MILESTONE-024-standalone-linux-game-export-and-development-export-equivalence.md
docs/milestones/MILESTONE-025-reference-game-workspace-procedural-presentation-synthesized-sound-and-extension-discovery.md
```

Read `.review/` only for the required M025 review workflow.

## Files or areas likely affected

Consumer workspace:

```text
consumers/signal-passage/
```

Likely provider projects:

```text
src/Agentic2D.Contracts
src/Agentic2D.Rendering
src/Agentic2D.Sound
src/Agentic2D.Validation
src/Agentic2D.ScenarioRunner
src/Agentic2D.Workspaces
src/Agentic2D.Tools
src/Agentic2D.DebugClient.Raylib
src/Agentic2D.GameHost
src/Agentic2D.Engineering
tests/unit/Agentic2D.Tests.Unit
```

Possible new focused project only if existing ownership is unsuitable:

```text
src/Agentic2D.SoundSynthesis
```

Generated areas:

```text
artifacts/consumers/signal-passage/
artifacts/performance/M025/
artifacts/exports/signal-passage/
artifacts/validation/m025-smoke/
```

## Validation tiers and concrete commands

### Tier 1 — Focused provider capability

```bash
./eng/build.sh
./eng/test-filter.sh Geometry
./eng/test-filter.sh SoundSynthesis
./eng/content-validate.sh visuals
./eng/content-validate.sh sounds
```

Use the nearest existing focused-test syntax when exact filters differ.

### Tier 1 — Consumer workspace

```bash
./eng/signal-passage-validate.sh
./eng/signal-passage-smoke.sh
```

### Tier 2 — Direct integration

```bash
./eng/check.sh
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/signal-passage-export.sh
```

### Tier 5 — Required human review

```bash
./eng/signal-passage-review.sh
./eng/review-list.sh
./eng/review-check.sh
```

`review-check` must report the M025 request as approved and current.

### Resumable M025 aggregate

```bash
./eng/m025-smoke.sh --plan-json
./eng/m025-smoke.sh --shard workspace-isolation
./eng/m025-smoke.sh --shard geometric-presentation
./eng/m025-smoke.sh --shard sound-synthesis
./eng/m025-smoke.sh --shard consumer-gameplay
./eng/m025-smoke.sh --shard save-resume
./eng/m025-smoke.sh --shard linux-export
./eng/m025-smoke.sh --shard performance-report
./eng/m025-smoke.sh --shard extension-discovery
./eng/m025-smoke.sh --shard human-review
./eng/m025-smoke.sh --shard integrated
./eng/m025-smoke.sh --verify
```

### Regression suites

Run M019 through M024 through their current `--plan-json`, separate required shards, and `--verify` interfaces.

Do not infer aggregate success from partial output.

## Validation execution mode

```text
focused capability and workspace checks: direct
M025 aggregate suite: resumable-sharded
creative/playable acceptance: human-review, required and blocking
```

Suite command:

```bash
./eng/m025-smoke.sh
```

Plan:

```bash
./eng/m025-smoke.sh --plan-json
```

Receipt root:

```text
artifacts/validation/m025-smoke/
```

Fingerprint scope includes:

- provider source/configuration relevant to geometry and synthesis;
- Signal Passage workspace/project/code/content/scenarios;
- generated synthesis definition/output fingerprints;
- review request and approval record;
- required export/equivalence/performance/extension artifacts.

Exclude machine-dependent timing values, physical audio-device state, screenshots' filesystem timestamps, and generated transient directories from semantic fingerprints.

Verification:

```bash
./eng/m025-smoke.sh --verify
```

Expected aggregate evidence:

- current receipts for every required shard;
- valid consumer workspace;
- isolated relocation success;
- geometric structural artifacts;
- deterministic WAV generation evidence;
- complete game journey;
- save/resume evidence;
- Linux export and equivalence evidence;
- M025 performance report;
- extension-discovery report;
- approved current human-review record.

## Acceptance criteria

### Workspace and consumer boundary

1. `consumers/signal-passage/` is a valid workspace.
2. Game code compiles as a separate consumer assembly.
3. Authored game content is consumer-owned.
4. Consumer scenarios are separate from provider capability tests.
5. Engine-provider area is read-only by default.
6. Relocated external-style workspace validation passes.
7. No accidental original repository path is semantically required.
8. Ordinary game logic is not silently placed in engine projects.

### Geometry

9. All required geometry kinds validate.
10. Unsupported kinds and invalid parameters reject.
11. Geometry is presentation-only.
12. Structural render artifacts preserve normalized geometry.
13. Ordering and fingerprints are deterministic.
14. Raylib projects all required shapes.
15. Geometric visuals work in the Linux export.
16. Signal Passage objects use the locked visual language.

### Sound synthesis

17. Required oscillator types validate.
18. ADSR and frequency fields are bounded.
19. Noise uses deterministic seed data.
20. Equivalent definitions generate byte-identical WAV data.
21. Generated WAV files are valid mono PCM.
22. Generated files retain provenance.
23. Generated files are not hand-edited authority.
24. Existing sound definitions consume generated WAV assets.
25. Runtime sound command semantics remain unchanged.
26. Required Signal Passage cues are generated and used.
27. Synthesized cues are included in the Linux export.

### Game journey

28. Player can move and interact.
29. Objective is understandable through HUD/prompts.
30. Containers open.
31. Three fragments can be collected.
32. Hazards cause bounded abstract damage.
33. Mechanism rejects activation before requirements.
34. Mechanism activates after requirements.
35. Exit opens.
36. Save/resume reconstructs persistent state and presentation.
37. Old transient presentation does not replay.
38. Objective zone completes the run.
39. Completion is represented structurally and physically.
40. Manual play length is approximately 3–5 minutes.

### Export and performance

41. Signal Passage exports through M024 `linux-x64`.
42. Export launches outside source/workspace.
43. Development/export semantic equivalence passes.
44. M025 JSON and Markdown performance reports exist.
45. Expected workload increases are distinguished from regressions.
46. Material differences are explained.
47. No unnecessary optimization scope is introduced.

### Extension discovery

48. Every feature has a boundary classification.
49. Every engine-source modification is listed.
50. Internal API leakage is listed.
51. Temporary workarounds are listed.
52. Boilerplate and missing diagnostics are listed.
53. M026 candidates are prioritized.
54. No unsupported claim of stable extension API is made.

### Human review

55. Required review request exists.
56. Review evidence includes geometry, audio, complete journey, export, and reports.
57. Review record is approved and current.
58. Visual object classes are distinguishable without debug overlay.
59. Colors have acceptable contrast.
60. Synthesized cues are recognizable and not uncomfortably loud/harsh.
61. Objective journey is understandable.
62. The result is recognizable as a small game.
63. `review-check` passes.

### Constrained validation and scope protection

64. M025 exposes list, plan, shard, verify, and no-argument modes.
65. Receipts are atomic and current.
66. Failed/interrupted shards leave no valid passing receipt.
67. Only successful verifier establishes aggregate completion.
68. M019–M024 regression gates remain passing.
69. No SDK, separate permanent game repository, external asset pack, runtime synthesizer, full dialogue/quest framework, new platform, installer, or public release infrastructure is introduced.

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
docs/specs/render-projection-contract.md
docs/specs/sound-definition-and-command-contract.md
docs/specs/game-project-manifest-contract.md
docs/specs/game-workspace-manifest-contract.md
docs/specs/unified-agent-execution-workflow-contract.md
docs/specs/standalone-linux-game-export-contract.md
docs/artifacts/unified-run-artifact-contract.md
docs/artifacts/standalone-game-export-artifact-contract.md
```

Do not perform unrelated synchronization.

## Deferred documentation synchronization hints

Created by this package:

```text
.guide-sync/pending/2026-07-17-m025-index-roadmap-and-consumer-workspace-sync.md
.guide-sync/pending/2026-07-17-m025-extension-findings-and-m026-planning-sync.md
```

Ordinary implementation agents do not read them.

## Human-review requirements

```text
applicability: required
blocking: yes
review classes:
  - visual
  - UX
  - creative
  - artifact-quality
```

Review subject:

```text
Signal Passage complete consumer vertical slice
```

Required request:

```text
.review/pending/M025-signal-passage-playable-vertical-slice.md
```

Required evidence:

- geometric visual definition summary;
- final structural render artifacts;
- graphical screenshots showing the complete visual vocabulary;
- synthesized sound definitions and WAV inventory;
- cue preview/playback evidence or review notes;
- complete-journey run artifact;
- save/resume artifact;
- Linux export manifest and isolated launch result;
- M025 performance report;
- consumer extension report;
- M025 verifier output available up to the human-review dependency.

Approval criteria:

- shapes and colors communicate object roles;
- prompts/HUD make the objective understandable;
- cues are distinguishable and comfortable;
- the 3–5 minute journey is coherent;
- the export presents as a game;
- no unresolved severe visual/audio/UX defect remains.

Record location:

```text
.review/records/
```

Re-review triggers:

- visual-language mapping changes;
- significant colors/contrast changes;
- synthesis oscillator/envelope or cue mapping changes;
- objective flow changes;
- complete-journey scenario changes;
- graphical host/export presentation changes;
- evidence fingerprints change.

Milestone completion behavior:

```text
automated semantic validation passes
+ current approved review record
+ M025 verifier passes
= complete
```

## Constrained-runtime handling

In a constrained environment:

1. run `./eng/m025-smoke.sh --plan-json`;
2. execute each required non-review shard in a separate invocation;
3. generate the review pack and request;
4. if review is pending, report the exact blocking review ID and do not claim completion;
5. after approval, execute or refresh the `human-review` shard;
6. run `./eng/m025-smoke.sh --verify`;
7. report the verifier result.

Do not use backgrounding, `nohup`, `setsid`, timeout inflation, or partial child logs.

## Out-of-scope guide migration work

No guide migration is included.

Do not:

- modify `.guide-profile.json`;
- copy guide documents or prompt templates;
- reference external guides as implementation authority;
- introduce TBPs or issue templates;
- require ordinary implementation agents to read `.guide-sync/`;
- perform release-readiness or broad documentation-synchronization work.
