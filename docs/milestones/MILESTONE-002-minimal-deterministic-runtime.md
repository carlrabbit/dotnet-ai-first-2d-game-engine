# Milestone 002 — Minimal Deterministic Runtime

## Goal

Create the first minimal engine core that can execute deterministic ticks, accept commands, emit events, answer queries, and produce a scenario-like result artifact.

## Required authority

- `docs/specs/project-thesis.md`
- `docs/specs/runtime-principles.md`
- `docs/specs/agentic-workflow.md`
- `docs/SCENARIOS.md`
- `docs/ARTIFACTS.md`

## Scope

- Initial contracts for IDs, commands, events, queries, diagnostics.
- Minimal engine state.
- Fixed tick loop.
- Event log.
- One trivial scenario or smoke runner.
- `result.json` artifact.

## Non-goals

- No renderer.
- No asset pipeline.
- No packaged runtime.
- No source generators except if explicitly justified for minimal typed IDs.

## Validation tier

Tier 1 focused implementation, then Tier 2 once `eng/check.sh` is meaningful.
