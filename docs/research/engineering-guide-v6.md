# Engineering Guide V6

## Status

Authoritative engineering guide for the default .NET-oriented repository profile.

## Purpose

This guide defines an opinionated, AI-agent-friendly engineering setup for professional repositories while avoiding a single oversized command and documentation model.

Version 6 replaces the V4/V5 style universal validation path with:

- validation tiers;
- focused implementation validation;
- explicit PR and release gates;
- separation between implementation and documentation synchronization;
- optional public documentation and release readiness;
- generic artifact-first building blocks;
- first-class scenario validation and human review gates for repositories that need them.

The default stack remains:

- .NET 10 or newer where selected by the repository;
- Microsoft Testing Platform (MTP);
- TUnit;
- BenchmarkDotNet where useful;
- Bun and Biome where TypeScript/JavaScript tooling is used.

Optional modules cover:

- Blazor;
- Playwright;
- TypeScript runtime/browser tooling;
- NuGet packaging;
- samples;
- GitHub Copilot;
- OpenAI Codex;
- GitHub Pages;
- public documentation;
- release readiness;
- product CLI/headless tooling;
- structured project data;
- scenario validation;
- generated artifacts;
- human review gates;
- dual-mode runtime validation.

---

## 1. Relationship to Project Setup Guide V7

Project Setup Guide V7 defines repository knowledge organization, maturity modes, documentation layers, and task-mode routing.

Engineering Guide V6 defines the concrete engineering substrate:

- command contracts;
- validation tiers;
- build/test/format/package/release commands;
- optional engineering building blocks;
- artifact-first validation patterns;
- scenario and human-review support.

```text
Project Setup Guide V7 tells the repository how to organize truth.
Engineering Guide V6 tells the repository how to build, validate, package, and produce evidence.
```

---

## 2. Core principles

### 2.1 Agent-executable over descriptive

Instructions must be executable or directly checkable.

Prefer:

```text
Run ./eng/test-project.sh tests/unit/Example.Tests.Unit and ensure it exits with code 0.
```

Avoid:

```text
Make sure the project looks clean.
```

### 2.2 Canonical commands, not guessed commands

Agents must use repository-provided commands instead of inventing validation flows.

The repository should provide small canonical commands as well as broad gates.

### 2.3 Focused local validation, broader CI validation

Narrow implementation work should not always run the entire repository gate locally.

Use:

```text
focused validation for implementation;
standard validation for local confidence;
PR workflows for integration;
release validation for publishing;
human review gates for non-automatable acceptance.
```

### 2.4 Building blocks, not one giant template

Repositories start small and add capabilities by applying building blocks.

A block defines:

- purpose;
- when to apply;
- files to create or modify;
- commands to expose;
- validation command;
- done criteria.

### 2.5 Optional means absent by default

Do not create Blazor, Playwright, TypeScript, NuGet, public-docs, release, scenario, content, artifact, or human-review structure unless the repository needs it.

### 2.6 Product commands and engineering commands are different

`eng/` commands are the repository engineering API.

A product CLI is the product/runtime API.

Both may exist. Agents should use both through documented commands.

---

## 3. Validation tiers

Every non-trivial repository should define validation tiers in `docs/ENGINEERING.md` or `docs/engineering/command-contract.md`.

| Tier | Name | Purpose | Typical commands |
|---:|---|---|---|
| 0 | Edit sanity | Cheap checks for trivial or doc-only edits. | format touched files, schema check touched files |
| 1 | Focused implementation | Validate affected code/content only. | build affected project, run affected tests |
| 2 | Standard local gate | Normal pre-completion local confidence. | `./eng/check.sh` |
| 3 | PR integration | Clean repository validation in CI. | GitHub Actions calling `eng/` scripts |
| 4 | Release gate | Validate public artifacts before publish. | `./eng/release-check.sh <version>` |
| 5 | Artifact/human review | Validate generated evidence and review-gated outputs. | scenario artifacts, review packs, approval record |

Milestones and tasks should name the expected validation tier.

---

## 4. Recommended command contract

### 4.1 Required base commands

```text
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/format.sh
./eng/check.sh
```

### 4.2 Recommended focused commands

```text
./eng/test-project.sh <project-or-path>
./eng/test-filter.sh <filter>
./eng/check-affected.sh
./eng/schema-validate.sh <path-or-scope>
```

