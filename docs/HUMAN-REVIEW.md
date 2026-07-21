# Human Review

## Authority

This document defines current human-review boundaries.

## Review-pack principle

Review generated evidence rather than reconstructing behavior from implementation source alone.

## Current review areas

### M028 shared asset-library foundation

The blocking M028 review is `review.m028.shared-asset-library-discovery-and-campaign-foundation`. It verifies safe generated cleanup, retained reusable annotations, campaign/profile authority separation, and usable headless review evidence. It remains pending until an explicit repository-user approval record exists.

### M029 choice-driven workbench and promotion

The blocking M029 review is `review.m029.choice-driven-workbench-preview-and-promotion`. It reviews editable text-stream and mouse/touch interaction, preview/recovery evidence, consequence-aware decisions, deterministic promotion provenance, and the M030 handoff. It does not approve consumer-game integration.

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

Required and blocking reviews use `.review/pending/`, `.review/records/`, and `.review/closed/`. Generated or large evidence remains in `artifacts/review/`.

Use the canonical review command family defined in `docs/engineering/human-review-workflow.md`. `./eng/review-check.sh --milestone <id>` evaluates only required/blocking reviews owned by that milestone and fails unresolved, malformed, superseded, or insufficiently evidenced reviews. Completed records are immutable historical evidence: later commits do not stale M022, M025, or M026. M027 requires `review.m027.authoring-contracts-review-evidence-and-v060-migration` and its durable consumer-authoring review pack.
