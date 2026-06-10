# Project Setup Guide V7

## Status

Authoritative project setup guide.

## Purpose

This guide defines how a repository is structured as a documentation-first, AI-assisted engineering system without forcing every project into the same documentation weight.

Version 7 replaces the broad V5 default model with:

- a project maturity model;
- task-mode separation;
- narrow default agent reading;
- optional rather than default task-best-practice and issue-template layers;
- explicit separation between planning, implementation, documentation synchronization, and release readiness;
- support for artifact-first and agentic authoring repositories;
- first-class human review gates where automation cannot fully decide correctness.

The guide remains stack-independent. Concrete .NET, TypeScript, build, test, validation, artifact, release, and tooling commands are defined by the Engineering Guide V6 and by repository-local `docs/ENGINEERING.md`.

---

## 1. Core doctrine

A repository should hold **project truth**, not the whole work methodology.

The default model is:

```text
Specs define what must be true.
Architecture and decisions define why the system is shaped that way.
Milestones define what to build next.
Engineering defines how to build and validate.
Public docs define what users need to know.
Scenarios define executable behavioral validation when ordinary tests are insufficient.
Content docs define structured non-code source truth when the project has authored content.
Artifact docs define generated evidence and reports.
Human review docs define acceptance gates that automation cannot fully close.
AGENTS.md routes agents to the minimum relevant authority.
External setup/engineering guides teach the general method.
```

Avoid turning every repository into a documentation governance system.

The repository should not force implementation agents to act as planners, architects, release engineers, documentation maintainers, and CI systems at the same time.

---

## 2. Project maturity model

Repository structure should grow with maturity.

| Mode | Purpose | Typical active layers |
|---|---|---|
| Exploration | Explore ideas and uncertainty. | `README.md`, research notes, minimal `AGENTS.md` |
| Design-ready | Capture durable behavior and design. | terminology, specs, architecture, decisions |
| Implementation-ready | Execute scoped work. | milestones, relevant specs, engineering commands |
| PR-ready | Integrate safely. | focused validation, standard check, CI workflows |
| Release-ready | Publish or ship supported artifacts. | public docs, release notes, package/API checks, release gate |
| Published/public | Maintain external compatibility and user docs. | public documentation, versioning, diagnostics, API baselines |
| Artifact-first | Validate generated or authored artifacts. | scenarios, content docs, artifact reports, human review gates |

A repository may be in multiple modes for different areas. For example, a library may be release-ready while an experimental subsystem remains design-ready.

---

## 3. Repository profiles

Profiles select optional documentation and engineering capabilities.

| Profile | Use when | Common layers |
|---|---|---|
| Library/package | The repository publishes reusable libraries or packages. | public docs, samples, public API, release readiness |
| Runtime/tool | The repository builds a CLI, runtime, compiler, analyzer, or framework tool. | product CLI contract, diagnostics, artifact reports |
| UI/component | The repository exposes a UI component or browser/runtime surface. | public docs, samples, scenario or E2E validation |
| Artifact-first/agentic authoring | The repository produces, validates, or modifies structured artifacts or authored content. | content docs, scenarios, artifact reports, human review |
| Game/simulation | The repository contains deterministic runtime behavior, scenario execution, save/load, assets, or visual previews. | scenarios, runtime observability, content validation, human review |

Profiles are additive. They do not imply that all possible docs must exist immediately.

---

## 4. Task modes

Tasks determine what an agent should read and validate.

| Task mode | Purpose | Default reading | Default output |
|---|---|---|---|
| Planning | Explore what should be built. | Broad project docs, research, external guides | notes, candidate milestones |
| Design/spec | Normalize durable truth. | terminology, specs, architecture, decisions | updated specs/docs |
| Milestone authoring | Convert design into executable work. | specs, architecture, decisions, engineering validation tiers | milestone focus areas |
| Implementation | Change code/content in a narrow scope. | milestone section, relevant specs, engineering commands | code/content + direct docs only |
| Scenario/content validation | Validate runtime/content behavior. | relevant scenario/content/artifact docs | scenario artifacts/reports |
| Documentation synchronization | Normalize docs after implementation. | broad relevant docs and external guides | doc updates and cross-link cleanup |
| Workflow/CI | Change automation. | engineering docs, workflow docs, command contract | scripts/workflows/docs |
| Release | Prepare public release. | public docs, release docs, package/API/versioning docs | release-ready repository |
| Human review | Evaluate artifacts automation cannot judge. | review docs, artifact reports, relevant scenario/content docs | approval/rejection record |

