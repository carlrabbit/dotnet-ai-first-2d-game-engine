# Milestone 036 — Guide-System v0.7.2 and Cross-Platform Engineering Foundation

## 1. Goal

Migrate the repository from guide-system metadata/version `0.6.0` to `0.7.2` and establish a tested native development baseline for both Linux/Bash and Windows/PowerShell 7 before the postponed product-shell milestone proceeds.

The target state is:

```text
guide-system 0.7.2
  planning
    -> ready only when executable by the configured baseline implementation model
  implementation
    -> implement
    -> validate
    -> completion audit
    -> COMPLETE | AWAITING HUMAN REVIEW | BLOCKED

repository engineering
  Agentic2D.Engineering owns semantics
    -> thin Bash launchers on Linux
    -> thin PowerShell 7 launchers on Windows

parallel development
  Git is the synchronization boundary
  build outputs and machine-local assets remain host-local
```

M036 also performs an evidence-backed cleanup of historical `eng/*.sh` wrappers. Shell suite files that exist only for completed milestone history and have no current capability, regression, engineering, documentation, or supported platform purpose must be deleted instead of mechanically ported to PowerShell.

The previously planned product-shell milestone is postponed and becomes M037. M036 does not implement product-shell, UI, save-browser, settings UI, or input-rebinding product work.

## 2. Repository role and maturity assumptions

```text
repository role: capability-provider
profiles:
  - artifact-first-agentic-authoring
  - runtime-tool
  - game-simulation
current guide metadata: 0.6.0
target guide metadata: 0.7.2
current engineering baseline: Linux/Bash only
target engineering baseline: Linux/Bash + Windows/PowerShell 7
current maturity: implementation-ready, artifact-first, M035 complete
target maturity: cross-platform-engineering-ready for subsequent milestones
execution profile: ai-executed-broad
baseline implementation model: GPT-5.6 Luna
```

M000–M035 are historical completed milestones. Their completed reviews and completion decisions remain historical evidence and are not reopened.

The current M035 stale-receipt limitation may be addressed only at the general engineering/fingerprint/receipt layer. M036 must not attempt to make historical M035 receipts current after repository changes.

## 3. Execution mode

`ai-executed-broad`

This milestone is ready only when the configured baseline implementation model can execute it from this milestone, the listed repository authority, the live repository, and normal repository tooling without inventing a new material decision.

Implementation families:

1. guide-system 0.6.0 -> 0.7.2 migration and repository-local execution semantics;
2. platform-neutral engineering host and environment model;
3. active dual-platform launcher surface plus historical shell-suite inventory, migration, and deletion;
4. cross-platform filesystem, Git, path, temporary-file, process, and machine-local-data policy;
5. Linux/Windows parity validation, platform evidence, graphical development proof, and completion audit.

Focus areas are transformation families, not file edit allowlists. The executor may perform supporting repository-local work required to satisfy this milestone without introducing unrelated product scope.

## 4. Scope

### 4.1 Guide-system v0.7.2 migration

Update `.guide-profile.json` from the current v0.6.0 shape to the current v0.7.2 profile shape while preserving project truth.

Required effective values:

```text
guideSystem.repository: carlrabbit/agentic-project-guides
guideSystem.version: 0.7.2
base/applied profile version: 0.7.2

repositoryRole: capability-provider
maturityStage: implementation-ready

executionModel.baselineImplementationModel: GPT-5.6 Luna
executionModel.defaultImplementationMode: ai-executed-broad
executionModel.documentationSync: separate-pass
executionModel.releaseReadiness: separate-pass
executionModel.humanReview: milestone-scoped
```

After platform validation succeeds:

```text
engineeringCommands.canonicalInterface: eng
engineeringCommands.implementation: repository-specific
engineeringCommands.supportedLaunchers:
  - bash
  - powershell
engineeringCommands.testedPlatforms:
  - linux
  - windows
```

Adopt the v0.7 execution lifecycle semantics in localized repository instructions:

```text
planning -> ready milestone -> implementation
```

`ready` requires that the configured baseline implementation model can execute the milestone without inventing a new material decision about architecture, semantics, compatibility, scope, acceptance, or validation policy.

Adopt v0.7.1 executor closure semantics:

```text
implement
-> validate
-> completion audit
-> continue resolving agent-resolvable gaps
-> terminate only as:
     COMPLETE
     AWAITING HUMAN REVIEW
     BLOCKED
```

