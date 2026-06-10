# Runtime Principles

## Authority

This document is authoritative for initial runtime principles.

## Principles

- Fixed tick behavior is preferred for deterministic simulation.
- Commands request mutation.
- Systems validate and apply mutation.
- Events record factual outcomes.
- Queries read current state or projections.
- Behaviors read through queries and emit commands.
- Runtime state must be inspectable in structured form.
- Debug and packaged modes must preserve semantic equivalence.

## Avoid in hot paths

```text
reflection
boxed messages
string routing
JSON parsing
enterprise-style async command dispatch
DI-heavy pipelines
```

## Prefer in hot paths

```text
typed IDs
record structs
generated dispatch
event buffers
predeclared queries
dense component storage when needed
```