Repositories may omit commands that are not meaningful, but narrow implementation tasks should have a canonical path.

### 4.3 Optional package/release commands

```text
./eng/package.sh <version>
./eng/publish.sh <version>
./eng/package-smoke.sh <version>
./eng/public-api.sh
./eng/public-docs.sh
./eng/release-check.sh <version>
```

### 4.4 Optional frontend/UI commands

```text
./eng/frontend-check.sh
./eng/frontend-format.sh
./eng/e2e.sh
./eng/site-build.sh
```

### 4.5 Optional artifact-first commands

```text
./eng/product-validate.sh
./eng/cli-smoke.sh
./eng/scenario.sh <scenario-id>
./eng/scenario-smoke.sh
./eng/scenario-packaged.sh <scenario-id>
./eng/content-validate.sh <scope>
./eng/artifacts-validate.sh <artifact-path>
./eng/review-pack.sh <run-id-or-artifact-path>
```

---

## 5. Default command behavior

### 5.1 `eng/test.sh`

Runs fast tests only.

It should exclude:

- slow tests;
- E2E tests;
- package smoke tests;
- long-running integration tests;
- benchmarks;
- release-only validation;
- human review gates.

### 5.2 `eng/check.sh`

Standard local gate.

Typical flow:

```sh
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/format.sh --verify
```

It should stay safe for normal local development and AI-agent validation.

It should not perform release validation.

### 5.3 `eng/release-check.sh <version>`

Release gate.

May include:

- standard check;
- Release build;
- package creation;
- package smoke tests;
- public API validation;
- samples validation;
- public documentation validation;
- release notes validation;
- packaged-mode scenario validation when applicable.

### 5.4 `eng/check-affected.sh`

Focused validation for narrow implementation work.

It may inspect changed files or accept explicit scopes. It should fail clearly when affected-scope detection is unavailable.

### 5.5 Artifact-producing commands

Scenario, content, preview, and review commands should produce machine-readable artifacts under a documented folder.

---

## 6. Test and validation taxonomy

### 6.1 Base categories

| Category | Default local run | Description |
|---|---:|---|
| Unit | Yes | Fast, isolated tests. |
| Integration | Usually no or selected | Subsystems or external infrastructure substitutes. |
| PackageSmoke | No | Consume packed packages as a real consumer. |
| E2E | No | Browser/system tests. |
| Slow | No | Expensive tests not suitable for normal agent iterations. |
| Benchmark | Never via test command | BenchmarkDotNet or equivalent performance measurement. |

### 6.2 Artifact-first categories

| Category | Default local run | Description |
|---|---:|---|
| Scenario | Selected | Deterministic product/runtime execution that produces artifacts. |
| ContentValidation | Selected | Static validation of structured non-code project data. |
| GeneratedArtifactValidation | Selected | Validation of generated files, reports, previews, or derived outputs. |
| ReplayValidation | No or selected | Re-run deterministic event/input/save traces. |
| PackagedRuntimeValidation | Release-oriented | Run scenarios against optimized or packaged representation. |
| VisualRegression | Selected or review-gated | Compare generated visual artifacts or overlays. |
| HumanReview | Explicit | Human acceptance of semantic, visual, UX, gameplay, or other non-automatable outputs. |

---

## 7. Required repository layout

### 7.1 Base layout

```text
/
├─ .config/
├─ .github/
│  └─ workflows/
├─ artifacts/
├─ docs/
│  ├─ ENGINEERING.md
│  └─ engineering/
├─ eng/
│  ├─ restore.sh
│  ├─ build.sh
│  ├─ test.sh
│  ├─ format.sh
│  ├─ check.sh
│  └─ common.sh
├─ src/
├─ tests/
├─ .editorconfig
├─ Directory.Build.props
├─ Directory.Packages.props
├─ global.json
├─ AGENTS.md
└─ README.md
```

### 7.2 Optional artifact-first layout

```text
/
├─ artifacts/
│  ├─ scenarios/
│  ├─ reports/
│  ├─ previews/
│  └─ review/
├─ docs/
│  ├─ SCENARIOS.md
│  ├─ CONTENT.md
│  ├─ ARTIFACTS.md
│  ├─ HUMAN-REVIEW.md
│  ├─ scenarios/
│  ├─ content/
│  ├─ artifacts/
│  └─ review/
├─ content-or-project-data/
└─ tools/
```