Implementation tasks should not perform broad documentation normalization unless explicitly requested.

---

## 5. Phase-separated development lifecycle

Recommended lifecycle:

```text
Ideation / planning
  -> design normalization
  -> milestone authoring with focus areas
  -> implementation
  -> focused local validation
  -> PR integration validation
  -> documentation synchronization pass
  -> release/public-doc readiness pass
```

### 5.1 Ideation / planning

Purpose: decide what should exist and explore uncertainty.

Outputs may include:

- research notes;
- planning notes;
- candidate architecture options;
- candidate milestone structure.

Broad reading is acceptable in this phase.

### 5.2 Design normalization

Purpose: move decisions from chat, research, or planning into durable authority.

Outputs may include:

- updated terminology;
- updated specs;
- updated architecture docs;
- updated decision records;
- updated scenario/content/review docs where applicable.

Implementation tasks should not be expected to normalize unclear design during ordinary coding.

### 5.3 Milestone authoring

Purpose: turn design authority into executable work.

Milestones should reference specs and define focus areas. They should not duplicate entire specs.

### 5.4 Implementation

Purpose: complete a narrow focus area.

Implementation should update only directly affected docs when code/content would otherwise contradict authoritative behavior or public API.

### 5.5 PR integration

Purpose: repository-wide validation through CI/workflows.

Implementation agents do not need to reproduce every CI check locally unless the task requires it.

### 5.6 Documentation synchronization

Purpose: normalize documents after implementation has stabilized.

This is a separate task mode. It may read broadly and may use the external setup and engineering guides.

### 5.7 Release readiness

Purpose: prove public artifacts are ready to ship.

This is separate from ordinary implementation.

---

## 6. Documentation layers

### 6.1 Default layers

| Layer | Responsibility |
|---|---|
| `README.md` | First-contact user and contributor entry point. |
| `AGENTS.md` | Concise agent routing and task-mode rules. |
| `docs/TERMINOLOGY.md` | Canonical vocabulary. |
| `docs/SPECS.md` | Index for behavioral truth and invariants. |
| `docs/ENGINEERING.md` | Index for build, validation, command contracts, tooling, and engineering policy. |
| `docs/MILESTONES.md` | Index for planned implementation phases when milestone-driven work is used. |

### 6.2 Conditional layers

| Layer | Apply when |
|---|---|
| `docs/ARCHITECTURE.md` | Structural design is non-trivial or subsystem boundaries matter. |
| `docs/DECISIONS.md` | Durable design rationale must be preserved. |
| `docs/PUBLIC-DOCS.md` | The repository has external consumers or release-oriented public docs. |
| `public-docs/` | User-facing documentation is active or release preparation has started. |
| `docs/WORKFLOWS.md` | CI/release/workflow behavior is non-trivial and needs documentation. |
| `docs/SCENARIOS.md` | Runtime or product behavior is validated through deterministic scenarios. |
| `docs/CONTENT.md` | Non-code project data is a first-class source of truth. |
| `docs/ARTIFACTS.md` | Validation produces reports, traces, previews, overlays, packages, generated docs, or other evidence. |
| `docs/HUMAN-REVIEW.md` | Human acceptance is required for semantic, visual, UX, gameplay, safety, or other non-fully-automatable judgments. |
| `docs/RESEARCH.md` | Non-authoritative research needs to be retained. |
| `docs/TBPS.md` | Only for process-heavy repositories where recurring task methodology must be versioned inside the repo. |
| `.github/ISSUE_TEMPLATE/` | Only when GitHub issues are an active work intake mechanism. |

---

## 7. Recommended repository structure

### 7.1 Minimal implementation-ready repository

```text
/
├─ README.md
├─ AGENTS.md
├─ docs/
│  ├─ TERMINOLOGY.md
│  ├─ SPECS.md
│  ├─ ENGINEERING.md
│  ├─ specs/
│  └─ engineering/
├─ eng/
├─ src/
└─ tests/
```

### 7.2 Behavior-rich repository

```text
/
├─ README.md
├─ AGENTS.md
├─ docs/
│  ├─ TERMINOLOGY.md
│  ├─ SPECS.md
│  ├─ ARCHITECTURE.md
│  ├─ DECISIONS.md
│  ├─ ENGINEERING.md
│  ├─ MILESTONES.md
│  ├─ specs/
│  ├─ architecture/
│  ├─ decisions/
│  ├─ engineering/
│  └─ milestones/
├─ eng/
├─ src/
└─ tests/
```

