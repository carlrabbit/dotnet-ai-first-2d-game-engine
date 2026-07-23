# Internal Testing Scale and Performance Contract

## Authority

Authoritative for M035 supported scale envelopes, metric definitions, budgets, baselines, and regression decisions.

## Support envelope

A support envelope is a versioned set of fixture, host, scale, and budget declarations.

Required fields:

- envelope ID/version;
- repository/build fingerprint;
- host/runtime classification;
- region, worker, entity, infrastructure, opportunity, activity, reservation, and queue sizes;
- detailed/abstract mode composition;
- graphical/headless classification;
- metric budgets;
- known limitations.

## Metrics

Use stable names, units, sample rules, warm-up rules, and percentile definitions.

Required families:

```text
runtime.fixed-step
work.derivation
work.selection
navigation.search
abstract.events-per-second
abstract.queue-size
fidelity.materialize
fidelity.abstract
operations.projection
persistence.save
persistence.load
memory.working-set
memory.managed-allocation
render.frame-time
soak.throughput-trend
```

## Budget classes

- `blocking-semantic`: correctness/capacity requirement;
- `blocking-operational`: required to make the supported fixture usable;
- `advisory-performance`: same-machine target;
- `regression-threshold`: allowed baseline movement.

A semantic failure cannot be waived by performance.

## Baselines

Baseline records include provenance, environment, samples, fingerprint, and promotion decision.

Silent replacement is prohibited.

Non-comparable results are not passes.

## Trend detection

Soak evidence must detect monotonic or unbounded growth in memory, queue, journal, artifact, and projection cost.

## Optimization evidence

Every targeted optimization references the failing/regressed metric and includes before/after semantic-equivalent runs.

## Exclusions

- universal hardware claims;
- cross-machine raw timing comparison without classification;
- speculative architecture rewrites;
- timing as gameplay authority.