The domain should choose the concrete source-data folder name.

---

## 8. `eng/` folder design

`eng/` is the canonical engineering entry point for humans, agents, and CI.

Rules:

- top-level scripts are the public engineering API;
- nested scripts are implementation details;
- CI should call `eng/` scripts instead of embedding build logic;
- scripts should fail clearly;
- expensive commands must be explicit;
- secrets must not be required for normal validation.

Prefer:

```yaml
run: ./eng/check.sh
```

Avoid:

```yaml
run: |
  dotnet restore
  dotnet build
  dotnet test
```

---

## 9. Building block overview

| Block | Name | Required | Purpose |
|---|---|---:|---|
| BB00 | Repository Base | Yes | Common repository skeleton and command contract. |
| BB01 | .NET Solution | Yes for .NET repos | Solution, source project, test project structure. |
| BB02 | Shared Build Configuration | Yes for .NET repos | SDK, central package management, build props. |
| BB03 | EditorConfig and Style | Yes | Formatting and analyzer rules. |
| BB04 | MTP + TUnit Unit Tests | Yes for .NET repos | Fast unit testing foundation. |
| BB05 | Test Guardrails | Yes | Fast/slow/integration/e2e/package-smoke separation. |
| BB06 | BenchmarkDotNet | Recommended where performance matters | Dedicated benchmark project. |
| BB07 | GitHub Actions CI | Recommended | Build/test/check automation. |
| BB08 | Agent Instructions | Yes | Repository-local AI-agent operating instructions. |
| BB09 | Bun + Biome | Optional | TypeScript/JavaScript tooling. |
| BB10 | Blazor Module | Optional | Blazor application/component project. |
| BB11 | Playwright E2E Module | Optional | Browser/system tests. |
| BB12 | TypeScript Runtime Tools | Optional | Self-authored TypeScript tools/runtime code. |
| BB13 | Documentation Skeleton | Yes | Minimal docs for maintainability. |
| BB14 | NuGet Packaging | Required for NuGet packages | Package generation and publishing conventions. |
| BB15 | Samples | Recommended for public packages/tools | Runnable examples. |
| BB16 | GitHub Copilot | Optional | Copilot instructions. |
| BB17 | OpenAI Codex | Optional | Codex-friendly instructions and validation. |
| BB18 | GitHub Pages Website | Optional | Static website publishing. |
| BB19 | Public Documentation | Required for release-ready public artifacts | Consumer-facing docs and validation. |
| BB20 | Release Readiness | Required for public release | Package/API/docs/release gate. |
| BB21 | Headless Product CLI | Optional | Product/runtime command surface for agents and CI. |
| BB22 | Structured Project Data | Optional | Non-code source-of-truth data with schemas and stable IDs. |
| BB23 | Scenario-Based Validation | Optional | Deterministic product/runtime scenarios with artifacts. |
| BB24 | Artifact Evidence and Reports | Optional | Standard reports, traces, previews, and diagnostics artifacts. |
| BB25 | Generated/Derived Artifact Pipeline | Optional | Provenance, rebuilds, generated outputs. |
| BB26 | Human Review Gates | Optional | Review workflow for non-fully-automatable correctness. |
| BB27 | Dual-Mode Runtime Validation | Optional | Validate debug/development and packaged/release representations. |

---

## 10. BB00 — Repository Base

### Purpose

Create the repository skeleton and canonical engineering scripts.

### Apply when

Always.

### Files to create

```text
.gitignore
README.md
AGENTS.md
eng/restore.sh
eng/build.sh
eng/test.sh
eng/format.sh
eng/check.sh
artifacts/.gitkeep
docs/ENGINEERING.md
docs/engineering/command-contract.md
```

### Required conventions

- No non-root README files by default.
- Agents use `eng/` scripts.
- `artifacts/` is for generated outputs and is ignored except for placeholders or intentionally committed baselines.
- `README.md` links to canonical commands and docs.

### Validation

```sh
./eng/check.sh
```

### Done criteria

- Base files exist.
- Scripts are executable.
- Commands are documented.
- No non-root README files exist unless explicitly justified.

---

## 11. BB01 — .NET Solution

