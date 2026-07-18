# ADR-0037 — Small-Workload Timing Is Informational and Scaled Workloads Carry Comparison Authority

## Status

Proposed for M026. Accept when implementation is accepted.

## Decision

Elapsed timing below a 10 ms reference median is non-authoritative for ordinary percentage regression classification. Small workloads remain for correctness, counters, allocations, and a bounded catastrophic sentinel. Add scaled real workloads targeting at least 10 ms and retain the simple one-warm-up, five-measurement median method.

## Consequences

Reports avoid misleading percentages on tiny durations while preserving lightweight validation. No heavy statistical subsystem is introduced.