Passing tests is not by itself milestone completion.

Do not add generic `milestone-complete`, `milestone-check`, or equivalent commands merely for guide adoption.

### 4.2 Platform-neutral engineering authority

Keep `eng/` as the stable human/agent engineering command surface.

`src/Agentic2D.Engineering` owns non-trivial engineering semantics, including where applicable:

- suite registration;
- shard definitions and dependency relationships;
- plan generation;
- process invocation;
- repository/input/result fingerprints;
- receipt creation and verification;
- environment/platform classification;
- temporary paths;
- atomic file replacement;
- review-state operations;
- portable diagnostics;
- platform-parity evidence.

Shell and PowerShell launchers must not contain independently evolving copies of suite semantics, JSON schemas, fingerprints, review rules, or artifact validation.

### 4.3 Active launcher surface and historical shell cleanup

Do not port every historical Bash wrapper.

Inventory every tracked `eng/*.sh` file and classify it using the policy in `docs/engineering/cross-platform-development-and-launcher-policy.md`.

Required classifications:

```text
active-cross-platform
  current engineering/capability command that must work through Bash and PowerShell

active-platform-specific
  current supported command intentionally limited to one platform, such as a Linux export proof

thin-compatibility-wrapper
  retained because current repository authority or a live regression surface still references its stable name

historical-delete
  completed-milestone-only or superseded wrapper with no live capability/regression/engineering/documentation purpose
```

For `historical-delete`:

- delete the shell file;
- remove or update active documentation references;
- remove stale engineering-host registrations;
- remove current tests whose only purpose is preserving the obsolete wrapper name;
- preserve completed milestone documents and historical review records unchanged unless an active index incorrectly presents the wrapper as current.

Do not keep dead Bash files merely because they once belonged to a completed milestone.

Do not delete a wrapper solely because its milestone is complete if the wrapper still provides a current capability smoke, active regression, supported export proof, or stable engineering entry point.

Prefer a small generic cross-platform launcher family where it reduces duplication, for example:

```text
eng/suite.sh <suite-id> ...
eng/suite.ps1 <suite-id> ...
```

Repository-specific stable wrappers may remain where they materially improve usability or current compatibility.

Required native current surface includes at least:

```text
restore
build
test
format
check
test-filter or current focused equivalent
review list/show/check
resumable suite plan/shard/verify
current M036 migration suite
```

Windows launchers target PowerShell 7 through `pwsh`, not Windows PowerShell 5.1.

### 4.4 Cross-platform filesystem and Git behavior

Add explicit Git text/binary normalization through `.gitattributes`.

Required policy:

- deterministic text normalization;
- LF for shell and repository text formats unless a concrete tool requires otherwise;
- binary classification for PNG/WAV/other binary assets;
- no platform-generated mass line-ending diffs.

Move shared engineering semantics away from assumptions such as:

```text
/tmp
chmod as a semantic requirement
Bash quoting as data transport
hard-coded "/" or "\" path comparisons
case-sensitive path identity assumptions
shell-only temp-file behavior
shell-specific environment-variable expansion
```

Use .NET path, temporary-directory, process, and atomic-file APIs in the engineering host where practical.

Repository-relative serialized paths use canonical forward-slash representation.

Atomic replacement must stage on the same filesystem/volume when atomicity is required.

### 4.5 Machine-local development data

Git is the synchronization boundary between Linux and Windows development machines.

Do not synchronize through Git:

```text
bin/
obj/
ordinary generated artifacts/
IDE state
machine-local raw asset homes
temporary preview data
```

Continue to treat `AGENTIC2D_ASSET_HOME` as the portable explicit override.

Provide platform-native default shared asset-home resolution:

```text
Linux:
  XDG data location / existing Linux fallback

Windows:
  appropriate per-user local application-data location
```

Approved/promoted project assets remain repository/project authority. Raw asset-home contents remain machine-local authoring inputs.

### 4.6 Cross-platform receipt/environment model

Remove hard-coded Linux/Bash environment identity from generic engineering receipts.

Record environment metadata structurally, including at least:

```text
operating system
architecture
launcher family
.NET SDK/runtime identity
graphics-capability classification where relevant
```

Platform/environment metadata must not invalidate semantic fingerprints unless the platform is an explicit semantic input to the tested contract.

A receipt from one platform must never be silently relabeled as evidence from another.

