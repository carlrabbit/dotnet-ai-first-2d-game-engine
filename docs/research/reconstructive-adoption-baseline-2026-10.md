# Reconstructive Planning-Research Adoption Baseline — 2026-10

## Status

Current reconstructed planning baseline.

This document is non-authoritative planning knowledge. It records the scope, source classes, classifications, and remaining gaps of the October 2026 reconstructive adoption pass. Operative project requirements remain in normal specs, architecture, decisions, engineering documents, and ready milestones.

## Question

What historical planning knowledge is worth retaining so future planners can understand the engine's current direction, recurring architectural lessons, and unresolved planning surfaces without replaying months of conversations and milestone history?

## Context

The engine was largely designed before the project had a formal durable-research model. Valuable planning context therefore accumulated across planning conversations, milestone audits, corrective milestones, repository history, implementation evidence, and current authority.

M051 moved the repository to guide-system 0.9.1 and explicitly deferred historical reconstruction. This pass performs that reconstruction separately.

A concurrent UI track is active under `docs/architecture/agentic-ui-direction.md`. This research pass records that track as part of the planning landscape but does not redesign or implement it.

## Adoption mode

`reconstructive`.

The goal is a useful baseline across the small number of decision surfaces likely to matter repeatedly. It is not exhaustive historical recovery.

## Historical source classes used

Observed source classes available during this pass:

- current repository authority and source/test structure;
- milestone and completion records;
- current execution ledgers where they clarify unfinished work;
- Git/PR history visible in the repository;
- surviving project planning context available to the planning session;
- retained project-specific historical summary material.

Unavailable or unverified historical material is not reconstructed.

## Classification of pre-existing research

| Path | Classification | Treatment |
|---|---|---|
| `docs/research/agentic-2d-game-engine-project-summary.md` | historical project-specific planning input | Retain as historical source material; several technical and roadmap conclusions are superseded by later authority. Do not use as current project truth. |
| `docs/research/engineering-guide-v6.md` | legacy-guide | Remove from project research; external guide repository owns reusable methodology. |
| `docs/research/project-setup-guide-v7.md` | legacy-guide | Remove from project research; external guide repository owns reusable methodology. |

## Curated research surfaces created

- `project-direction-and-roadmap.md`
- `architectural-correction-lessons.md`
- `validation-and-human-review-lessons.md`
- `platform-and-development-context.md`

These artifacts summarize reusable findings rather than milestone chronology.

## Authority promotion review

Current repository authority already captures the major implementation-affecting conclusions reconstructed in this pass, including:

- project thesis and headless/artifact-first direction;
- authoritative runtime snapshot/mutation boundaries;
- multi-fidelity simulation equivalence;
- canonical save/persistence semantics;
- canonical asset candidate/decision/promotion authority;
- actual-candidate preview and modality-specific review surfaces;
- subjective-only human-review boundary;
- active Windows platform epoch and deferred Linux verification;
- typed compositional C# UI direction.

No new operative project rule is promoted solely from historical conversation in this initial pass.

If later reconstruction identifies a current requirement that exists only in research/history, planning must promote it into normal authority before implementation depends on it.

## Known gaps

This baseline intentionally does not yet reconstruct:

- a detailed feature-by-feature history before the current architectural foundation;
- every abandoned renderer/runtime option;
- every asset-workbench experiment;
- every simulation tuning discussion;
- every historical validation defect;
- the active UI track's eventual experimental results, because that work is still underway;
- M049 implementation planning, which should be revisited after planning organization is accepted.

## Freshness

Last reconstructed: 2026-10-12.

Revisit when:

- a major roadmap direction is added, removed, or completed;
- M049 closes the remaining promoted-content/rebuild gap;
- M052–M054 materially change the UI/agentic-authoring direction;
- a new renderer changes presentation boundaries;
- Linux becomes the active platform epoch;
- contradictory repository evidence appears.

## Planning implication

Future planning should begin with `docs/RESEARCH.md`, then load only the research surfaces relevant to the current decision.

Ordinary implementation should not need these documents to execute a ready milestone.
