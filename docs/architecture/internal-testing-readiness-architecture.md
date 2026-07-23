# Internal Testing Readiness Architecture

## Shape

```text
M031–M034 authoritative runtime/game
        │
        ├── health and invariant observers
        ├── metric and trend collectors
        ├── deterministic campaign/fault harness
        ├── compatibility and recovery tooling
        └── session/reproduction capture
                    │
                    ▼
          bounded readiness artifacts
                    │
                    ▼
          automated gate + human review
```

## Boundary

Readiness tooling observes, measures, checkpoints, or injects explicitly composed test faults. It does not become gameplay authority.

## Campaign hierarchy

M035 aggregate suite may contain nested resumable campaigns. Every parent receipt depends on a verified child campaign.

## Evidence flow

Failure detection captures a bounded causal window and a reproduction bundle. Aggregate reports reference detailed artifacts rather than duplicating them.

## Optimization loop

```text
measured failed/regressed budget
→ focused profile
→ targeted change
→ semantic equivalence
→ before/after comparison
→ disposition
```

## Persistence

Compatibility tooling is outside world mutation until a complete validated migration/load transaction commits.

## Architecture invariants

1. Monitoring does not change outcomes.
2. Fault injection is test-only and disabled by default.
3. Campaigns are deterministic and resumable.
4. Partial evidence never establishes readiness.
5. Baselines are versioned and explicit.
6. Recovery preserves previous-good evidence.
7. Reproduction bundles are portable and bounded.
8. Readiness is limited to a declared envelope.
