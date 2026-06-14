# ADR-0011 — Establish Content Validation Before Asset Curation

## Status

Proposed for Milestone 006.

## Context

The repository now has a minimal runtime, product CLI, and scenario runner foundation. Authored scenario JSON is already part of the engine workflow.

The next visible product direction is asset curation. Asset curation will create and refine structured metadata for assets, maps, tilesets, animations, and gameplay semantics.

Those future workflows need a consistent way to validate authored source content before they can safely generate previews, overlays, or review packs.

The active content model requires authored non-code project data to be schema-validated, diff-friendly, merge-friendly, stable-ID addressable, inspectable by agents, reviewable by humans, and separated from generated outputs.

## Decision

Before implementing asset curation, introduce a reusable content validation foundation.

Milestone 006 starts with scenario JSON as the first supported content domain because the repository already has authored scenario content and scenario execution artifacts.

Milestone 006 introduces:

```text
agentic2d content validate <scope-or-path> --output <directory>
./eng/content-validate.sh scenarios
structured content diagnostics
content validation result artifacts
scenario-content validation as the first domain
```

## Consequences

Future asset, map, animation, and metadata work can reuse a validation pattern instead of inventing one-off validators.

Content validation becomes a product capability exposed through `agentic2d`, while `eng/` remains the repository engineering wrapper surface.

The asset curation workbench remains deferred until source metadata validation exists.

Milestone 006 should avoid implementing a full JSON Schema registry. A narrow hand-written validation foundation is acceptable while the content model is still small.

## Alternatives considered

### Start asset curation immediately

Rejected for Milestone 006. Asset curation would create metadata and review workflows before the repository has a reusable validation spine for authored content.

### Build a full JSON Schema registry first

Rejected. A full registry is likely premature and could overfit before the engine has enough content domains. The first validator should support the scenario format and define reusable result and diagnostic conventions.

### Keep validation inside the scenario runner only

Rejected. Scenario execution and content validation are related but different capabilities. Content validation must be able to fail before runtime execution begins.

### Treat validation as only an engineering script

Rejected. The engine is CLI/API-first and artifact-first. Content validation is product behavior exposed through `agentic2d`; `eng/content-validate.sh` is only the repository validation wrapper.

## Follow-up

A later milestone may use the content validation foundation for asset metadata and tile atlas curation.

Potential next milestone direction:

```text
Milestone 007 — Asset Metadata and Tile Atlas Curation Slice
```
