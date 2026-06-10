# ADR-0003 — C# is the primary behavior language

## Status

Accepted as initial direction.

## Decision

C# is the default behavior language. F# may be optional later for rule-heavy or state-machine-heavy modules.

## Consequences

- Behavior modules should be analyzable by Roslyn analyzers.
- Source generators can produce registries and bindings.
- Behavior code should read queries and emit commands.