Fix generalized fingerprint/receipt behavior that causes avoidable stale-receipt churn when generated evidence refreshes, where this can be done without weakening source/input/result freshness guarantees.

### 4.7 Platform support classes

Class A — semantic parity required on Linux and Windows:

```text
restore
build
unit tests
format verification
product CLI
headless simulation/content validation
engineering suite plan/shard/verify
review commands
save/load and deterministic headless proofs used by current development
```

Class B — functional support required on both but host evidence may differ:

```text
Raylib graphical development client
audio device interaction where available
window lifecycle
native input delivery
native library loading
```

Class C — intentionally platform-specific:

```text
Linux game export and Linux packaging proofs remain Linux-specific
Windows game export is deferred
```

Native Windows development support does not imply Windows distribution support.

### 4.8 Parallel development workflow

Document the supported model:

```text
Git remote / normal branches
        |
  +-----+------+
  |            |
Linux clone  Windows clone
  |            |
local build  local build
local art.   local art.
local asset  local asset
```

No shared cross-OS working directory is required or supported.

The repository must tolerate normal branch-based parallel development from either platform without line-ending churn, host-specific tracked outputs, or machine-specific absolute paths entering project truth.

## 5. Non-goals

Do not:

- implement the postponed product-shell milestone;
- implement M037 UI/menu/save/settings/input product work;
- implement Windows game export or packaging;
- activate CI merely to obtain Windows coverage;
- create duplicated Bash/PowerShell implementations of engineering semantics;
- port obsolete historical wrappers only for symmetry;
- rewrite completed milestone documents;
- reopen completed human reviews;
- make historical M035 receipts current;
- copy guide documents or prompt templates;
- make ordinary implementation agents read `.guide-profile.json` after migration;
- add TBPs or issue templates;
- add containers;
- introduce a new build system;
- replace Git with a file synchronization mechanism;
- add cross-machine asset-library synchronization;
- change gameplay semantics;
- change product save format unless required by a proven platform defect;
- broadly refactor the game engine because cross-platform support exists.

## 6. Focus areas

### Focus Area 1 — Guide 0.7.2 profile and execution contract

Implement the guide metadata migration and localized repository-routing changes.

Acceptance focus:

- v0.7.2 profile shape and values;
- baseline-model readiness is explicit;
- implementation termination semantics are explicit;
- active milestones use project-local authority;
- completed milestones remain historical;
- M037 is reserved for the postponed product shell.

### Focus Area 2 — Platform-neutral engineering host

Refactor engineering semantics that are currently Bash- or Linux-bound into tested .NET infrastructure.

Acceptance focus:

- no hard-coded generic `linux-bash` receipt identity;
- portable process/temp/path behavior;
- same suite semantics behind both launcher families;
- existing Bash behavior remains valid.

### Focus Area 3 — Launcher inventory, active PowerShell surface, and deletion

Produce the command inventory first, then act on it.

Acceptance focus:

- every tracked `eng/*.sh` receives one classification;
- active cross-platform commands have tested PowerShell 7 access;
- active Linux-specific commands are explicitly documented as such;
- obsolete historical shell suite files are deleted;
- current docs and suite registry contain no stale references to deleted wrappers;
- no mechanical `.ps1` mirror forest is created.

### Focus Area 4 — Git/filesystem/machine-local portability

Acceptance focus:

- `.gitattributes` prevents line-ending churn;
- platform-native temp and user-data behavior;
- no absolute machine path in durable evidence/project truth;
- existing Linux asset-home behavior preserved;
- Windows default asset-home behavior validated;
- same-file-system/volume atomic replacement proven on both hosts.

### Focus Area 5 — Cross-platform validation and completion

Acceptance focus:

- Linux and Windows both execute the active Class A surface;
- graphics-capable Raylib startup proof on both supported development platforms;
- semantic comparison reports differences only where platform metadata is intentionally different;
- M036 aggregate verification consumes explicit platform evidence;
- final completion audit distinguishes implementation success, validation success, and milestone completion.

## 7. Implementation constraints

### Project truth boundary

External guide material is migration input only. Project-specific operational semantics must be localized in active repository docs.

### Thin-launcher rule

A launcher resolves the repository/host, forwards arguments, invokes the tested .NET engineering command, and propagates exit status. Anything substantially more complex requires justification.

### Backward compatibility rule