### Purpose

Create the .NET solution and project structure.

### Apply when

Apply for .NET repositories.

### Files/projects to create

```text
Example.Project.slnx
src/Example.Project/Example.Project.csproj
tests/unit/Example.Project.Tests.Unit/Example.Project.Tests.Unit.csproj
tests/integration/Example.Project.Tests.Integration/Example.Project.Tests.Integration.csproj
```

### Required conventions

- Production projects live under `src/`.
- Unit tests live under `tests/unit/`.
- Integration tests live under `tests/integration/`.
- Package smoke tests live under `tests/package-smoke/` when applied.

### Validation

```sh
./eng/build.sh
```

---

## 12. BB02 — Shared Build Configuration

### Purpose

Centralize SDK, analyzer, build, and package configuration.

### Files to create

```text
global.json
Directory.Build.props
Directory.Packages.props
.config/dotnet-tools.json
```

### Required conventions

- SDK is pinned or explicitly selected.
- Package versions are centralized.
- Warnings-as-errors policy is defined.
- Public package projects generate XML docs where applicable.

### Validation

```sh
./eng/restore.sh
./eng/build.sh
```

---

## 13. BB03 — EditorConfig and Style

### Purpose

Provide concrete formatting/style rules so agents do not infer style from examples.

### File to create

```text
.editorconfig
```

### Validation

```sh
./eng/format.sh --verify
```

---

## 14. BB04 — MTP + TUnit Unit Tests

### Purpose

Create the default .NET test foundation.

### Required conventions

- Unit tests are fast and deterministic.
- Unit tests do not use network, real database, browser automation, or sleeps.
- Test names describe observable behavior.
- Generated broad tests that assert implementation details should be avoided.

### Validation

```sh
./eng/test.sh
```

---

## 15. BB05 — Test Guardrails

### Purpose

Prevent slow, broad, or operationally expensive tests by default.

### Required rules

- `eng/test.sh` runs fast tests only.
- Slow, E2E, package smoke, scenario soak, benchmarks, and human review are excluded from normal test execution.
- Integration tests have explicit commands or filters.
- Benchmarks are never represented as tests.

### Validation

```sh
./eng/test.sh
```

---

## 16. BB06 — BenchmarkDotNet

### Purpose

Add performance measurement without polluting tests.

### Apply when

Apply for libraries, algorithms, runtimes, serialization, parsers, renderers, simulations, graph processing, or performance-sensitive code.

### Command

```sh
./eng/benchmark.sh
```

### Rule

Benchmarks are not part of `eng/test.sh` or ordinary implementation validation.

---

## 17. BB07 — GitHub Actions CI

### Purpose

Provide hosted validation for build, test, formatting, and optional checks.

### Required conventions

- CI uses `eng/` scripts.
- CI does not invent separate build logic.
- PR validation is broader than narrow local validation.
- CI uploads artifacts for scenario/content/artifact failures when applicable.

### Validation

CI passes on a clean checkout.

---

## 18. BB08 — Agent Instructions

### Purpose

Provide local operating rules for AI agents.

### Files

```text
AGENTS.md
.github/copilot-instructions.md when Copilot is used
```

### Required conventions

- `AGENTS.md` is concise.
- It routes by task mode.
- It does not require reading all docs by default.
- It identifies validation tiers.
- It marks expensive commands.
- It tells agents not to treat research guide copies as operational authority.

---

## 19. BB09 — Bun + Biome

### Purpose

Add JavaScript/TypeScript tooling with minimal moving parts.

### Apply when

Apply only when TypeScript/JavaScript is needed.

### Required conventions

- Use Bun, not npm.
- Use Biome, not ESLint/Prettier.
- Commit lockfile.
- Keep tooling scoped and optional.

### Validation

```sh
bun install --frozen-lockfile
bun run check
```

---

## 20. BB10 — Blazor Module

### Purpose

Add a Blazor application/component while keeping frontend tooling optional.

### Required conventions

- Blazor is primary UI framework when applied.
- JavaScript interop is small, typed, and isolated.
- Apply BB09 when TypeScript is used.
- Apply BB11 when browser tests are needed.

---

## 21. BB11 — Playwright E2E Module

### Purpose

Add explicit browser/system testing.

### Required conventions

