# ADR-0012 — Establish Asset Metadata Before Visual Workbench

## Status

Proposed for Milestone 007.

## Context

The repository has established the base engineering substrate, minimal deterministic runtime, product CLI, scenario runner, and content validation foundation.

The asset pipeline principles state that raw asset creation is out of scope while asset interpretation, import, metadata authoring, previewing, validation, and provenance are in scope. The same principles separate structural, visual, physical, gameplay, narrative/design, and technical metadata layers.

The content rules state that source content must be schema-validated, stable-ID addressable, agent-inspectable, human-reviewable, and separated from generated outputs. They also require human review for high-impact semantics such as collision, walkability, navigation-critical metadata, damage zones, interactability, and progression blockers unless explicit project rules say otherwise.

The project needs asset curation, but a graphical workbench or image perception workflow would be premature without durable asset metadata, validation, and inspection artifacts.

## Decision

Milestone 007 establishes an asset metadata and tile atlas curation slice before implementing a visual asset workbench.

The milestone introduces:

```text
authored asset metadata JSON
raw PNG fixture reference
asset metadata content validation
agentic2d asset inspect <asset-id-or-path> --output <directory>
asset inspection artifacts
human-review boundary for gameplay-relevant semantics
```

The first asset domain is a small tile atlas fixture. The fixture is used to validate structural handling and evidence generation, not art production.

The implementation must keep proposed visual labels distinct from approved physical/gameplay behavior. Approved high-impact semantics require explicit review evidence.

## Consequences

- The asset pipeline gains a durable source-of-truth metadata shape before UI or image perception work.
- Future asset curation workbench milestones can operate on validated metadata instead of inventing ad hoc files.
- Future map and tileset workflows can reference stable asset and tile IDs.
- Asset inspection can produce evidence without renderer integration.
- Semantic approval rules are established before AI-generated labels or gameplay metadata are introduced.
- The implementation remains bounded and can be validated through product CLI and engineering wrappers.

## Alternatives considered

### Build the visual asset curation workbench next

Rejected for Milestone 007.

A workbench would need asset metadata, validation, provenance, and semantic review rules. Building UI first would likely produce unstable implicit data contracts.

### Add image recognition or AI tagging first

Rejected.

AI-proposed tags are useful later, but the repository must first define how proposed labels differ from approved source truth and how review-gated semantics are represented.

### Integrate a renderer or preview image generation first

Rejected.

Preview artifacts are useful later, but structural metadata and inspection artifacts can be validated without renderer dependencies.

### Use only content validation without an asset inspection command

Rejected.

Content validation proves metadata shape. Asset inspection proves the metadata is connected to a raw asset and can produce agent/human evidence. Both are needed for an artifact-first asset slice.

## Non-goals

- graphical asset curation workbench;
- production asset library;
- image recognition;
- AI tagging;
- renderer integration;
- map editing;
- animation editing;
- shader/material preview;
- atlas packing;
- packaged-runtime validation;
- release readiness.