Stable current engineering entry points may be retained where useful. Completed-milestone-only wrapper names are not a compatibility obligation.

### Historical cleanup rule

Git history is the archive. The working tree carries current operational truth.

### Platform claims

A platform is not supported because code compiles there. It is supported only after the required M036 platform proof succeeds.

### Platform-specific product capability

Linux-only export commands are valid current commands and must not be deleted merely because Windows development is added.

### Fingerprint authority

Platform metadata and semantic input fingerprints are distinct. The implementation must not solve stale receipts by weakening freshness or omitting meaningful inputs.

### Current tests

Update tests to current project truth. Do not preserve obsolete wrapper-specific tests when the wrapper is intentionally deleted.

## 8. Required authority documents

Read these before implementation:

1. `AGENTS.md`;
2. `README.md`;
3. `.guide-profile.json`;
4. `docs/ENGINEERING.md`;
5. `docs/engineering/command-contract.md`;
6. `docs/engineering/validation-tiers.md`;
7. `docs/engineering/constrained-validation-execution.md` if present;
8. `docs/engineering/human-review-workflow.md`;
9. `docs/engineering/future-dotnet-solution.md`;
10. `docs/TERMINOLOGY.md`;
11. `docs/SPECS.md`;
12. current engineering/fingerprint/review ADRs resolved through the decision indexes;
13. current shared-asset-home authority;
14. current Linux export authority needed to preserve the Class C boundary;
15. `docs/specs/cross-platform-engineering-contract.md`;
16. `docs/architecture/cross-platform-engineering-architecture.md`;
17. `docs/decisions/ADR-0047-engineering-semantics-are-platform-neutral-with-thin-native-launchers.md`;
18. `docs/engineering/cross-platform-development-and-launcher-policy.md`;
19. `docs/artifacts/cross-platform-engineering-migration-artifact-contract.md`;
20. this milestone document.

Implementation may inspect all current `eng/` launchers, engineering source/tests, solution/project files, `.editorconfig`, `.gitignore`, `global.json`, package props, and current platform-sensitive product code as implementation evidence.

Completed milestone documents may be inspected only when needed to determine whether a shell wrapper is historical-only or still defines a current supported capability. Do not treat completed milestone bodies as current engineering authority when permanent docs supersede them.

Do not require the external guide repository during implementation. The planning package already localizes the 0.7.2 migration decisions.

## 9. Files or areas likely affected

```text
.guide-profile.json
.gitattributes
.gitignore
AGENTS.md
README.md

docs/ENGINEERING.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/engineering/
docs/decisions/
docs/artifacts/

eng/*.sh
eng/*.ps1

src/Agentic2D.Engineering/
src/Agentic2D.Tools/                 # only where platform-neutral product CLI support requires it
shared asset-home implementation      # platform default resolution only
Raylib development host               # only platform-startup/native-loading fixes

tests/unit/Agentic2D.Tests.Unit/

artifacts/engineering/M036/
artifacts/validation/m036-smoke/
```

## 10. Validation tiers and concrete commands

Implementation may introduce a generic suite launcher. The exact final names must be documented in `docs/engineering/command-contract.md`.

### Tier 0 — metadata and repository sanity

Linux:

```bash
./eng/format.sh --verify
./eng/docs-check.sh
```

Windows equivalents after bootstrap:

```powershell
pwsh ./eng/format.ps1 --verify
pwsh ./eng/docs-check.ps1
```

If `docs-check.ps1` is unnecessary because a generic engineering launcher owns it, use that shared entry point instead.

### Tier 1 — focused engineering implementation

Linux:

```bash
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/check.sh
```

Windows:

```powershell
pwsh ./eng/restore.ps1
pwsh ./eng/build.ps1
pwsh ./eng/test.ps1
pwsh ./eng/check.ps1
```

Required focused test families:

```text
GuideProfileV072
EngineeringPlatform
EngineeringProcessRunner
EngineeringTemporaryPaths
EngineeringAtomicReplace
EngineeringFingerprint
EngineeringReceipt
EngineeringLauncherInventory
PowerShellLauncher
AssetHomePlatform
GitAttributesPolicy
```

Use the repository's current test-filter mechanism.

### Tier 2 — active surface parity

Linux must prove:

```text
restore/build/test/format/check
review list/show/check
suite plan/shard/verify
representative product CLI validation
representative headless simulation/save-load path
```

