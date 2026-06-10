# Specs

## Authority

This document indexes internal behavioral authority for the engine.

Specs define what must be true. Milestones sequence work but do not define permanent behavior.

## Current spec documents

- `docs/specs/project-thesis.md`
- `docs/specs/runtime-principles.md`
- `docs/specs/agentic-workflow.md`
- `docs/specs/asset-pipeline.md`
- `docs/specs/behavior-modules.md`

## Initial invariants

- The engine is headless-first and CLI/API-first.
- Important project objects require stable IDs.
- Source-of-truth project data must be structured, diff-friendly, merge-friendly, schema-validatable, and reviewable.
- Runtime state must be observable through structured inspection.
- Scenario validation is a first-class engine concept.
- Failures must produce diagnostics and artifacts sufficient for agent or human diagnosis.
- Debug and packaged runtimes must preserve the same semantic behavior where both modes apply.
