# M034 Settlement Infrastructure and Operations Scenario

## Identity

```text
scenario.m034.settlement-infrastructure-and-operations
```

## World

```text
region.river
region.fields
region.home
```

One detailed at a time; others abstract. Each is locally sustainable. No cross-region transport.

Initial pressure: insufficient water storage, limited food, inadequate comfort, unbuilt infrastructure, and available wood/material extraction.

Required capabilities across the world: water collector/storage, food storage, farm, shelter/comfort, maintenance service, and road/path improvement.

Sequence:

1. inspect dashboard;
2. plan/build water infrastructure;
3. set water reserve;
4. establish farmland/food storage;
5. build comfort;
6. observe all regions across fidelity;
7. diagnose/recover water shortage;
8. diagnose/recover storage bottleneck;
9. diagnose/repair maintenance failure;
10. switch regions during active work;
11. save/load during shortage/failure;
12. stabilize;
13. run fourteen more days.

Evidence checkpoints: first plan, blocked delivery, completed water system, abstract crop growth, crop materialization, low-water alert, full-storage alert, maintenance alert, failure, repair, road cost change, save/load continuation, stable dashboard.

Assertions: conservation, no leaked reservation, no duplicate completion, no cross-fidelity semantic mismatch, causal alerts, recovered shortages, completed stabilized run.

Artifacts under `artifacts/simulation/M034/`.