- E2E tests are opt-in.
- E2E tests run through `eng/e2e.sh`.
- E2E tests are excluded from `eng/test.sh`.
- Keep E2E tests few and high-value.

---

## 22. BB12 — TypeScript Runtime Tools

### Purpose

Support self-authored TypeScript scripts or runtime utilities without adopting a full frontend stack.

### Apply when

Apply for graph processing, code generation, schema transformations, browser-adjacent tools, documentation tooling, or asset/report utilities.

### Required conventions

- Explicit inputs and outputs.
- Pure or mostly pure file boundaries.
- Heavy dependencies isolated behind adapters.

---

## 23. BB13 — Documentation Skeleton

### Purpose

Provide the minimum documentation needed for maintainable human and agent work.

### Files

```text
README.md
AGENTS.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
```

For behavior-rich repositories, add:

```text
docs/TERMINOLOGY.md
docs/SPECS.md
docs/specs/
```

### Rule

Do not create TBPs, issue templates, public docs, workflows, scenarios, content docs, or human review docs unless the repository profile needs them.

---

## 24. BB14 — NuGet Packaging

### Purpose

Provide standardized NuGet packaging and publishing.

### Apply when

Apply when the repository produces NuGet packages.

### Commands

```sh
./eng/package.sh <version>
./eng/publish.sh <version>
```

### Required conventions

- Publishing is explicit.
- Package metadata is centralized where practical.
- Public packages include source link and symbols when appropriate.
- Package README content comes from public docs when public docs are active.

---

## 25. BB15 — Samples

### Purpose

Add small runnable examples.

### Required conventions

- Samples are executable documentation.
- Samples remain small.
- Samples do not replace tests.
- Samples must not become a second application architecture.
- No local `samples/README.md` by default.

### Command

```sh
./eng/samples.sh
```

---

## 26. BB16 — GitHub Copilot

### Purpose

Add repository-specific Copilot guidance.

### Files

```text
.github/copilot-instructions.md
.github/instructions/ when path-specific rules reduce ambiguity
```

### Required conventions

- Instructions are short.
- They point to `AGENTS.md`.
- They do not duplicate setup or engineering guides.

---

## 27. BB17 — OpenAI Codex

### Purpose

Optimize the repository for Codex local/cloud workflows.

### Required conventions

- `AGENTS.md` is primary.
- Expensive commands are marked.
- Completion criteria are explicit.
- Cloud-safe and local-only workflows are distinguished.
- Focused validation is available for narrow work.

---

## 28. BB18 — GitHub Pages Website

### Purpose

Add static website publication.

### Required conventions

- Website source lives under `site/` or another documented source folder.
- Public docs remain source of user-facing content where applicable.
- Publishing is separate from normal validation.
- No `site/README.md` by default.

---

## 29. BB19 — Public Documentation

### Purpose

Add public user-facing documentation for packages, APIs, diagnostics, samples, release notes, and websites.

### Apply when

Apply when public documentation is active or release-ready.

### Files

```text
docs/PUBLIC-DOCS.md
public-docs/
eng/public-docs.sh
```

### Required conventions

- Public docs are user-first.
- Public docs do not duplicate internal specs verbatim.
- Public docs are not required before the repository maturity warrants them.
- Public docs may be preview, active, or release-ready.

---

## 30. BB20 — Release Readiness

### Purpose

Add release-oriented validation.

### Apply when

Apply when the repository publishes public artifacts.

### Commands

```sh
./eng/release-check.sh <version>
./eng/package-smoke.sh <version>
./eng/public-api.sh
./eng/public-docs.sh
```

### Rule

Release validation is not normal implementation validation.

---

## 31. BB21 — Headless Product CLI

### Purpose

Define a product/runtime command surface that agents, CI, and humans can use without a graphical editor or manual UI workflow.

### Apply when

Apply when the repository builds a runtime, compiler, engine, content processor, simulator, analyzer, generator, or authoring system that should be operated through commands.

### Examples

```text
game engine CLI
schema compiler CLI
asset processor CLI
simulation runner CLI
documentation generator CLI
migration tool CLI
```

### Files to create or modify

```text
docs/engineering/product-cli.md
docs/engineering/command-contract.md
eng/cli-smoke.sh
eng/product-validate.sh
```

### Required conventions

