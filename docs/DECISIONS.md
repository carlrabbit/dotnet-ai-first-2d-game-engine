# Decisions

## Authority

This document indexes durable project decisions.

## Current decisions

| Decision | Status | Topic |
|---|---|---|
| `docs/decisions/ADR-0001-agentic-engine-not-editor-plugin.md` | Accepted | Agentic engine rather than editor plugin. |
| `docs/decisions/ADR-0002-headless-first-artifact-first.md` | Accepted | Headless-first and artifact-first. |
| `docs/decisions/ADR-0003-csharp-primary-behavior-language.md` | Accepted | C# as primary behavior language. |
| `docs/decisions/ADR-0004-runtime-evaluation-order.md` | Accepted | Runtime evaluation order. |
| `docs/decisions/ADR-0005-research-docs-are-non-authoritative.md` | Accepted | Research docs are non-authoritative. |
| `docs/decisions/ADR-0006-establish-engineering-substrate-before-runtime.md` | Accepted | Engineering substrate before runtime. |
| `docs/decisions/ADR-0007-expose-minimal-runtime-through-tools-cli.md` | Accepted | Minimal runtime through tools CLI. |
| `docs/decisions/ADR-0008-product-cli-is-the-agent-facing-product-api.md` | Accepted | Product CLI is agent-facing API. |
| `docs/decisions/ADR-0009-adopt-external-guide-system-v0.2.0.md` | Accepted | External guide-system routing and sync metadata. |
| `docs/decisions/ADR-0010-scenario-runner-before-asset-workbench.md` | Accepted | Scenario runner before workbench. |
| `docs/decisions/ADR-0011-content-validation-before-asset-curation.md` | Accepted | Content validation before curation. |
| `docs/decisions/ADR-0012-asset-metadata-before-visual-workbench.md` | Accepted | Asset metadata before visual workbench. |
| `docs/decisions/ADR-0013-review-pack-before-interactive-curation-editor.md` | Accepted | Review pack before editor. |
| `docs/decisions/ADR-0014-one-bounded-smoke-journey-for-broad-expansion.md` | Accepted | Bounded smoke journey for broad expansion. |
| `docs/decisions/ADR-0015-behaviors-emit-intents-and-spatial-modules-resolve-them.md` | Accepted | Behaviors emit intents; spatial modules resolve. |
| `docs/decisions/ADR-0016-runtime-owns-entities-components-and-spatial-modules-own-spatial-semantics.md` | Accepted | Runtime/entity/spatial ownership boundary. |
| `docs/decisions/ADR-0017-authored-definitions-instantiate-runtime-entities-and-interactions-use-explicit-intents.md` | Accepted | Definitions, spawns, and explicit interactions. |
| `docs/decisions/ADR-0021-workspaces-separate-game-truth-from-engine-acquisition.md` | Accepted | Separate game truth from workspace acquisition. |
| `docs/decisions/ADR-0018-rendering-is-read-only-and-raylib-is-an-isolated-adapter.md` | Accepted | Read-only rendering and isolated raylib adapter. |

## Decision policy

Create an ADR for durable choices affecting architecture, repository shape, runtime, content, validation, command/artifact contracts, or public APIs. Routine implementation details governed by existing authority do not need separate ADRs.
| `docs/decisions/ADR-0028-engineering-validation-uses-resumable-fingerprinted-suites.md` | Accepted | Resumable fingerprinted engineering validation. |
| `docs/decisions/ADR-0040-shared-local-asset-home-is-authoring-infrastructure-not-game-authority.md` | Proposed for M028 | Shared local discovery and annotations remain outside game/runtime authority. |
| `docs/decisions/ADR-0041-asset-preview-host-is-a-separate-restartable-client-around-engine-systems.md` | Proposed for M029 | Workbench state survives a separately restartable preview client. |
| `docs/decisions/ADR-0029-human-review-state-is-repository-local.md` | Accepted | Repository-local required review state. |
| `docs/decisions/ADR-0049-platform-epochs-defer-inactive-platform-verification.md` | Accepted | Active platform epochs and deferred inactive-platform verification. |
| `docs/decisions/ADR-0039-m027-combines-authoring-contract-stabilization-with-guide-system-v060-review-migration.md` | Proposed | Stable authoring evidence and milestone-scoped review migration. |
