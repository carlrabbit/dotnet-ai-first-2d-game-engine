# Project Direction and Roadmap — Reconstructed Planning Context

## Status

Current planning context; non-authoritative.

## Question

What major directions are currently active, paused, or intentionally separate, and how should future milestone planning interpret them?

## Context

Corrective work consumed milestone numbers that had previously been mentally associated with feature-roadmap work. Milestone numbering is therefore a poor proxy for product direction.

The useful planning model is a set of tracks rather than a single numeric sequence.

## Current planning landscape

### Track A — corrective / architectural closure

A long corrective train strengthened foundations that later audits showed were semantically incomplete.

Major closure areas now represented in current authority include:

- shared simulation semantics and real abstract execution;
- fidelity ownership/reconciliation and multi-fidelity equivalence;
- canonical gameplay persistence and process-separated continuation;
- immutable runtime snapshots and atomic mutation authority;
- continuous spatial correctness and evidence;
- exact asset candidate/decision/promotion authority;
- actual candidate preview and human curation;
- modality-specific asset-preview review surfaces.

The remaining reserved asset-pipeline closure is:

`M049` — runtime/renderer/audio consumption of promoted content plus real dependency-aware affected rebuild.

Current asset authority explicitly reserves that scope. It should not be silently absorbed into unrelated work.

### Track B — agent-oriented UI

Current project authority defines a separate active UI direction:

- M052: typed compositional C# UI DSL, deterministic layout/interaction/presentation runtime, headless evidence, Raylib adapter, future-renderer boundary;
- M053: migrate real workbench operations/views through the new UI foundation;
- M054: comparative agentic-effectiveness evaluation against real tasks.

The architectural intent is important: mechanically complete UI software is not itself the research result. The distinctive question is whether the surface improves actual agentic game-development outcomes.

This track is active and should remain separate from the research-adoption cleanup.

### Track C — paused product/game feature roadmap

Before the corrective train took priority, the product roadmap broadly moved toward:

1. richer terrain/map authoring and procedural world generation;
2. stronger entity presentation, animation and visual life;
3. broader audiovisual content integration;
4. tutorial/story/statistics/export surfaces;
5. population departure and settlement-abandonment behavior;
6. distribution, compatibility, capture and final product polish.

This sequence is historical planning context, not current milestone authority.

Its assumptions are partly stale because the corrective train significantly strengthened runtime, persistence, asset and review foundations.

Do not revive old milestone definitions verbatim. Re-plan this roadmap from current authority when corrective/research organization work is complete.

### Track D — separate engineering/meta work

Keep these as explicit supporting tracks rather than mixing them casually into product milestones:

- documentation synchronization;
- guide-system migrations;
- inactive-platform catch-up;
- release/readiness work;
- research adoption and revalidation.

## Current ordering guidance

```text
organize durable planning knowledge
    ↓
continue active UI track independently
    ↓
reassess remaining corrective work, especially M049
    ↓
rebase the broader feature roadmap on current project truth
    ↓
resume feature milestones
```

This is planning guidance only. It does not authorize M049 or future feature implementation.

## Observed findings

- Milestone numbers alone no longer communicate roadmap direction reliably.
- Corrective milestones have often changed assumptions beneath older feature-roadmap ideas.
- The UI track has its own research/evaluation lifecycle and should not be conflated with asset cleanup.
- Broad documentation/platform/release tasks are healthier as explicit passes than as incidental scope in feature milestones.

## Inferences

- Future roadmap documents should be organized by planning track and capability dependency, not only numeric milestone order.
- M049 should be re-planned against current repository truth before execution even though its conceptual scope is already reserved.
- The broader feature roadmap should be treated as candidate direction until revalidated after the current cleanup period.

## Uncertainty / open questions

- Whether M049 remains the immediate next non-UI implementation milestone after this research pass.
- How much of the old terrain/world-generation roadmap remains the highest-value next feature direction.
- Whether UI research results will change authoring/workbench priorities enough to reorder feature work.
- When Linux catch-up should occur relative to the next product-facing milestone group.

## Detailed research

Use these when the planning question needs more than this track map:

- simulation/game baseline: `docs/research/simulation-and-settlement-evolution.md`;
- asset/workbench history and remaining closure: `docs/research/asset-authoring-and-workbench-evolution.md`;
- historical future-feature sequence and rebase questions: `docs/research/feature-roadmap-reconstruction.md`.

## Related authority

- `docs/specs/project-thesis.md`
- `docs/architecture/agentic-ui-direction.md`
- `docs/specs/approved-asset-and-deterministic-promotion-contract.md`
- `docs/engineering/platform-verification.md`

## Freshness

Last verified against repository authority: 2026-10-12.

Revalidate after M049, M052–M054, a platform-epoch switch, or any explicit roadmap reset.
