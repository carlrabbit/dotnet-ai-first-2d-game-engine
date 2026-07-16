# ADR-0027 — UI Bindings Use a Finite Semantic Vocabulary

## Status

Proposed for Milestone 021. Accept when implementation is accepted.

## Context

Arbitrary property paths, reflection, and expressions would make UI brittle, difficult to validate, and capable of bypassing domain boundaries.

## Decision

UI bindings use explicitly registered semantic binding IDs. Binding providers project immutable values from authoritative state and presentation state.

Unknown bindings fail validation. UI cannot mutate gameplay or reevaluate gameplay rules.

## Consequences

UI is more explicit and less generic, but remains deterministic, inspectable, and safe for agent authoring.
