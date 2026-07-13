# Behavior and Spatial Execution Artifact Contract

## Authority

Authoritative for behavior/spatial evidence added to runtime inspection by Milestone 012.

## Required files

```text
<output>/behaviors.json
<output>/intents.jsonl
<output>/spatial-resolutions.jsonl
```

Existing runtime-inspection artifacts remain required.

`behaviors.json` records scenario ID, selected spatial module, assignments, behavior/entity IDs, lifecycles, execution ticks, snapshot fingerprints, and statuses.

`intents.jsonl` records stable intent ID, tick, assignment, behavior, entity, kind, payload, and ordering key.

`spatial-resolutions.jsonl` records intent, module, accepted/rejected status, reason, map/cell/tile references, semantic source/value, resulting command reference, events, and diagnostics.

Use relative paths. Exclude wall-clock, process, machine, and absolute-path data from semantic comparison. Assignments sort by stable ID; intents and resolutions use documented deterministic order.
