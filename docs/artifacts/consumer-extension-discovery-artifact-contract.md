# Consumer Extension Discovery Artifact Contract

## Authority

Authoritative for recording how a consumer-game requirement maps to consumer code/content, reusable engine capability, provisional workaround, or boundary violation.

## Required files

```text
consumer-extension-report.json
consumer-extension-report.md
```

## Entry classification

```text
engine-capability-reused
consumer-only
supported-consumer-extension
new-general-engine-capability
temporary-engine-workaround
boundary-violation
```

## Required entry fields

- feature ID;
- consumer requirement;
- implementation location;
- consumer files;
- engine files changed;
- engine internals accessed;
- classification;
- rationale;
- validation evidence;
- boilerplate assessment;
- missing diagnostics or validation;
- recommended M026 action;
- priority.

## Summary

The report summarizes:

- ordinary game changes requiring engine modification;
- leaked internal APIs;
- repeated registration/serialization plumbing;
- temporary workarounds;
- justified M026 extension candidates;
- rejected speculative extension candidates;
- external-repository readiness assessment.

## Completion rule

Every feature in the accepted Signal Passage journey has exactly one current classification.

`boundary-violation` and `temporary-engine-workaround` entries do not automatically fail M025, but each requires a concrete prioritized M026 recommendation and cannot be silently omitted.