- Product commands are deterministic where practical.
- Commands support machine-readable output.
- Commands write artifacts to documented locations.
- Commands fail with structured diagnostics where practical.
- Product CLI commands are documented separately from `eng/` scripts.

### Validation

```sh
./eng/cli-smoke.sh
./eng/product-validate.sh
```

---

## 32. BB22 — Structured Project Data

### Purpose

Support repositories where source-of-truth data is not only code.

### Apply when

Apply when the repository contains authored data such as game content, schema catalogs, simulation definitions, workflow definitions, visual editor files, content packages, or domain fixtures.

### Files to create or modify

```text
docs/CONTENT.md
docs/content/stable-ids.md
docs/content/schema-validation.md
eng/content-validate.sh
```

### Required conventions

Structured project data should be:

```text
schema-validated
diff-friendly
merge-friendly
stable-ID-addressable
reviewable
round-trippable where needed
```

Generated data must be clearly separated from source data.

### Validation

```sh
./eng/content-validate.sh <scope>
```

---

## 33. BB23 — Scenario-Based Validation

### Purpose

Validate behavior by running deterministic scenarios that produce structured evidence.

### Apply when

Apply when ordinary unit/integration tests are insufficient to prove product behavior.

### Examples

```text
simulation scenarios
gameplay scenarios
compiler input/output scenarios
workflow execution scenarios
UI behavior scenarios
asset import scenarios
save/load scenarios
```

### Files to create or modify

```text
docs/SCENARIOS.md
docs/scenarios/
eng/scenario.sh
eng/scenario-smoke.sh
```

### Required conventions

Scenarios define:

```text
id
category
initial state
inputs
seed policy
expected events/assertions
artifact outputs
review requirements when applicable
```

### Validation

```sh
./eng/scenario.sh <scenario-id>
./eng/scenario-smoke.sh
```

---

## 34. BB24 — Artifact Evidence and Reports

### Purpose

Standardize generated evidence from validation, product commands, previews, diagnostics, and scenarios.

### Apply when

Apply when commands produce reports, traces, previews, overlays, screenshots, dumps, or diagnostics that agents/humans inspect.

### Files to create or modify

```text
docs/ARTIFACTS.md
docs/artifacts/report-contract.md
eng/artifacts-validate.sh
```

### Required conventions

Artifacts should be:

```text
machine-readable where possible
stable enough for comparison
linked to command/scenario IDs
stored under documented paths
uploaded by CI on failure or review-relevant runs
```

Typical artifacts:

```text
result.json
diagnostics.json
events.jsonl
trace logs
screenshots
preview images
overlays
metrics.json
review summaries
```

### Validation

```sh
./eng/artifacts-validate.sh <artifact-path>
```

---

## 35. BB25 — Generated/Derived Artifact Pipeline

### Purpose

Control files derived from source data, code generation, compilation, import, transformation, or packaging.

### Apply when

Apply when the repository generates code, metadata, binary resources, indexes, previews, docs, assets, or packages from source inputs.

### Files to create or modify

```text
docs/artifacts/generated-artifacts.md
docs/content/provenance.md when source data is involved
eng/rebuild-generated.sh
```

### Required conventions

- Generated files are marked.
- Source/provenance is tracked where useful.
- Manual edits to generated files are forbidden or controlled.
- Rebuild command exists.
- Affected rebuild strategy is documented when needed.
- Generated output is stable enough for review or excluded intentionally.

### Validation

```sh
./eng/rebuild-generated.sh --verify
```

---

## 36. BB26 — Human Review Gates

### Purpose

Define explicit review gates for outputs automation cannot fully judge.

### Apply when

Apply when correctness includes visual quality, semantic classification, UX behavior, gameplay feel, generated documentation tone, model-generated labels, safety-sensitive content, or other human judgment.

### Files to create or modify

```text
docs/HUMAN-REVIEW.md
docs/review/
eng/review-pack.sh
```

### Required conventions

Human review docs define:

```text
what requires review
what can be auto-approved
required artifacts
approval record format
whether unreviewed outputs block PR or release
```

Review gates should not be hidden in informal comments.

### Validation

```sh
./eng/review-pack.sh <artifact-path-or-run-id>
```

Done criteria:

- review artifacts are generated;
- high-impact unreviewed outputs can be detected;
- approval/rejection is recorded in a documented form.