### 7.3 Public package/tool repository

```text
/
├─ README.md
├─ AGENTS.md
├─ docs/
│  ├─ TERMINOLOGY.md
│  ├─ SPECS.md
│  ├─ ENGINEERING.md
│  ├─ PUBLIC-DOCS.md
│  ├─ specs/
│  └─ engineering/
├─ public-docs/
│  ├─ getting-started.md
│  ├─ installation.md
│  ├─ concepts.md
│  ├─ packages.md
│  ├─ samples.md
│  ├─ diagnostics.md
│  ├─ versioning.md
│  └─ release-notes.md
├─ eng/
├─ src/
├─ tests/
└─ samples/
```

### 7.4 Artifact-first / agentic authoring repository

```text
/
├─ README.md
├─ AGENTS.md
├─ docs/
│  ├─ TERMINOLOGY.md
│  ├─ SPECS.md
│  ├─ SCENARIOS.md
│  ├─ CONTENT.md
│  ├─ ARTIFACTS.md
│  ├─ HUMAN-REVIEW.md
│  ├─ ENGINEERING.md
│  ├─ MILESTONES.md
│  ├─ specs/
│  ├─ scenarios/
│  ├─ content/
│  ├─ artifacts/
│  ├─ review/
│  ├─ engineering/
│  └─ milestones/
├─ content-or-project-data/
├─ artifacts/
├─ eng/
├─ src/
└─ tests/
```

The concrete project-data folder should be named by the domain, for example `game/`, `schemas/`, `models/`, `catalog/`, or `fixtures/`.

---

## 8. README rule

Only the root-level `README.md` is allowed by default.

Do not create additional `README.md` files under:

```text
docs/**
public-docs/**
eng/**
samples/**
tools/**
site/**
```

Use named Markdown files instead.

Rationale:

- one global README avoids conflicting entry points;
- named docs are easier for agents to reference;
- folder-local README files tend to become stale duplicates.

Repositories may explicitly opt out only when external tooling requires local README files.

---

## 9. Index document convention

A documentation folder under `docs/` may have one index document named:

```text
docs/<FOLDER>.md
```

where `<FOLDER>` is the folder name written in uppercase.

Examples:

```text
docs/SPECS.md indexes docs/specs/
docs/ENGINEERING.md indexes docs/engineering/
docs/SCENARIOS.md indexes docs/scenarios/
docs/CONTENT.md indexes docs/content/
docs/ARTIFACTS.md indexes docs/artifacts/
docs/HUMAN-REVIEW.md indexes docs/review/
```

Index documents should:

- define the purpose of the area;
- state when the area applies;
- list available documents;
- identify authority boundaries;
- avoid duplicating the documents they index.

---

## 10. Documentation authority

Use authority sections only where they clarify overlap.

Do not require long authority boilerplate in every document.

Recommended format:

```md
## Authority

This document is authoritative for:
- <area>

This document is not authoritative for:
- <excluded area>
```

Authority sections are especially useful for:

- specs;
- public docs;
- command contracts;
- scenario contracts;
- generated artifact contracts;
- human review gates;
- release readiness.

---

## 11. Document synchronization

Avoid broad document contracts that force every implementation task to update many files.

Use two documentation categories instead.

### 11.1 Direct documentation update

Required during implementation when a change would otherwise contradict:

- an authoritative spec;
- public API contract;
- diagnostic contract;
- command contract;
- scenario contract;
- content schema;
- human review gate.

### 11.2 Deferred documentation synchronization

Performed as a separate task after implementation stabilizes.

May update:

- indexes;
- public docs;
- examples;
- cross-links;
- release notes;
- README;
- terminology cleanup;
- milestone status;
- architecture/decision cleanup.

Milestones should declare whether a deferred documentation pass is expected.

---

## 12. `AGENTS.md`

`AGENTS.md` is a routing accelerator, not a documentation index.

Recommended template:

