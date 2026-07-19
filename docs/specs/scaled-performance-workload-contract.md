# Scaled Performance Workload Contract

## Authority

Authoritative for timing-authority classification, scaled real workloads, small-workload handling, and M026+ performance interpretation.

Sub-10-ms fixed workloads remain useful for correctness, deterministic work counters, allocations, and catastrophic sentinels. Ordinary percentage movement is not timing authority.

A capture records `timingAuthority`, `timingAuthorityReason`, and `referenceMedianMilliseconds`.

Default floor: `10 ms`.

Below the floor:

```text
timingAuthority: false
status: not-timing-authoritative
```

Optional catastrophic advisory requires both after median >= 10 ms and after median >= 4 × before median.

Required scaled workloads:

```text
performance.runtime-reference-scaled
performance.entities-reference-scaled
performance.persistent-world-reference-scaled
performance.presentation-reference-scaled
```

They use fixed seed, real engine operations, deterministic counters, headless execution, at least 10 ms target median, and no artificial busy loops.

Retain one warm-up, five measurements, median, same-machine, same-configuration, close-in-time limitations. No significance framework is required.