---

## 37. BB27 — Dual-Mode Runtime Validation

### Purpose

Validate that debug/development and packaged/release representations preserve the same semantics.

### Apply when

Apply when a product has a rich development mode and optimized release mode.

### Examples

```text
game debug runtime vs packaged runtime
compiler debug IR vs optimized IR
JSON source schema vs generated binary tables
verbose tool mode vs release tool mode
```

### Required conventions

- Same semantic model.
- Same command/event/scenario semantics.
- Different representations are allowed.
- Release mode may remove diagnostics or debug metadata.
- Release-relevant scenarios run against both modes.

### Commands

```sh
./eng/scenario-packaged.sh <scenario-id>
./eng/release-check.sh <version>
```

---

## 38. Recommended setup sequences

### 38.1 Normal .NET library/service

```text
BB00 Repository Base
BB01 .NET Solution
BB02 Shared Build Configuration
BB03 EditorConfig and Style
BB04 MTP + TUnit Unit Tests
BB05 Test Guardrails
BB07 GitHub Actions CI
BB08 Agent Instructions
BB13 Documentation Skeleton
```

Add BB06 when performance matters.

### 38.2 Public NuGet package

Add:

```text
BB14 NuGet Packaging
BB15 Samples
BB19 Public Documentation when active/release-ready
BB20 Release Readiness before release
```

### 38.3 Blazor/browser component

Add as needed:

```text
BB09 Bun + Biome
BB10 Blazor Module
BB11 Playwright E2E Module
BB12 TypeScript Runtime Tools
```

### 38.4 Artifact-first / agentic authoring repository

Add as needed:

```text
BB21 Headless Product CLI
BB22 Structured Project Data
BB23 Scenario-Based Validation
BB24 Artifact Evidence and Reports
BB25 Generated/Derived Artifact Pipeline
BB26 Human Review Gates
BB27 Dual-Mode Runtime Validation
```

---

## 39. Agent repository creation workflow

An AI agent creating a new repository should:

1. Determine project maturity and profile.
2. Select applicable building blocks.
3. Create the base repository skeleton.
4. Add solution/tooling structure.
5. Add canonical `eng/` commands.
6. Add focused validation commands where useful.
7. Add optional public, release, scenario, content, artifact, or review blocks only when selected.
8. Create concise `AGENTS.md` with task-mode routing.
9. Avoid TBPs and issue templates unless explicitly selected.
10. Run the required validation tier.
11. Report selected blocks, commands created, and validation result.

Agents must not declare completion until the applicable validation command succeeds or the failure is reported with the exact failing command and a concise output summary.

---

## 40. Upgrade guide from V4/V5-style repositories

1. Add validation tiers to `docs/ENGINEERING.md`.
2. Add focused commands where broad `eng/check.sh` is too expensive.
3. Simplify `AGENTS.md` to task-mode routing.
4. Remove setup/engineering guide copies from default required reading.
5. Remove `docs/TBPS.md` and `docs/tbps/` unless process-heavy status is justified.
6. Delete issue templates unless issues are an active work intake path.
7. Merge guardrails into engineering docs unless a separate guardrail layer is still justified.
8. Make public docs conditional by maturity: preview, active, release-ready.
9. Split implementation tasks from documentation synchronization tasks.
10. Add artifact-first blocks only where the product needs them.
11. Ensure CI uses `eng/` commands and uploads relevant artifacts.
12. Keep `eng/release-check.sh` release-oriented, not normal implementation-oriented.

---

## 41. Final V6 model

Engineering Guide V6 keeps the strong V4 ideas:

- canonical `eng/` scripts;
- explicit command contracts;
- building blocks;
- .NET/TUnit/MTP support;
- BenchmarkDotNet when useful;
- Bun/Biome when TypeScript is needed;
- optional Blazor and Playwright;
- package, sample, public API, public docs, and release readiness support.

It changes the operating model:

```text
Focused implementation validation is allowed.
PR workflows provide integration validation.
Release checks are explicit.
Documentation synchronization is a separate task.
Artifact-first repositories get scenarios, reports, generated-artifact rules, and human review gates.
```

The final rule:

```text
Do not make every task run every command or read every document.
Expose the smallest canonical command and smallest authoritative reading set that can validate the task honestly.
```
