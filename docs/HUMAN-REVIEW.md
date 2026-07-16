# Human Review

## Authority

This document defines current human-review boundaries.

## Review-pack principle

Review generated evidence rather than reconstructing behavior from implementation source alone.

## Current review areas

### Asset semantics and curation

Verify proposed versus approved semantics, asset provenance, review decisions, generated workbench usefulness, and that high-impact gameplay semantics remain review-gated.

### Maps and runtime evidence

Verify map references, static geometry, authored/runtime separation, runtime inspection completeness, and that diagnostics explain failures.

### Behavior and spatial boundaries

Verify behaviors read immutable snapshots and emit intents; spatial modules do not own entity identity; grid and continuous modules remain replaceable; collision evidence explains accepted, clipped, slid, and blocked outcomes.

### Entity definitions and interactions

Verify definition, spawn, and runtime entity IDs remain distinct; overrides and transactional instantiation are explainable; provenance is immutable; static objects are not silently converted to entities; trigger state is inspectable; interaction requires explicit intent and deterministic target selection; current interaction stops at `interaction.started`.

### Rendering

Verify:

- rendering cannot mutate or advance runtime;
- raylib types do not leak outside the adapter;
- the headless CLI works without native graphics;
- static/entity ownership remains distinct from visual references;
- PNG, asset, region, and render-item bindings are inspectable;
- static cache invalidation and ordering are credible;
- tree base/canopy occlusion is understandable;
- pause, step, and reset use runtime APIs;
- live and snapshot modes use one projector;
- screenshots are explicit review evidence;
- native resources are cleaned up;
- future animation, input, or alternate backends can extend projection without changing runtime authority.

### Consumer workspace evidence

Verify that workspace acquisition remains separate from game truth, the engine-provider area remains read-only by default, generated wrappers use workspace resolution instead of brittle machine paths, and `run-manifest.json` links the available structural evidence before optional screenshots.

## Evidence hierarchy

1. Structured source and contracts.
2. Deterministic JSON artifacts and diagnostics.
3. Review-pack summaries and manifests.
4. Explicit screenshots and graphical inspection where required.

Screenshot pixels are not cross-platform semantic truth.

## Repository-local review gate

Required and blocking reviews use `.review/pending/`, `.review/records/`, and `.review/evidence/`. Generated or large evidence remains in `artifacts/review/`.

Use the canonical review commands. `./eng/review-check.sh` fails required/blocking reviews that are unresolved, malformed, stale, superseded, or missing evidence. M022 requires an approved, current migration review covering shard boundaries, receipt/fingerprint trustworthiness, review usability, platform support, guide leakage, and ordinary-agent isolation.
