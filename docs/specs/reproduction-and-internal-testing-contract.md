# Reproduction and Internal Testing Contract

## Authority

Authoritative for M035 tester sessions, defect evidence, reproduction bundles, and readiness decisions.

## Tester session

A session has stable ID, build fingerprint, environment, scenario/save, seed, start/end, operator-provided notes, diagnostics mode, and artifact root.

## Reproduction bundle

Required contents or references:

- schema/version;
- build/repository fingerprint;
- scenario/campaign/case;
- seed and authored inputs;
- save/checkpoint;
- command/event/input replay where needed;
- expected failure signature;
- exact run and verify commands;
- artifact index;
- sanitization report;
- minimization status.

No dependency on local absolute paths, external guides, or planning chat.

## Commands

Conceptual command family:

```text
session start/finish/inspect
repro capture/inspect/run/verify
repro reduce
readiness inspect/verify
```

Exact CLI follows repository conventions.

## Failure signature

A stable signature uses diagnostic/invariant code, relevant semantic IDs/classes, and bounded state fingerprint—not volatile stack addresses.

## Readiness decision

```text
ready
ready-with-declared-limitations
not-ready
```

A limitation cannot waive corruption, duplication, unreproducible crash, failed recovery, or unresolved blocking defect inside the support envelope.
