# Content Validation Contract

## Authority

This document is authoritative for the first reusable content validation foundation introduced by Milestone 006.

This document is not authoritative for:

- asset curation workflows beyond metadata validation;
- image inspection;
- map, animation, shader, or UI-specific content semantics;
- packaged-runtime validation;
- release readiness;
- public documentation;
- a full JSON Schema registry for all future content domains.

## Purpose

The content validation foundation validates authored non-code project data before runtime or scenario execution depends on it.

The first supported validation domain was authored scenario JSON. Milestone 007 adds authored asset metadata JSON validation.

Initial validation flow:

```text
content scope or file path
→ content loader
→ content contract validator
→ stable ID and reference checks
→ diagnostics
→ result.json and diagnostics.json
→ product CLI exit code
```

Validation must produce enough evidence for an agent or human to diagnose malformed content without guessing.

## Source content principles

Content validation must enforce the repository's source content principles where they apply to the supported domain:

| Principle | Current meaning |
|---|---|
| Schema-validated | Scenario JSON must conform to `agentic2d.scenario.v1`; asset metadata JSON must conform to `agentic2d.asset-metadata.v1`. |
| Diff-friendly | Authored content remains plain JSON. |
| Merge-friendly | Stable IDs and deterministic ordering are preferred where validation outputs lists. |
| Stable-ID addressable | Scenario IDs, entity IDs, step IDs, and assertion IDs must be stable strings. |
| Agent-inspectable | Validation artifacts expose item references and diagnostics. |
| Human-reviewable | Diagnostics and summaries are readable without inspecting source code. |
| Source/generated separation | Authored content is source; validation artifacts under `artifacts/` are generated evidence. |

## Supported content domains

The supported content domains are:

```text
scenarios
assets
```

Supported paths:

```text
game/scenarios/smoke/runtime-smoke.json
game/assets/metadata/tile-atlas-smoke.asset.json
```

Supported scopes:

```text
scenarios
assets
```

The `scenarios` scope validates authored scenario files under:

```text
game/scenarios/
```

The `assets` scope validates authored asset metadata files under:

```text
game/assets/metadata/
```

The implementation may initially validate the known smoke scenario and deterministic nested JSON files under `game/scenarios/`. File discovery must be deterministic if more than one file is discovered.

## Product CLI command

Milestone 006 introduces:

```text
agentic2d content validate <scope-or-path> --output <directory>
```

Development invocations:

```bash
dotnet run --project src/Agentic2D.Tools -- content validate scenarios --output artifacts/content/scenarios
dotnet run --project src/Agentic2D.Tools -- content validate game/scenarios/smoke/runtime-smoke.json --output artifacts/content/runtime-smoke
dotnet run --project src/Agentic2D.Tools -- content validate assets --output artifacts/content/assets
dotnet run --project src/Agentic2D.Tools -- content validate game/assets/metadata/tile-atlas-smoke.asset.json --output artifacts/content/tile-atlas-smoke
```

## Supported target forms

`<scope-or-path>` may be:

| Form | Required support | Meaning |
|---|---:|---|
| `scenarios` | Yes | Validate known authored scenario content. |
| `assets` | Yes | Validate known authored asset metadata content. |
| repository-relative `.json` path | Yes | Validate a single supported content file. |
| repository-relative `.asset.json` path | Yes | Validate a single asset metadata file. |
| arbitrary folder path | No | Deferred. |
| glob expression | No | Deferred. |
| map/animation domain name | No | Deferred. |

Unsupported scopes or malformed paths must produce a stable diagnostic and a non-zero exit code.

## Content validation result status

Validation result status must be one of:

```text
passed
failed
error
```

Use:

- `passed` when all content items pass validation and no error diagnostics exist;
- `failed` when content is loaded and validation finds contract, ID, or reference errors;
- `error` when validation cannot complete because of unexpected IO, serialization, or command failures.

## Exit code policy

The product CLI must follow the repository product CLI policy:

| Exit code | Meaning |
|---:|---|
| `0` | Validation completed and passed. |
| `1` | Validation completed and content failed validation. |
| `2` | Invalid CLI usage, unsupported scope, malformed option, or invalid target form. |
| `3` | Unexpected validation, IO, artifact writing, or command failure. |

The implementation may classify malformed JSON as either `1` or `2`, but the choice must be consistent and tested. Preferred policy: malformed authored content is validation failure `1`; invalid command-line shape is usage failure `2`.

## Scenario content validation

Scenario content validation must validate the contract from `docs/specs/scenario-runner-contract.md` for the supported Milestone 006 subset.

Required checks for scenario JSON:

| Check | Required behavior |
|---|---|
| JSON parse | File must be valid JSON. |
| `schema` | Must equal `agentic2d.scenario.v1`. |
| Required fields | Required top-level fields from the scenario runner contract must exist. |
| `id` | Must be a valid stable scenario ID. |
| `category` | Must be supported, initially `smoke`. |
| `seedPolicy` | Must be supported, initially `none`. |
| `runtime.ticks` | Must be a positive integer. |
| `initialState.entities` | Must be present and contain valid unique entity IDs. |
| `steps` | Must contain supported deterministic command steps. |
| Move commands | `entityId` must refer to an entity in initial state; `amount` must be an integer. |
| `expectedEvents` | Must contain valid stable event type strings. |
| `assertions` | Must contain supported assertion types and valid references. |
| `artifacts` | Declared artifact names must be relative filenames, not absolute paths. |
| `humanReview.required` | Must be a boolean. |