```md
# Agent Instructions

## Default implementation path

Read:
- README.md
- docs/ENGINEERING.md
- the relevant issue, task, or milestone focus area
- relevant specs under docs/specs/

Use canonical `eng/` commands only.

For narrow implementation work:
- use the validation tier named by the milestone or task;
- do not perform broad documentation synchronization unless explicitly requested;
- update directly affected specs/docs only when behavior, public API, diagnostics, scenarios, content schemas, or command contracts change.

## Conditional reading

Read docs/PUBLIC-DOCS.md and public-docs/ only when changing public API, package behavior, diagnostics, samples, release behavior, or user-facing documentation.

Read docs/ARCHITECTURE.md and docs/decisions/ only when changing subsystem boundaries or durable design choices.

Read docs/WORKFLOWS.md only when changing CI, release, or workflow behavior.

Read docs/SCENARIOS.md only when changing scenario definitions, scenario runner behavior, or scenario validation outputs.

Read docs/CONTENT.md only when changing structured project/content data rules.

Read docs/ARTIFACTS.md only when changing generated reports, traces, previews, overlays, or artifact schemas.

Read docs/HUMAN-REVIEW.md only when changing review-gated behavior, generated semantic content, visual/UX acceptance, or review artifacts.

Do not treat docs/research/project-setup-guide-*.md or docs/research/engineering-guide-*.md as operational authority.
```

---

## 13. Milestones

Milestones define planned implementation phases.

They should be implementation-heavy but not documentation-heavy.

A milestone should include:

```md
# Milestone <ID> — <Title>

## Goal

## Scope

## Non-goals

## Required authority

- docs/specs/<relevant>.md
- docs/architecture/<relevant>.md when needed
- docs/scenarios/<relevant>.md when needed
- docs/content/<relevant>.md when needed

## Focus areas

### Focus Area A — <name>

#### Goal

#### Scope

#### Likely files or areas

#### Validation tier

#### Direct documentation impact

#### Deferred documentation impact
```

Milestones must not:

- copy entire specs after specs exist;
- reference external setup/engineering guides as default implementation reading;
- require all documentation to be read for narrow implementation;
- require release validation unless the milestone is release-oriented.

---

## 14. Specs

Specs remain internal behavioral authority.

Rules:

- specs define what must be true;
- implementation should conform to specs;
- public docs explain supported usage, not internal truth;
- milestones sequence work but do not define permanent behavior;
- if implementation changes durable behavior, update the relevant spec directly.

---

## 15. Public documentation

Public documentation is conditional by maturity.

### 15.1 Public-doc states

| State | Meaning |
|---|---|
| Not active | No external consumer-facing docs are maintained yet. |
| Preview | Public docs exist for concepts or future consumers, but are not release authority. |
| Active | Public docs are expected to reflect supported behavior. |
| Release-ready | Public docs are part of release validation. |

### 15.2 Public documentation structure

For public package/tool repositories:

```text
public-docs/
├─ getting-started.md
├─ installation.md
├─ concepts.md
├─ packages.md
├─ samples.md
├─ diagnostics.md
├─ versioning.md
├─ release-notes.md
├─ guides/
├─ api/
├─ diagnostics/
├─ nuget/
├─ samples/
└─ website/
```

Do not apply this full structure before the repository needs it.

---

## 16. Scenarios

Scenarios are first-class for repositories where behavior must be validated by deterministic product/runtime execution.

Use:

```text
docs/SCENARIOS.md
docs/scenarios/
```

A scenario document should define:

```text
id
purpose
category
initial state
inputs
random seed policy
expected events
expected assertions
expected artifacts
human review requirements
performance budget if applicable
debug-mode applicability
packaged-mode applicability
```

Scenario categories may include:

```text
smoke
behavior
runtime
UI
asset import
content validation
save/load
performance
soak
regression
```

Scenarios differ from ordinary tests because they produce structured evidence and may require artifact review.

---

## 17. Content

Use content docs when non-code project data is a first-class source of truth.

Use:

```text
docs/CONTENT.md
docs/content/
```

Content docs may define:

- stable ID rules;
- schema rules;
- source/generated separation;
- provenance;
- dependency tracking;
- taxonomy and semantic metadata;
- validation rules;
- review gates;
- migration rules.

Source content should be:

```text
schema-validated
diff-friendly
merge-friendly
round-trippable when needed
addressable by stable ID
inspectable by agents
reviewable by humans
```

---

## 18. Artifacts

Use artifact docs when validation or product commands produce evidence.

Use:

```text
docs/ARTIFACTS.md
docs/artifacts/
```

Artifact docs may define:

