# Milestone 006 — Content Schema Validation Foundation

## Goal

Introduce the first reusable content validation foundation for authored project data and expose it through the product CLI.

The milestone starts with scenario content because the repository already has authored scenario JSON from Milestone 005. The implementation must validate authored scenario files before or independently from running them and must produce structured validation evidence.

Required validation flow:

```text
authored content scope
→ content loader
→ contract validation
→ stable ID and reference checks
→ structured diagnostics
→ result.json and diagnostics.json
→ product CLI exit code
```

The milestone must produce real validation behavior. It must not create a success-only command that merely checks whether a file exists or whether JSON can be parsed.

## Repository role and maturity assumptions

Repository role:

```text
capability-provider
```

This repository builds engine/runtime/tooling capabilities. Milestone 006 validates the capability to validate authored content. It does not use the engine to build or validate a separate consumer game.

Current maturity assumption:

```text
implementation-ready for base engineering, minimal runtime, first product CLI, and scenario runner foundation
artifact-first
```

The milestone assumes Milestones 001–005 are implemented:

- canonical `eng/` scripts exist;
- `src/Agentic2D.Contracts`, `src/Agentic2D.Engine`, `src/Agentic2D.ScenarioRunner`, and `src/Agentic2D.Tools` exist;
- the minimal deterministic runtime exists;
- the product CLI supports runtime smoke and scenario execution;
- the repository has an authored `runtime.smoke` scenario;
- `./eng/check.sh`, `./eng/cli-smoke.sh`, `./eng/product-validate.sh`, and `./eng/scenario-smoke.sh` exist.

## Execution mode

```text
ai-executed-human-reviewed
```

Scope size:

```text
medium coherent vertical slice
```

Implementation autonomy:

The implementation agent may modify multiple components when changes remain inside this milestone's content validation contracts and validation expectations.

Expected touched areas include the product CLI, a content validation project or module, scenario validation integration, unit tests, engineering wrappers, and directly affected project documentation.

The implementation agent must not expand the milestone into asset curation, map authoring, visual previews, full JSON Schema registry work, packaged-runtime validation, release work, or guide-system migration.

## Scope

Implement a content validation foundation with scenario JSON as the first supported content domain.

Required product capability:

```text
agentic2d content validate <scope-or-path> --output <directory>
```

Required initial supported scopes:

```text
scenarios
game/scenarios/smoke/runtime-smoke.json
```

Required development invocations:

```bash
dotnet run --project src/Agentic2D.Tools -- content validate scenarios --output artifacts/content/scenarios
dotnet run --project src/Agentic2D.Tools -- content validate game/scenarios/smoke/runtime-smoke.json --output artifacts/content/runtime-smoke
```

Required engineering validation wrapper:

```bash
./eng/content-validate.sh scenarios
```

Optional engineering wrapper when simple and meaningful:

```bash
./eng/content-smoke.sh
```

The content validator must parse authored JSON content, validate the required scenario contract shape, validate stable IDs and references, emit structured diagnostics, and write validation artifacts.

## Non-goals

Do not implement any of the following in this milestone:

```text
asset curation workbench
asset import pipeline
image inspection
PNG parsing
map validation scenarios
animation validation scenarios
shader/material preview scenarios
UI scenarios
save/load scenarios
performance or soak scenarios
packaged-runtime validation
visual review packs
full JSON Schema registry
external JSON Schema package dependency unless clearly justified
schema generation
source generators
renderer integration
raylib-cs integration
MonoGame integration
public documentation
release readiness
NuGet packaging
GitHub Actions workflows
TBPs
issue templates
guide-system migration
```

Do not make `.guide-profile.json` or `.guide-sync/` required reading for ordinary implementation agents.

Do not copy guide documents or prompt templates into this repository.

## Required authority documents

A later implementation agent must read only these project authority documents before implementing this milestone:

```text
README.md
AGENTS.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/validation-tiers.md
docs/engineering/future-dotnet-solution.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/CONTENT.md
docs/SCENARIOS.md
docs/ARTIFACTS.md
docs/specs/product-cli-contract.md
docs/specs/scenario-runner-contract.md
docs/specs/content-validation-contract.md
docs/scenarios/scenario-runner-foundation.md
docs/artifacts/scenario-runner-artifact-contract.md
docs/artifacts/content-validation-artifact-contract.md
docs/decisions/ADR-0011-content-validation-before-asset-curation.md
```

Do not require the implementation agent to read the external guide repository, copied guide documents under `docs/research/`, `.guide-profile.json`, `.guide-sync/`, prompt templates, TBPs, or issue templates.

## Files or areas likely affected

