# ADR-0056 — Resume Equivalence Requires Process-Separated Continuation

## Status

Accepted for M044.

## Context

Historical M020 proved object reconstruction inside one process and compared reduced event fields that omitted event identity. M039–M042 established stronger fresh-process and continuation evidence for simulation-specific capabilities.

With M043 providing one canonical durable save architecture, the repository needs one durable rule for what "resume equivalence" means.

## Decision

Canonical resume equivalence requires distinct control, producer and consumer OS processes.

The producer must exit before the consumer starts. The consumer must load the producer's durable bytes and continue beyond the checkpoint.

Under the same semantic initial state, seed, external schedule/policy and target, final canonical authoritative state is exact.

Event/command sequence and identity continuity are part of the comparison; identifiers may not be dropped.

Recovery is not complete until recovered bytes are loaded and continued in a separate process.

Producer-authored pass/fail booleans and artifact existence are not acceptance authority.

## Consequences

- object recreation alone no longer qualifies as canonical persistence continuation proof;
- sequence-reset and duplicate-ID defects become mechanically visible;
- M040–M042 continuation semantics are exercised through the same product persistence boundary;
- recovery evidence proves resumability, not only checksum/file restoration;
- M044 can remain entirely machine-reviewed.
