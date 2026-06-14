# Asset Curation Workbench Contract

## Authority

This document is authoritative for the minimal generated asset curation workbench introduced by Milestone 010.

This document is not authoritative for:

- interactive editor architecture;
- persistent UI state mutation;
- image recognition;
- automatic semantic tagging;
- map editing;
- animation editing;
- shader/material preview;
- renderer integration;
- public documentation or release readiness.

## Purpose

The asset curation workbench is a generated review artifact that presents existing asset metadata, asset inspection evidence, and review-pack information for human inspection.

Initial flow:

```text
asset metadata
+ asset inspection artifacts
+ content validation artifacts
+ review pack
→ generated workbench artifacts
→ human inspection
```

The workbench is not a general editor. It must not mutate source metadata.

## Product CLI command

Milestone 010 introduces:

```text
agentic2d asset curate --asset <asset-id-or-path> --review-pack <review-pack-path> --output <directory>
```

Required development invocation:

```bash
dotnet run --project src/Agentic2D.Tools -- asset curate --asset asset.tile-atlas-smoke --review-pack artifacts/review/latest --output artifacts/workbench/asset-curation
```

## Supported asset target

Required initial asset target:

```text
asset.tile-atlas-smoke
```

Required direct metadata target:

```text
game/assets/metadata/tile-atlas-smoke.asset.json
```

Unsupported targets must produce a stable diagnostic and a non-zero exit code.

## Workbench type

The Milestone 010 workbench is generated static output.

Required generated files:

```text
<output>/index.html
<output>/review-data.json
<output>/diagnostics.json
```

Optional generated file:

```text
<output>/review-board.md
```

The generated `index.html` must be viewable from disk without a server, network access, package install, or JavaScript build pipeline.

## Review-state vocabulary

The workbench uses this review state vocabulary:

```text
proposed
approved
rejected
needs-revision
not-required
```

Required meanings:

| State | Meaning |
|---|---|
| `proposed` | Suggested or authored as a proposal. Not approved gameplay truth. |
| `approved` | Backed by explicit human review evidence in source metadata or review data. |
| `rejected` | Marked as rejected by explicit review evidence. Initial implementation may display but does not need to author this state. |
| `needs-revision` | Requires follow-up before approval. Initial implementation may display but does not need to author this state. |
| `not-required` | No approval required for the displayed proposal or structural fact. |

## Semantic separation rule

The workbench must keep these separate:

```text
visualLabelsProposed
physicalBehaviorsApproved
gameplay-relevant approved semantics
```

Rules:

- proposed visual labels may be displayed as proposals;
- proposed visual labels must not be displayed as approved physical/gameplay behavior;
- approved physical/gameplay behavior must show the review evidence or mark the approval evidence as missing;
- missing review evidence for physical/gameplay approval must be visible to the reviewer.

## Required workbench sections

The generated workbench must present at least:

```text
asset identity
source metadata path
raw asset path
structural tile atlas summary
tile list with stable tile IDs
proposed visual labels
approved physical/gameplay behaviors
review state per tile or semantic item
diagnostics summary
links or references to generated evidence artifacts
human review questions
```

## Relationship to source metadata

The workbench may read source metadata and generated artifacts.

The workbench must not write or modify:

```text
game/assets/metadata/*.asset.json
game/assets/raw/**
```

If a future milestone adds mutation or approval-writing, it must introduce a separate command and contract.

## Determinism requirements

For the same asset metadata, review pack, and source revision, repeated workbench generation must produce semantically equivalent `review-data.json`.

Semantic comparison includes:

```text
schema
asset ID
asset metadata path
source path
tile IDs and coordinates
visual proposal values
physical behavior values
review states
diagnostic IDs and severities
artifact reference kinds and paths
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

## Human review policy

Human review is required to judge whether the generated workbench is useful and whether it clearly separates proposed semantics from approved semantics.

The workbench does not approve semantics by itself.