Windows must prove the same semantic surface.

PowerShell 7 is required.

### Tier 2 — platform-specific graphics development proof

Linux graphics-capable host:

```bash
./eng/raylib-debug-client-smoke.sh
```

Windows graphics-capable host:

```powershell
pwsh ./eng/raylib-debug-client-smoke.ps1
```

A generic cross-platform wrapper is acceptable.

The proof must record platform/native-library/window startup status explicitly.

### Tier 2 — launcher cleanup proof

Generate:

```text
artifacts/engineering/M036/launcher-inventory.json
artifacts/engineering/M036/launcher-cleanup-report.json
```

Validate:

- all tracked Bash launchers classified;
- every deleted launcher has no active references;
- every retained launcher has a current reason;
- no orphan PowerShell mirror exists without an active command purpose;
- active docs do not advertise deleted commands.

### Tier 3 — cross-platform semantic comparison

Generate one platform report on each host for the same repository revision/fingerprint:

```text
artifacts/engineering/M036/platform/linux/platform-verification.json
artifacts/engineering/M036/platform/windows/platform-verification.json
```

Comparison requires:

- same source revision;
- same relevant repository fingerprint;
- same .NET SDK policy from `global.json`;
- Class A semantic outcomes equal;
- allowed host metadata differences explicit;
- graphics development proofs passed on both hosts.

If evidence is produced on separate machines, transfer the small platform verification/report files through an explicit user-controlled mechanism. Do not commit transient platform artifacts solely to transport them.

### Tier 4 — M036 resumable aggregate

Provide Bash and PowerShell entry points over one suite definition:

Linux:

```bash
./eng/m036-smoke.sh --plan-json
./eng/m036-smoke.sh --shard <id>
./eng/m036-smoke.sh --verify
```

Windows:

```powershell
pwsh ./eng/m036-smoke.ps1 --plan-json
pwsh ./eng/m036-smoke.ps1 --shard <id>
pwsh ./eng/m036-smoke.ps1 --verify
```

A generic `suite.sh` / `suite.ps1` may replace the milestone-specific wrappers if documented and equally usable.

Required logical shards:

```text
guide-profile-v072
localized-execution-contract
engineering-host-portability
launcher-inventory
historical-shell-cleanup
git-line-endings-and-paths
asset-home-platform-defaults
linux-core
windows-core
linux-graphics
windows-graphics
platform-semantic-comparison
current-regression
documentation
integrated
```

Only final aggregate verification after both platform reports exist establishes automated validation success.

## 11. Validation execution mode

```text
Tier 0: direct
Tier 1: direct
Tier 2 Linux: direct on Linux
Tier 2 Windows: direct on Windows
Tier 2 graphics: graphics-capable host for the matching platform
Tier 3: cross-machine evidence comparison
Tier 4: resumable-sharded
human review: none required
```

M036 is intentionally cross-host. A single host cannot prove both platform support declarations.

## 12. Acceptance criteria

M036 is accepted only when all are true:

