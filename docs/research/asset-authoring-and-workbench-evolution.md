# Asset Authoring and Workbench Evolution — Reconstructed Planning Context

## Status

Current reconstructed planning knowledge; non-authoritative.

## Question

What is the durable direction of asset authoring/curation, which earlier implementations were superseded, and what remains unfinished?

## Early direction

The project has consistently treated assets as an agent-operable semantic pipeline rather than opaque editor state.

The durable idea was:

```text
raw source
+ observation/perception
+ semantic authoring
+ provenance
+ preview/evidence
+ validation
→ project-local usable presentation content
```

Important early concepts included stable IDs, semantic regions/frames, provenance, symbolic/map authoring, semantic labels, overlays, deterministic processing, and machine-readable evidence.

Not every early proposed command or taxonomy became current capability; the durable value is the semantic/provenance-first direction.

## Shared discovery versus game-local meaning

M028 separated:

```text
shared asset home
  raw source + reusable discovery/annotations

game-local campaign
  relevance + proposal/role/selection
```

This separation remains useful:

- shared discovery should not silently become game authority;
- one source may support several game-local interpretations;
- game-local approval/promotion belongs to the consuming project.

## M029 workbench model

M029 established several interaction ideas that remain useful:

- persistent workbench sessions;
- canonical text/mouse/touch actions;
- consequence-aware review;
- decision history/supersession;
- restartable separate preview-host process;
- deterministic promotion intent;
- presentation approval distinct from gameplay binding.

But later audit showed the authority chain was not mechanically strong enough.

Historical M029 implementation should therefore be read as the origin of the workbench interaction model, not as current decision/promotion authority.

## M047 authority correction

M047 rebuilt the core chain:

```text
exact source bytes
+ structured campaign candidate
→ canonical candidate fingerprint
→ exact v2 decision
→ typed variant/corrections
→ deterministic versioned recipe
→ immutable validated promoted generation
→ atomic current-generation publication
```

Major superseded assumptions:

- candidate ID/name is enough to find media;
- first matching PNG/WAV is an acceptable source;
- filename/candidate text can imply media semantics;
- free-text corrections can be recorded while processing ignores them;
- a staging-success flag is enough validation;
- mutable promoted directories are adequate publication authority;
- a shortened opaque approved ID is enough without collision semantics.

Current planning should build from exact candidate/variant/source identity and deterministic materialization, not recover heuristics.

## M048 preview correction

M048 made preview part of the same materialization chain rather than a separately labeled smoke scene:

```text
candidate
+ selected variant
+ typed corrections
→ canonical materialization subject
→ M047 recipe/materializer
→ disposable preview bundle
→ actual engine image/animation/audio path
→ human curation
→ v2 decision over the same subject
```

Key lesson:

A preview is truthful only when the exact shown/played media can be mechanically tied to the same subject the durable decision will bind.

Operational curation drafts are deliberately separate from durable decisions.

## M050 reviewer-facing correction

Human use then exposed a different defect: one generic executable had become one union-of-all-controls UI.

M050 clarified:

```text
generic preview infrastructure
+
structured mediaKind dispatch
→ modality-specific visible review surface
```

The visible image, animation and audio experiences should show only controls meaningful to the current subject and state plainly what is being judged.

Generic implementation does not imply generic visible chrome.

## Human review versus curation authority

Keep two concepts distinct.

### Milestone human review

The generic Review Workbench owns Restart/Reject/Accept for subjective milestone review.

Launching a preview from that exact review session is optional support. A reviewer may have inspected the subject elsewhere.

### Interactive asset approval

The asset-curation workflow may require a current exact preview acknowledgement before committing an approval-like decision because that acknowledgement is part of domain decision safety.

These are intentionally different semantics.

## Current unfinished closure — reserved M049

Current asset authority still reserves:

```text
runtime/renderer/audio consumption of promoted generations
+
real dependency-aware asset rebuild --affected
```

Until that work exists:

- promotion authority can be correct without proving downstream runtime consumption;
- affected rebuild must not claim dependency closure it did not perform.

This is the main remaining asset-pipeline cleanup direction.

## Relationship to the new UI track

M052–M054 should not invent a second asset-workbench domain model.

The new UI foundation may replace how workbench state and operations are projected/presented, but it should reuse:

- canonical asset candidates;
- curation drafts;
- v2 decision authority;
- preview materialization subjects;
- actual preview operations;
- promotion authority.

M053 is the natural integration point for moving workbench views through the new UI foundation.

## Durable planning lessons

- authoring state, preview state, decision authority and promoted authority should be separate but mechanically linked;
- raw/shared discovery is infrastructure, not game authority;
- typed corrections beat free-text processing intent;
- preview and promotion should share materialization semantics;
- publication should switch a small validated authority record, not mutate live content;
- machine validation should derive identity from bytes/references rather than producer claims;
- human review requires a clear, modality-appropriate experience;
- presentation approval must remain separate from gameplay binding.

## Superseded historical material

Treat as history/evidence rather than current design:

- M029 v1 decision records as promotion authority;
- fixed smoke media relabeled as the active candidate;
- substring/filename media semantics;
- generic union-of-controls preview UI;
- manual JSON/report inspection as human review;
- fake-success affected rebuild.

## Related authority

- `docs/architecture/asset-workbench-and-preview-host-architecture.md`
- `docs/specs/canonical-asset-candidate-contract.md`
- `docs/specs/asset-workbench-decision-and-consequence-contract.md`
- `docs/specs/approved-asset-and-deterministic-promotion-contract.md`
- `docs/specs/actual-candidate-preview-and-curation-contract.md`
- `docs/specs/asset-preview-review-surface-contract.md`
- M047–M050

## Freshness

Last reconstructed: 2026-10-12.

Revalidate after M049, substantial M053 workbench migration, new asset media classes, or a change to promotion/publication semantics.