- artifact folder layout;
- schema for `result.json`, diagnostics, traces, reports, previews, overlays;
- artifact retention policy;
- PR upload rules;
- failure evidence requirements;
- human review package format.

Rule:

```text
A failed scenario, content validation, or generated-artifact validation should produce enough evidence for an agent or human to diagnose it without guessing.
```

---

## 19. Human review

Use human review docs when automation can produce evidence but cannot fully decide correctness.

Use:

```text
docs/HUMAN-REVIEW.md
docs/review/
```

Human review may be required for:

- visual quality;
- UX behavior;
- gameplay feel;
- generated semantic labels;
- physical/collision semantics;
- progression-critical content;
- generated documentation tone;
- model-produced classifications;
- safety-sensitive decisions.

Human review docs should define:

```text
review classes
what requires review
what can be auto-approved
required artifacts
approval record format
failure/rejection handling
whether unreviewed artifacts block PR or release
```

Human review is a validation tier, not an informal comment.

---

## 20. TBPs

Task Best Practices are no longer part of the default repository structure.

Use TBPs only when:

- the repository is process-heavy;
- many contributors or agents repeatedly perform the same complex task;
- the methodology must be versioned with the repository;
- the knowledge does not belong in specs, engineering docs, milestones, workflows, or external guides.

Most repositories should not have:

```text
docs/TBPS.md
docs/tbps/
```

Former TBP responsibilities should usually move to:

| Former TBP purpose | Preferred location |
|---|---|
| Feature implementation method | milestone focus area |
| Bug investigation method | task description or engineering docs |
| Spec creation method | external setup guide or documentation pass |
| Terminology update method | external setup guide or docs/TERMINOLOGY.md notes |
| Public docs update method | documentation synchronization task |
| Release preparation | engineering release-readiness docs |
| Workflow changes | engineering/workflow docs |

---

## 21. Issue templates

Issue templates are optional.

Do not use issue templates as the primary carrier of implementation methodology.

Prefer:

- milestones;
- focus areas;
- specs;
- scenario definitions;
- content/review docs;
- explicit Codex/GitHub tasks.

If issue templates are used, keep them minimal:

```md
# Implementation Task

Milestone:
Focus area:
Relevant specs:
Expected validation tier:
Notes:
```

Delete issue templates if implementation work is transported through milestones and specs.

---

## 22. Research docs

Research docs are non-authoritative.

External setup and engineering guide copies may be stored under:

```text
docs/research/
```

But agents must not treat them as operational authority by default.

Extract project-specific rules into active docs.

---

## 23. Migration from V5/V6-style repositories

Recommended migration:

1. Classify the repository maturity and profiles.
2. Simplify `AGENTS.md` to task-mode routing.
3. Remove setup/engineering guide copies from default required reading.
4. Remove `docs/TBPS.md` and `docs/tbps/` unless process-heavy status is justified.
5. Delete `.github/ISSUE_TEMPLATE/` unless issues are an active work intake mechanism.
6. Merge guardrails into `docs/ENGINEERING.md` unless a separate guardrail layer is justified.
7. Replace broad document contracts with direct/deferred documentation impact rules.
8. Split milestones into focus areas with validation tiers.
9. Add `docs/SCENARIOS.md`, `docs/CONTENT.md`, `docs/ARTIFACTS.md`, or `docs/HUMAN-REVIEW.md` only when the repository profile needs them.
10. Move broad documentation cleanup into explicit documentation synchronization tasks.

---

## 24. Final V7 model

The V7 repository model is:

```text
README.md
  first-contact user/contributor entry point

AGENTS.md
  concise task-mode routing

docs/TERMINOLOGY.md
  canonical vocabulary

docs/SPECS.md
  behavioral truth

docs/ENGINEERING.md
  command contracts, validation tiers, tooling

docs/MILESTONES.md
  planned implementation focus areas

docs/ARCHITECTURE.md and docs/DECISIONS.md
  conditional structural design and rationale

docs/PUBLIC-DOCS.md and public-docs/
  conditional public documentation

docs/SCENARIOS.md
  conditional executable behavior validation

docs/CONTENT.md
  conditional structured project data authority

docs/ARTIFACTS.md
  conditional generated evidence/report authority

docs/HUMAN-REVIEW.md
  conditional human acceptance gates

docs/TBPS.md and issue templates
  optional only for process-heavy repositories
```

The key principle:

```text
Use the minimum authoritative context required for the task.
Grow structure by maturity and profile, not by default.
```
