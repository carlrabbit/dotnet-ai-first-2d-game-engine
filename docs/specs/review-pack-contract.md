# Review Pack Contract

## Authority

This document is authoritative for the review pack capability introduced by Milestone 010.

This document is not authoritative for:

- public documentation;
- release readiness;
- package publishing;
- interactive editor behavior;
- image recognition;
- semantic asset approval policy beyond evidence aggregation and review-state reporting.

## Purpose

A review pack aggregates existing generated evidence into a bounded package that humans and agents can inspect without manually locating each artifact directory.

Initial flow:

```text
current generated artifacts
→ review pack command
→ review manifest
→ review summary
→ diagnostics
→ optional artifact index
```

The review pack is an evidence aggregator. It must not rerun product behavior unless explicitly invoked by engineering smoke scripts before the review pack command.

## Product CLI command

Milestone 010 introduces:

```text
agentic2d review pack --input <artifact-root> --output <directory>
```

Required development invocation:

```bash
dotnet run --project src/Agentic2D.Tools -- review pack --input artifacts --output artifacts/review/latest
```

## Supported input

The input is an artifact root directory.

Required support:

```text
artifacts
```

The command must discover only known artifact groups by contract shape or expected locations. It must not attempt to interpret every file as a known artifact.

## Known artifact groups

Milestone 010 must support these known artifact families:

| Artifact family | Existing contract |
|---|---|
| Scenario runner | `docs/artifacts/scenario-runner-artifact-contract.md` |
| Content validation | `docs/artifacts/content-validation-artifact-contract.md` |
| Asset inspection | `docs/artifacts/asset-inspection-artifact-contract.md` |

Optional future artifact families must be ignored or diagnosed without breaking current review pack generation.

## Output

Required output files:

```text
<output>/review-summary.md
<output>/review-manifest.json
<output>/diagnostics.json
```

Optional output file:

```text
<output>/artifact-index.json
```

Artifact shapes are defined by:

```text
docs/artifacts/review-pack-artifact-contract.md
```

## Status values

Review pack status must be one of:

```text
passed
failed
error
```

Use:

- `passed` when the review pack was generated and no error diagnostics exist;
- `failed` when known artifact groups were found but contain failed statuses or contract-level errors that need review attention;
- `error` when the pack cannot be generated because of unexpected IO, serialization, or artifact writing failures.

A review pack may be `passed` even when it contains review questions. Review questions are not validation failures by themselves.

## Review questions

The pack must generate review questions for evidence that automation cannot fully judge.

Initial required review questions:

- Are proposed visual labels acceptable as proposals?
- Are any approved physical/gameplay behaviors backed by human review evidence?
- Are diagnostics clear enough to diagnose failures?
- Are source references and artifact references sufficient for follow-up work?

## Determinism requirements

For the same input artifacts and source revision, repeated review pack generation must produce semantically equivalent artifacts.

Semantic comparison includes:

```text
schema
status
input root reference
included artifact group kinds
source item IDs
diagnostic IDs and severities
review question IDs
artifact reference kinds and relative paths
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

## Relationship to asset curation workbench

The asset curation workbench consumes a review pack or a review-pack-compatible manifest.

The review pack must therefore expose stable asset references and review questions for assets, but it must not mutate source asset metadata or record approvals.

## Human review policy

Human review is required for milestone acceptance to judge evidence usefulness.

The review pack does not approve gameplay semantics. It only reports evidence and review state.