Likely source and test areas:

```text
src/Agentic2D.Contracts
src/Agentic2D.ScenarioRunner
src/Agentic2D.Tools
tests/unit/Agentic2D.Tests.Unit
```

A new project is allowed only if the implementation remains focused and the resulting dependency graph is clear:

```text
src/Agentic2D.Validation
```

Recommended project reference direction if a new validation project is created:

```text
Agentic2D.Validation -> Agentic2D.Contracts
Agentic2D.ScenarioRunner -> Agentic2D.Validation
Agentic2D.Tools -> Agentic2D.Validation
Agentic2D.Tests.Unit -> Agentic2D.Validation
```

If no new project is created, the validation module may live inside `Agentic2D.ScenarioRunner` or `Agentic2D.Tools` temporarily, but the implementation summary must explain the choice.

Likely authored content area:

```text
game/scenarios/smoke/runtime-smoke.json
```

Likely engineering script area:

```text
eng/content-validate.sh
```

Optional if implemented:

```text
eng/content-smoke.sh
```

Likely documentation areas directly affected by the implementation:

```text
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
docs/specs/product-cli-contract.md
docs/CONTENT.md
docs/ARTIFACTS.md
```

## Implementation constraints

### Content validation boundary

Milestone 006 validates authored JSON content. It does not execute scenario runtime behavior as the primary validation outcome.

A valid scenario file may later be executed by the scenario runner, but content validation must also be able to fail before runtime execution when source content is malformed.

### Initial domain

The first supported content domain is:

```text
scenarios
```

The validator must support:

```text
game/scenarios/smoke/runtime-smoke.json
```

and should support repository scope:

```text
scenarios
```

where `scenarios` validates the known authored scenario content under:

```text
game/scenarios/
```

Full recursive discovery may be narrow. It is acceptable for `scenarios` to validate only the currently known scenario folder and files if this behavior is documented in the implementation summary and direct docs.

### No dependency on runtime execution

Content validation must not require scenario runtime execution to identify structural content errors.

The implementation may optionally call shared scenario parsing code, but it must distinguish:

```text
content validation failure
scenario execution failure
```

### Diagnostics

Diagnostics must use stable IDs that tests can assert without depending on prose.

Recommended initial diagnostic ID families:

```text
CONTENT0001 missing required field
CONTENT0002 invalid schema value
CONTENT0003 invalid stable ID
CONTENT0004 duplicate ID
CONTENT0005 invalid reference
CONTENT0006 unsupported command type
CONTENT0007 unsupported assertion type
CONTENT0008 invalid artifact declaration
CONTENT0009 invalid human review declaration
CONTENT0010 invalid scope or path
```

Exact IDs may vary if they are stable, documented in tests, and consistently emitted.

### Product CLI behavior

`agentic2d content validate` must follow the existing product CLI exit-code policy:

```text
0 validation completed and passed
1 validation completed and failed
2 invalid CLI usage or invalid content input/scope
3 unhandled validation, artifact writing, or command failure
```

Invalid content shape may return exit code `1` or `2` depending on whether the implementation treats content contract violations as validation failures or invalid input. The chosen policy must be consistent, tested, and documented in direct docs.

Recommendation:

```text
1 for content contract violations
2 for invalid CLI usage, missing path, unsupported scope, malformed option values
3 for unexpected failures
```

### Artifact behavior

The content validation command must write:

```text
<output>/result.json
<output>/diagnostics.json
```

Optional but recommended:

```text
<output>/validated-items.json
```

Artifact contract authority:

```text
docs/artifacts/content-validation-artifact-contract.md
```

### Determinism

For the same content files, CLI arguments, and source revision, repeated validation runs must produce equivalent semantic artifacts.

Do not make validation outcomes depend on:

```text
wall-clock time
absolute paths
machine name
file enumeration order without deterministic sorting
current working directory except for resolving documented repository-relative inputs
network access
thread scheduling
```

### Human review boundary

Human review is not required to decide whether scenario content passes validation.

Human review is required for milestone acceptance only to judge whether diagnostics and artifacts are useful enough for future agents and humans.

High-impact asset or gameplay semantics remain review-gated by `docs/CONTENT.md`, but those domains are outside this milestone.

## Focus areas

### Focus Area A — Content validation contracts and model

#### Goal

Create the minimal reusable model for content validation results, diagnostics, targets, scopes, and validated item references.

#### Scope

Implement or define internal types for:

```text
content validation request
content validation result
content validation diagnostic
validated content item
content scope
content item reference
```

At minimum, the implementation must represent:

```text
scope or path
status
stable diagnostics
validated item IDs and paths
artifact paths
```

