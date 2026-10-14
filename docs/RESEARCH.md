# Research

## Authority

This document is the compact retrieval index for non-authoritative planning research.

Research preserves evidence, reconstructed planning context, uncertainty, and revalidation triggers. It is not implementation authority. Ordinary implementation must use ready milestones and normal project authority.

## Current research map

| Subject | Status / freshness | Revalidate when | Related authority | Research path |
|---|---|---|---|---|
| Reconstructive adoption baseline | Current — 2026-10-12 | adoption scope expands or contradictory evidence appears | M051; project research boundary | `docs/research/reconstructive-adoption-baseline-2026-10.md` |
| Project direction and track map | Current — 2026-10-12 | M049 or M052–M054 materially completes/changes direction; explicit roadmap reset | project thesis; agent-oriented UI direction; asset promotion authority | `docs/research/project-direction-and-roadmap.md` |
| Simulation and settlement evolution | Current — 2026-10-12 | indirect-control, fidelity, population or core settlement semantics change | simulation/runtime/persistence authority; M032–M046 | `docs/research/simulation-and-settlement-evolution.md` |
| Asset authoring/workbench evolution | Current — 2026-10-12 | M049, substantial M053 workbench migration, new media kinds, or promotion semantics change | M047–M050 asset authority | `docs/research/asset-authoring-and-workbench-evolution.md` |
| Feature-roadmap reconstruction | Current historical roadmap — 2026-10-12 | M049 or M052–M054 completes; next product objective selected | M037 product shell; current project thesis and capability | `docs/research/feature-roadmap-reconstruction.md` |
| Architectural correction lessons | Current — 2026-10-12 | new subsystem intentionally changes authority/equivalence/publication patterns | runtime, persistence, fidelity, asset and UI authority | `docs/research/architectural-correction-lessons.md` |
| Validation and human-review lessons | Current — 2026-10-12 | review framework, validation host, or guide execution model materially changes | AGENTS; validation tiers; human-review contracts | `docs/research/validation-and-human-review-lessons.md` |
| Platform and development context | Current — 2026-10-12 | platform epoch, renderer, remote-access, graphics or audio topology changes | platform verification; UI direction | `docs/research/platform-and-development-context.md` |
| Agentic engine design evolution | Current reconstructed rationale — 2026-10-14 | project thesis, runtime/renderer authority, or provider role materially changes | project thesis; runtime/render/CLI authority | `docs/research/agentic-engine-design-evolution.md` |
| Human review/workbench evolution | Current reconstructed rationale — 2026-10-14 | review authority/workbench architecture materially changes | HUMAN-REVIEW; simple review workbench; M048/M050 | `docs/research/human-review-and-workbench-evolution.md` |
| Agent-oriented UI research context | Active research context — 2026-10-14 | M052/M053 implementation or M054 evaluation changes the hypothesis | agent-oriented UI direction | `docs/research/agent-oriented-ui-research-context.md` |
| Planning and bounded-dogfood lessons | Current project-specific methodology context — 2026-10-14 | repository role or external guide planning boundaries materially change | AGENTS; guide 0.9 planning/execution authority | `docs/research/planning-and-dogfood-lessons.md` |
| Early project summary | Historical / substantially superseded in detail | only when investigating early design intent | current project authority supersedes operative conclusions | `docs/research/agentic-2d-game-engine-project-summary.md` |

## Retrieval rule

Planning should load only research relevant to the current decision surface.

Suggested starting points:

- overall direction: `project-direction-and-roadmap.md`;
- core engine rationale: `agentic-engine-design-evolution.md`;
- gameplay/simulation: `simulation-and-settlement-evolution.md`;
- assets/workbench: `asset-authoring-and-workbench-evolution.md`;
- future product work: `feature-roadmap-reconstruction.md`;
- human review/workbenches: `human-review-and-workbench-evolution.md`;
- active UI hypothesis/rationale: `agent-oriented-ui-research-context.md`;
- project-specific planning/dogfood methodology: `planning-and-dogfood-lessons.md`;
- why prior corrective work mattered: `architectural-correction-lessons.md` and `validation-and-human-review-lessons.md`;
- platform-sensitive planning: `platform-and-development-context.md`.

Do not load the whole corpus merely because it exists.

If research contains a conclusion that implementation must obey, promote that conclusion into the appropriate spec, architecture, decision, engineering document, or ready milestone before implementation depends on it.

## Historical classification

The old copied `engineering-guide-v6.md` and `project-setup-guide-v7.md` were classified as legacy guide leakage during reconstructive adoption and removed. Reusable methodology remains owned by the external guide repository:

`carlrabbit/agentic-project-guides`
