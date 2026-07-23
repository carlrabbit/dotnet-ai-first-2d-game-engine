# Stress, Soak, and Fault Campaign Contract

## Authority

Authoritative for M035 deterministic campaigns, nested resumable execution, soak completion, and fault injection.

## Campaign

A campaign contains:

- stable campaign ID/version;
- fixture generator or authored fixture;
- case matrix;
- seeds;
- safety limits;
- required metrics/invariants;
- receipt schema;
- aggregate verifier;
- failure reduction metadata.

## Execution

```text
plan
→ run case/shard
→ write receipt
→ resume remaining cases
→ verify campaign
```

Partial case output is not campaign success.

## Required campaign families

- population/entity scale;
- work/path contention;
- abstract queue/stale triggers;
- transition churn;
- infrastructure shortage/recovery;
- persistence cycles;
- 365-day headless soak;
- four-hour graphical soak.

## Fault injection

Faults are explicit, deterministic, disabled by default, and installed only through test composition.

Required boundaries include command commit, persistence write/load, transition reconciliation, entity/reservation invalidation, navigation/abstract graph, duplicate delivery, projection, and graphical adapter termination.

## Soak completion

A soak receipt records target and completed duration, early termination, checkpoints, saves/loads, metrics trend, invariants, failures, and final fingerprint.

An early-terminated required soak fails.

## Bounded artifacts

Campaign logs and traces use retention/size limits and preserve causal failure windows.
