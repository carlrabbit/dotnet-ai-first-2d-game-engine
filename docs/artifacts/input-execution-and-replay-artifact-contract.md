# Input Execution and Replay Artifact Contract

## Authority

Authoritative for M016 mapping, tick-frame, recording, replay, and equivalence evidence.

## Required files

```text
input-map.json
raw-input-samples.jsonl
input-action-resolutions.jsonl
input-frames.jsonl
input-recording.json
input-replay-result.json
input-diagnostics.json
```

## Semantics

`input-map.json` contains canonical actions, bindings, policies, revision, and validation.

`raw-input-samples.jsonl` records sample sequence, source/device, semantic control, value, space, provenance, and diagnostics.

`input-action-resolutions.jsonl` records contributing bindings, transformed values, combination policy, final action value/phase, and diagnostics.

`input-frames.jsonl` records one consumed semantic frame per tick.

`input-recording.json` stores compatibility identity and ordered semantic frames.

`input-replay-result.json` stores compatibility, replay status, compared range, equivalence categories, mismatches, diagnostics, and artifact references.

## Determinism

Exclude wall-clock time, process IDs, durations, absolute paths, transient native device indices, and undefined collection order.

## Review linkage

Evidence must answer which sample/binding produced an action, which frame a tick consumed, which behavior intent followed, why replay matched/failed, and whether any physical adapter was required.
