# M035 Heavy Internal Testing Readiness Campaign

## Identity

```text
campaign.m035.heavy-internal-testing-readiness
```

## Fixtures

### Supported-scale settlement

- five regions;
- fifty workers;
- at least one thousand authoritative entities;
- M034 infrastructure and operations systems;
- one detailed region;
- abstract execution elsewhere.

### Queue stress

At least ten thousand peak scheduled triggers with controlled stale/cancelled cases.

### Graphical fixture

At least ten detailed-region workers and representative infrastructure, plans, alerts, and switching.

## Required campaigns

1. Population/entity scale.
2. Work and path contention.
3. Abstract queue and stale triggers.
4. One-thousand transition churn.
5. Two-hundred-fifty save/load cycles.
6. Shortage, maintenance, and recovery stress.
7. Three-hundred-sixty-five-day headless soak.
8. Four-hour graphical soak.
9. Fault-injection matrix.
10. Reproduction-bundle verification matrix.

## Deterministic assertions

- identical semantic reruns match fingerprints where exact equivalence is required;
- zero-tolerance invariants remain valid;
- injected failures produce expected signatures;
- recovery reaches expected previous-good or migrated state;
- no required run terminates early;
- no unbounded growth trend.

## Readiness output

The campaign produces a readiness candidate of:

```text
ready
ready-with-declared-limitations
not-ready
```

The human review owns the final milestone completion decision within the allowed choices.