## Asset metadata validation

Asset metadata validation must validate `docs/specs/asset-metadata-contract.md` for the supported Milestone 007 subset.

Required checks for asset metadata JSON:

| Check | Required behavior |
|---|---|
| JSON parse | File must be valid JSON. |
| `schema` | Must equal `agentic2d.asset-metadata.v1`. |
| Required fields | Required top-level fields from the asset metadata contract must exist. |
| `id` | Must be a valid stable asset ID. |
| `kind` | Must be supported, initially `tile-atlas`. |
| `source.path` | Must be repository-relative, safe, and exist in the repository context. |
| `source.mediaType` | Must be `image/png`. |
| `tileAtlas` | Tile size, columns, and rows must be positive integers. |
| `tiles` | Tile IDs must be unique; coordinates must be within the declared grid and not duplicated. |
| `semantics` | Visual labels are proposals; approved physical/gameplay behaviors require review evidence. |
| `provenance` | `sourceKind` and `createdBy` must be present. |
| `humanReview` | Approved physical behavior review gate must be declared. |

## Stable ID policy

Milestone 006 must validate these ID classes:

| ID class | Example | Required rule |
|---|---|---|
| Scenario ID | `runtime.smoke` | Lowercase dotted segments. Stable across file moves. |
| Entity ID | `entity.player` | Non-empty stable string. Dotted lowercase preferred. Unique within a scenario. |
| Step ID | `step.move-player` | Non-empty stable string. Unique within a scenario when present. |
| Assertion ID | `assert.playerPosition` | Non-empty stable string. Unique within a scenario. |
| Event type | `entity.moved` | Non-empty stable string. |

The implementation may accept existing mixed-case assertion IDs such as `assert.playerPosition` to preserve compatibility with Milestone 005. If stricter ID rules are added, they must not invalidate the existing authored `runtime.smoke` scenario without updating that scenario source as part of the same implementation.

## Reference validation

The validator must detect invalid references where supported by the current scenario format.

Required reference checks:

```text
move command entityId references an entity in initialState.entities
entityExists assertion entityId references an entity in initialState.entities
entityPositionEquals assertion entityId references an entity in initialState.entities
eventOccurred assertion eventType references a valid expected or emitted event type declaration when represented in source
```

If the current authored scenario does not include all assertion types, tests must include synthetic invalid content cases.

## Diagnostic contract

Diagnostics must be structured and stable.

Minimum diagnostic shape:

```json
{
  "id": "CONTENT0001",
  "severity": "error",
  "message": "Missing required field: id",
  "target": "game/scenarios/smoke/runtime-smoke.json"
}
```

Recommended fields:

| Field | Required | Meaning |
|---|---:|---|
| `id` | Yes | Stable diagnostic identifier. |
| `severity` | Yes | `info`, `warning`, or `error`. |
| `message` | Yes | Human-readable explanation. |
| `target` | Yes | Content scope, path, or item reference. |
| `field` | No | JSON field or path when applicable. |
| `itemId` | No | Stable content item ID when applicable. |

Recommended diagnostic IDs:

| ID | Meaning |
|---|---|
| `CONTENT0001` | Missing required field. |
| `CONTENT0002` | Invalid schema value. |
| `CONTENT0003` | Invalid stable ID. |
| `CONTENT0004` | Duplicate ID. |
| `CONTENT0005` | Invalid reference. |
| `CONTENT0006` | Unsupported command type. |
| `CONTENT0007` | Unsupported assertion type. |
| `CONTENT0008` | Invalid artifact declaration. |
| `CONTENT0009` | Invalid human review declaration. |
| `CONTENT0010` | Invalid scope or path. |
| `ASSET0001` | Missing required asset metadata field. |
| `ASSET0002` | Invalid asset source reference. |
| `ASSET0003` | Invalid tile grid. |
| `ASSET0004` | Duplicate tile ID or coordinate. |
| `ASSET0005` | Semantic approval violation. |
| `ASSET0006` | Invalid provenance. |
| `ASSET0007` | Unsupported asset kind. |
| `ASSET0008` | Unsupported asset media type. |

Exact IDs may vary if they are stable and tests assert the chosen IDs.

## Artifact output

A content validation run must write these files:

```text
<output>/result.json
<output>/diagnostics.json
```

Recommended file:

```text
<output>/validated-items.json
```

The artifact contract is defined in:

```text
docs/artifacts/content-validation-artifact-contract.md
```

## Determinism requirements

For the same content files, CLI arguments, and source revision, repeated content validation runs must produce equivalent semantic artifacts.

Semantic comparison includes:

```text
schema
command
scope
status
exit code
validated item IDs
validated item paths
summary counts
diagnostic IDs and severities
```

Tests must not depend on:

```text
absolute paths
wall-clock timestamps
elapsed duration
local SDK path
machine name
process ID
filesystem enumeration order without deterministic sorting
```

## Relationship to scenario execution

Content validation is not scenario execution.

The content validator may share parsing and contract code with the scenario runner, but content validation must be able to report source-shape errors without starting runtime execution.

Scenario execution may rely on content validation before running a scenario, but Milestone 006 must avoid creating circular behavior where content validation requires scenario execution.

## Human review policy

Human review is not required to decide whether content validation passes.

Human review is required for milestone acceptance only to judge whether diagnostics and artifacts are useful for future agents and humans.

Review-gated asset semantics remain governed by `docs/CONTENT.md` and `docs/specs/asset-metadata-contract.md`.