#### Validation tier

Tier 1 — Focused implementation.

#### Required validation

Run the focused unit tests that cover the validation model. If no focused wrapper exists, run:

```bash
./eng/test.sh
```

#### Direct documentation impact

Update direct docs only if the implementation chooses a material structure not already described by `docs/specs/content-validation-contract.md`.

#### Deferred documentation synchronization impact

A later documentation-sync pass may improve index tables and cross-links.

### Focus Area B — Scenario content validator

#### Goal

Validate authored scenario JSON files as the first content domain.

#### Scope

Validate at least:

```text
game/scenarios/smoke/runtime-smoke.json
```

Required checks:

```text
valid JSON
schema equals agentic2d.scenario.v1
required top-level fields exist
scenario ID is stable and valid
category is supported
runtime.ticks is positive integer
initial state entity IDs are unique and valid
steps use supported command types
move command references an existing entity
assertions use supported assertion types
assertion references target existing entities or expected events when applicable
artifact declarations are valid filenames
humanReview.required is boolean
```

#### Validation tier

Tier 1 — Focused implementation.

#### Required validation

Run unit tests for valid and invalid scenario content.

#### Direct documentation impact

None unless implemented validation behavior materially differs from the spec.

#### Deferred documentation synchronization impact

A later documentation-sync pass may update scenario docs to mention content validation after the implementation is complete.

### Focus Area C — Product CLI command

#### Goal

Expose content validation through the product CLI.

#### Scope

Add:

```text
agentic2d content validate <scope-or-path> --output <directory>
```

Support at minimum:

```text
scenarios
game/scenarios/smoke/runtime-smoke.json
```

Required development invocations:

```bash
dotnet run --project src/Agentic2D.Tools -- content validate scenarios --output artifacts/content/scenarios
dotnet run --project src/Agentic2D.Tools -- content validate game/scenarios/smoke/runtime-smoke.json --output artifacts/content/runtime-smoke
```

#### Validation tier

Tier 1 — Focused product CLI validation.

Tier 2 — Product/content validation gate when called through the engineering wrapper.

#### Required validation

Run direct CLI validation during implementation.

At minimum:

```bash
dotnet run --project src/Agentic2D.Tools -- content validate scenarios --output artifacts/content/scenarios
```

#### Direct documentation impact

Update:

```text
docs/specs/product-cli-contract.md
docs/engineering/product-cli.md
```

The docs must describe the command as supported only after implementation.

#### Deferred documentation synchronization impact

A later documentation-sync pass may improve examples in `README.md` and `AGENTS.md` if useful.

### Focus Area D — Engineering wrapper

#### Goal

Expose a canonical repository engineering command for content validation.

#### Scope

Create:

```text
eng/content-validate.sh
```

Required behavior:

```text
./eng/content-validate.sh scenarios
```

The wrapper must call the product CLI development invocation and fail with a non-zero exit code when validation fails.

Optional behavior:

```text
./eng/content-validate.sh <path>
```

This is allowed when simple and meaningful.

#### Validation tier

Tier 2 — Content validation gate for current maturity.

#### Required validation

Run:

```bash
./eng/content-validate.sh scenarios
```

#### Direct documentation impact

Update:

```text
docs/ENGINEERING.md
docs/engineering/command-contract.md
```

If a new validation project is created, also update:

```text
docs/engineering/future-dotnet-solution.md
```

#### Deferred documentation synchronization impact

A later documentation-sync pass may normalize command examples and milestone indexes.

### Focus Area E — Artifacts and diagnostics

#### Goal

Write deterministic content validation artifacts sufficient for agent diagnosis.

#### Scope

Required artifacts:

```text
<output>/result.json
<output>/diagnostics.json
```

Recommended artifact:

```text
<output>/validated-items.json
```

The artifacts must conform to:

```text
docs/artifacts/content-validation-artifact-contract.md
```

#### Validation tier

Tier 1 — Focused artifact contract tests.

Tier 2 — Content validation gate through `./eng/content-validate.sh scenarios`.

#### Required validation

Unit tests must cover artifact shape for passing and failing validation.

#### Direct documentation impact

Update:

```text
docs/ARTIFACTS.md
```

after implementation to index the new artifact contract and describe current content validation artifacts.

#### Deferred documentation synchronization impact

A later documentation-sync pass may normalize artifact cross-links.

### Focus Area F — Final validation and human review evidence

#### Goal

Prove that the content validation foundation works end-to-end and provide evidence for human review.

#### Scope

Run the required validation commands and summarize the produced artifacts.

#### Required validation

Run:

```bash
./eng/check.sh
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/scenario-smoke.sh
./eng/content-validate.sh scenarios
```

The milestone is incomplete unless these commands exit with code `0`, or the implementation summary reports the exact failing command and concise failure reason.

#### Validation tier

Tier 2 — Standard local and content validation gates.

#### Direct documentation impact

If validation behavior differs materially from the planned command names or artifacts, update direct docs before finishing.

#### Deferred documentation synchronization impact

Human review findings about evidence quality may be captured in `.guide-sync/pending/` by a later planning or documentation-sync task. Ordinary implementation agents must not read `.guide-sync/` for this milestone.

## Validation expectations

### Required final validation

```bash
./eng/check.sh
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/scenario-smoke.sh
./eng/content-validate.sh scenarios
```

### Expected successful content validation output

```text
artifacts/content/scenarios/result.json
artifacts/content/scenarios/diagnostics.json
```

Recommended output:

```text
artifacts/content/scenarios/validated-items.json
```

### What must not be required

Do not require:

```text
release validation
package smoke tests
benchmarks
packaged-runtime validation
asset import validation
map validation
visual preview validation
human approval to determine pass/fail
public documentation validation
GitHub Actions validation
```

Those validation classes are outside this milestone.

## Acceptance criteria

The milestone is complete when all of the following are true:

- `agentic2d content validate <scope-or-path> --output <directory>` exists through the development invocation.
- The command supports `scenarios` scope.
- The command supports validating `game/scenarios/smoke/runtime-smoke.json` by path.
- Content validation catches missing required scenario fields.
- Content validation catches invalid scenario schema value.
- Content validation catches duplicate entity IDs.
- Content validation catches unsupported command type.
- Content validation catches references to missing entities for move commands or assertions.
- Content validation emits stable diagnostics for failures.
- Content validation writes `result.json` and `diagnostics.json` under the requested output directory.
- Passing validation exits `0`.
- Validation failures exit according to the documented product CLI policy.
- `eng/content-validate.sh` exists and validates `scenarios` through the product CLI.
- Unit tests cover valid content and at least three invalid-content cases.
- Required final validation commands have been run.
- Directly affected documentation has been updated.
- No asset curation, image processing, full JSON Schema registry, TBP, issue-template, public-doc, release, workflow, or guide-system migration scope has been introduced.

## Direct documentation impact

The implementation agent must update documentation directly only where repository behavior changes immediately.

Required direct documentation updates after implementation:

```text
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/specs/product-cli-contract.md
docs/ARTIFACTS.md
docs/CONTENT.md
```

Conditional direct documentation update:

```text
docs/engineering/future-dotnet-solution.md
```

Update it only if a new `Agentic2D.Validation` project or another durable project seam is created.

Do not perform a broad documentation synchronization pass.

## Deferred documentation synchronization hints

This planning package may include `.guide-sync/pending/` files for index and follow-up cleanup. The implementation agent must ignore `.guide-sync/` unless explicitly assigned documentation synchronization work.

A later documentation-sync pass should update or check:

```text
docs/MILESTONES.md
docs/SPECS.md
docs/CONTENT.md
docs/ARTIFACTS.md
docs/DECISIONS.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/future-dotnet-solution.md
README.md
AGENTS.md
```

Only apply README or AGENTS changes if current guidance becomes stale or materially incomplete after implementation.

## Human review requirements

Human review is required for milestone acceptance, but not to decide whether validation passes.

The human reviewer should inspect the produced validation artifacts and answer:

- Can a malformed scenario be diagnosed from `result.json` and `diagnostics.json` without guessing?
- Are diagnostic IDs stable and granular enough for future agents?
- Is the content validation model reusable for asset metadata and map metadata later?
- Is the `content validate` CLI shape acceptable for future scopes?
- Did the implementation avoid overbuilding a full schema registry too early?

No visual, gameplay-feel, UX, or asset-semantic review is required in this milestone.

## Out-of-scope guide migration work

Guide-system migration is out of scope.

The implementation agent must not:

```text
modify .guide-profile.json
consume .guide-sync/pending/
copy external guide documents
copy prompt templates
add TBPs
add issue templates
```

The external guide system is planning context only. Project truth for this milestone is contained in the repository documents listed under required authority.

## Implementation summary requirements

The implementation agent's final response must include:

```text
Files created/modified
Whether a new validation project was created and why
Content scopes implemented
Product CLI commands added or changed
Engineering wrappers added or changed
Artifacts produced by validation
Validation commands executed
Validation results
Human review evidence summary
Any deviations from this milestone and why
Deferred documentation synchronization notes
```