1. `.guide-profile.json` uses the v0.7.2 profile structure and reports guide-system v0.7.2.
2. The configured baseline implementation model is `GPT-5.6 Luna`.
3. The default implementation mode remains `ai-executed-broad`.
4. Repository-local planning truth defines `ready` as executable by the baseline implementation model.
5. Repository-local implementation guidance requires a completion audit.
6. Implementation runs terminate only as `COMPLETE`, `AWAITING HUMAN REVIEW`, or `BLOCKED`.
7. Completed milestones and reviews are not rewritten or reopened for guide adoption.
8. Ordinary implementation agents are not required to read external guides, `.guide-profile.json`, or `.guide-sync/`.
9. `Agentic2D.Engineering` owns non-trivial suite/receipt/fingerprint/platform semantics.
10. Generic receipt environment metadata is no longer hard-coded to Linux/Bash.
11. Bash and PowerShell 7 expose the same required active Class A engineering surface.
12. PowerShell support is native and tested, not a compatibility claim based only on .NET portability.
13. No broad duplicated Bash/PowerShell command logic exists.
14. Every tracked `eng/*.sh` file is classified.
15. Historical shell suite files with no current purpose are deleted.
16. Active docs and current suite registries contain no references to deleted wrappers.
17. Current capability/regression wrappers are not deleted merely because their originating milestone is complete.
18. Active Linux-only export/packaging commands remain explicitly platform-specific.
19. `.gitattributes` defines repository text/binary normalization suitable for parallel Linux/Windows development.
20. Normal Linux/Windows edits do not cause line-ending-only mass diffs.
21. Repository-relative durable paths use canonical portable representation.
22. Temporary directories and atomic replacement work on both tested platforms.
23. `AGENTIC2D_ASSET_HOME` remains the portable explicit override.
24. Linux asset-home default behavior remains valid.
25. Windows receives a validated per-user local default asset-home location.
26. Build outputs, ordinary artifacts, IDE state, and raw asset homes remain machine-local and untracked.
27. Linux restore/build/test/format/check pass.
28. Windows restore/build/test/format/check pass under PowerShell 7.
29. Current review commands function on both platforms.
30. Current resumable suite plan/shard/verify behavior functions on both platforms.
31. Representative product CLI and headless simulation/save-load behavior is semantically equivalent across platforms.
32. Raylib graphical development startup proof passes on both supported development platforms.
33. Windows game export remains explicitly deferred.
34. Platform verification reports are bound to the same relevant source revision/fingerprint.
35. Platform semantic comparison reports only declared host-specific differences.
36. General fingerprint/receipt stale-churn fixes do not weaken freshness guarantees.
37. Current affected regression tests pass.
38. `m036-smoke` aggregate verification passes.
39. The executor performs the v0.7.1-style completion audit after validation.
40. The final implementation report states exactly one terminal outcome: `COMPLETE`, `AWAITING HUMAN REVIEW`, or `BLOCKED`.
41. M037 product-shell work is not implemented.
42. No unrelated gameplay, guide-copy, TBP, issue-template, CI, packaging, or distribution scope is introduced.

## 13. Direct documentation impact

Implementation must update current project truth where behavior changes:

- `AGENTS.md`;
- `README.md` only where current platform/development claims change;
- `docs/ENGINEERING.md`;
- `docs/engineering/command-contract.md`;
- `docs/engineering/validation-tiers.md`;
- `docs/engineering/constrained-validation-execution.md` if active;
- `docs/engineering/cross-platform-development-and-launcher-policy.md`;
- `docs/TERMINOLOGY.md` for platform/launcher terms where needed;
- decision/artifact indexes;
- current shared-asset-home docs;
- current Linux-export docs only to state the Windows-development/non-Windows-export boundary;
- `.gitignore` / `.gitattributes` policy where required.

Do not broadly rewrite product capability docs.

The previously planned M036 product-shell package is external planning material and is not repository authority. It will be regenerated/renumbered as M037 after this milestone.

## 14. Deferred documentation synchronization hints

This package creates:

```text
.guide-sync/pending/2026-08-20-m036-guide-v072-cross-platform-engineering-sync.md
```

Ordinary implementation agents must not read or resolve it.

## 15. Human-review requirements

```text
applicability: none
completion effect: none
reason: platform support, launcher cleanup, guide metadata, semantic parity, and development-client startup are machine-decidable through explicit Linux/Windows evidence
```

No M036 review request is created.

If implementation discovers genuinely subjective acceptance that automation cannot decide, that is a planning gap rather than permission to invent a new blocking review.

## 16. Constrained-runtime handling

The suite must support resumable execution.

Required contract:

```text
suite: m036-smoke
plan: --plan-json
shard: --shard <id>
receipt root: artifacts/validation/m036-smoke/
verification: --verify
```

Each platform runs only shards valid for that platform plus platform-neutral shards as assigned by the plan.

Cross-platform completion requires both platform verification reports for the same relevant repository fingerprint.

Partial child output, one-platform success, or a graphics skip is not aggregate success.

A run on only one supported platform may complete all agent-resolvable local work but cannot report `COMPLETE`.

Under v0.7.1 terminal semantics:

- use `BLOCKED` only when the second platform or another required external capability is genuinely unavailable after all agent-resolvable work is complete;
- do not use `BLOCKED` for failing tests, missing launchers, incomplete cleanup, missing docs, or other agent-resolvable work;
- no human review is required, so `AWAITING HUMAN REVIEW` should not normally occur for M036.

## 17. Out-of-scope guide migration work

Do not migrate beyond v0.7.2.

Do not copy guide documents, prompt templates, ADRs, or meta documents into the target repository.

Do not make project documentation cite external guide files as operational authority.

Future guide versions require a separate migration decision.
