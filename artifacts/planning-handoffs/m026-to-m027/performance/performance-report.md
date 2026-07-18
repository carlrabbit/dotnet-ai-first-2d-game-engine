# Performance report — M026

Status: `possible-regression`

This is an advisory, same-machine comparison. Timing values are observational and not deterministic receipt fingerprints. Sub-10-ms references are not timing-authoritative; only scaled real workloads at or above 10 ms receive ordinary percentage classification.

| Workload | Elapsed change | Allocation change | Timing authority | Status |
|---|---:|---:|---|---|
| performance.runtime-reference | -4.35% | 0.00% | False | not-timing-authoritative |
| performance.entities-reference | -37.75% | 0.00% | False | not-timing-authoritative |
| performance.persistent-world-reference | -7.69% | 0.00% | False | not-timing-authoritative |
| performance.presentation-reference | -23.15% | 0.00% | False | not-timing-authoritative |
| performance.runtime-reference-scaled | -9.14% | 0.00% | True | improved |
| performance.entities-reference-scaled | 68.10% | 0.00% | True | possible-regression |
| performance.persistent-world-reference-scaled | 0.13% | 0.00% | True | within-noise |
| performance.presentation-reference-scaled | 1.86% | 0.00% | True | within-noise |
