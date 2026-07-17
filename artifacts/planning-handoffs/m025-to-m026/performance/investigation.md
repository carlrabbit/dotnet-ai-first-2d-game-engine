# M025 fixed-reference performance investigation

Conclusion: **measurement-defect**. The original M025 timing deltas were not reproduced on fresh, same-machine captures of the actual M024 base (`50ac5bf`) and reviewed M025 revision (`88f9051`). These are fixed reference workloads; they must not be described as Signal Passage workload growth.

| Workload | Original median (before → after) | Original change | Fresh median (M024 → M025) | Fresh change | Allocation / counters |
|---|---:|---:|---:|---:|---|
| runtime-reference | 0.0564 → 0.0802 ms | +42.20% | 0.0648 → 0.0528 ms | -18.52% | identical / identical |
| entities-reference | 0.0655 → 0.1086 ms | +65.80% | 0.0726 → 0.0567 ms | -21.90% | identical / identical |
| persistent-world-reference | 0.0836 → 0.1224 ms | +46.41% | 0.0946 → 0.0790 ms | -16.49% | identical / identical |
| presentation-reference | 0.0920 → 0.1372 ms | +49.13% | 0.1075 → 0.0891 ms | -17.12% | identical / identical |

The fresh captures used Release/headless, the same canonical workload definitions, fixed seed, one warm-up, five measured iterations, and summary metrics. They were run close together on this machine in isolated clean worktrees. The capture format does not retain individual samples or variance. At 0.05–0.14 ms medians, CPU frequency, scheduling, cache, and JIT/process effects are credible contamination sources.

No M025-specific common-path cost is established. Geometry projection, offline synthesis tooling, consumer workspace/export integration, and the temporary playable Raylib host were changed in M025, but these fixed `RuntimeSmokeScenario` workloads did not show changed work counters or allocations, and the effect reversed on remeasurement.

The historical M025 report remains a truthful record of what its original capture observed, but it requires this correction note and should be treated as **inconclusive for a regression claim**. M026 should include a focused performance-methodology investigation: increase sample count, retain distributions/variance, interleave revisions where feasible, and add environment/JIT diagnostics before escalating fixed-work timing deltas.

Raw fresh captures and the canonical comparison are retained in `investigation-before/`, `investigation-after/`, and `investigation-comparison/`.
