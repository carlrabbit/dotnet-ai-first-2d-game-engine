# ADR-0034 — First Reference Game Is an Isolated In-Repository Consumer Workspace

## Status

Proposed for Milestone 025. Accept when implementation is accepted.

## Context

The engine needs real consumer pressure before stabilizing an SDK or extension API. A separate repository would add cross-repository coordination while extension boundaries are still expected to change.

## Decision

The first reference game lives under:

```text
consumers/signal-passage/
```

It uses the existing workspace/project model and a separate consumer assembly.

Isolation validation relocates/materializes the workspace outside its repository location and proves that no undeclared co-location dependency is required.

## Consequences

Engine and consumer changes can remain atomic during discovery. The game is treated as a real consumer rather than an engine fixture. A permanent repository split is deferred until extension and acquisition boundaries are proven.
