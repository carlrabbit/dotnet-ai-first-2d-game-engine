# Canonical Save Resume Equivalence Contract

## Authority

Authoritative for proving process-separated continuation and recovery correctness of the canonical durable game save established by M043.

## Fresh process

Fresh-process evidence requires:

```text
control process A
producer process B
consumer process C
```

with distinct OS process identities and B fully exited before C starts.

C must load bytes written by B and advance beyond the saved checkpoint.

## Exact same-schedule equivalence

For identical semantic initial state, seed, execution policy/schedule and target:

```text
uninterrupted final canonical world
==
save → producer exit → consumer load → continued final canonical world
```

exactly.

No tolerance applies to same-schedule persistence continuation.

## Identity continuity

Persisted continuation preserves sequence/epoch state needed to allocate future deterministic IDs.

Post-load command/event IDs never reuse a pre-save ID, follow current deterministic sequence rules, and match the uninterrupted control under the same schedule.

Comparers must not discard identity fields to make a resume proof pass.

## Stable save points

Canonical saves are taken only at stable semantic boundaries. A half-committed fidelity transition is not a valid ordinary save point.

Required current checkpoint classes are defined by M044 and cover typed/reservation, tombstone, detailed, abstract, need and post-fidelity-transition states.

## External control

Execution schedule/policy intentionally external to gameplay state is re-supplied to the consumer and fingerprint-validated. It is not silently embedded in the world save to bypass the external-control contract.

## Product continuation

M037 catalog metadata resolves to an actual canonical save. Continue validates the chosen save, reconstructs the actual world and advances it.

A metadata-only `WorldSession` reconstruction is not save/resume proof.

## Recovery continuation

Recovery is complete only after:

```text
damaged current detected
→ valid previous-good selected
→ atomic recovery
→ separate process loads recovered bytes
→ world advances
→ target comparison passes
```

Recovery may not substitute a semantically incompatible previous-good save.

## Evidence integrity

Independent comparison derives process provenance, checkpoint/target progression, canonical equality, identity continuity, schedule/content compatibility, recovery provenance, and catalog/save linkage.

Producer-authored success booleans and artifact existence are not proof.
