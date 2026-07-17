# Performance report — M025

Status: `possible-regression`

This is an advisory, same-machine comparison. Timing values are observational and not deterministic receipt fingerprints.

| Workload | Elapsed change | Allocation change | Status |
|---|---:|---:|---|
| performance.runtime-reference | 42.20% | 0.00% | possible-regression |
| performance.entities-reference | 65.80% | 0.00% | possible-regression |
| performance.persistent-world-reference | 46.41% | 0.00% | possible-regression |
| performance.presentation-reference | 49.13% | 0.00% | possible-regression |

Signal Passage expected cost: more entities, behavior executions, spatial work, geometry render items, HUD text, synthesized content bytes, and export bytes. These are expected workload increases, not same-work regressions.
