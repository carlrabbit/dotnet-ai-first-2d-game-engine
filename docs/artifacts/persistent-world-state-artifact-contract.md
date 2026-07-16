# Persistent World State Artifact Contract

## Authority

Authoritative for flag, condition, switch, door, collision/projection invalidation, diagnostic, persistence, and replay/resume evidence.

Required files:

```text
persistent-world-result.json
flag-transitions.jsonl
condition-evaluations.jsonl
switch-intents.jsonl
switch-resolutions.jsonl
switch-transitions.jsonl
door-intents.jsonl
door-resolutions.jsonl
door-transitions.jsonl
projection-invalidations.jsonl
persistent-world-diagnostics.json
```

Records include before/after state, condition atom results, transaction/correlation IDs, ticks, collision changes, invalidations, events, contributor linkage, and fingerprints.
