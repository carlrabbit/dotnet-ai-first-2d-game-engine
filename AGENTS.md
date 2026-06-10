# Agent Instructions

## Default implementation path

Read:

- `README.md`
- `docs/INITIALIZATION.md`
- `docs/ENGINEERING.md`
- the relevant milestone or task document under `docs/milestones/`
- relevant specs under `docs/specs/`

Use canonical `eng/` commands only after they exist. At this initialization stage, the `eng/` folder is a placeholder and scripts must be created explicitly by a setup milestone.

## Task-mode routing

### Planning

Read broadly across `docs/`, including architecture, decisions, milestones, content, scenarios, artifacts, and human review docs.

### Implementation

Read the relevant milestone focus area, directly relevant specs, and engineering command contracts. Do not perform broad documentation synchronization unless the task explicitly asks for it.

### Scenario/content validation

Read:

- `docs/SCENARIOS.md`
- `docs/CONTENT.md`
- `docs/ARTIFACTS.md`
- relevant documents under `docs/scenarios/`, `docs/content/`, and `docs/artifacts/`

### Documentation synchronization

Read broadly across project docs. Normalize indexes, cross-links, terminology, and deferred documentation impact.

### Human review

Read:

- `docs/HUMAN-REVIEW.md`
- relevant scenario/content/artifact documents
- generated review packs when they exist

## Authority rule

Do not treat `docs/research/project-setup-guide-v7.md`, `docs/research/engineering-guide-v6.md`, or `docs/research/agentic-2d-game-engine-project-summary.md` as operational authority. They are retained as source research. Project-specific rules must be extracted into active docs.

## Current hard constraints

- Do not add non-root `README.md` files.
- Do not create executable `eng/` scripts until the setup milestone explicitly does so.
- Do not create `.csproj`, `.slnx`, or package references until the .NET solution milestone explicitly does so.
- Do not add TBPs or issue templates by default.
- Generated outputs belong under `artifacts/` or `game/assets/generated/` and must not be treated as hand-authored source.
