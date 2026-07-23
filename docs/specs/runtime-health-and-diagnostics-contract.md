# Runtime Health and Diagnostics Contract

## Authority

Authoritative for M035 invariant monitoring, progress health, deadlock/livelock/starvation detection, and bounded failure evidence.

## Health monitor

Modes:

```text
off
checkpoint
continuous-bounded
failure-only
```

The monitor observes immutable/current authoritative state and produces diagnostics. It does not repair gameplay state.

## Invariants

Monitor the complete M031–M034 invariant set, including identity, lifecycle, fidelity ownership, triggers, activities, reservations, quantities, capacities, construction, crops, condition, alerts, and persistence references.

## Progress health

Track semantic progress using stable state fingerprints and meaningful counters.

Classify:

```text
healthy
validly-idle
resource-constrained
blocked-recoverable
starved
livelocked
deadlocked
invalid
```

## Required detectors

- same-state repeated work selection;
- reservation cycle/leak;
- route replan loop;
- same-instant trigger loop;
- ownerless activity;
- activity timeout without progress;
- critical need starvation despite supply;
- satisfiable construction/maintenance demand never scheduled;
- unchanging alert with unresolved causal contradiction.

## Evidence window

Failure evidence includes bounded recent commands, events, triggers, decisions, routes, activities, reservations, alerts, and fingerprints.

Retention and truncation are explicit.

## Diagnostic format

Stable code, severity, classification, first/current instant, related IDs, expected/actual values, causal references, artifact pointers, and suggested triage command.

## Determinism

Equivalent valid runs produce equivalent invariant results. Monitoring must not change semantic outcomes.
