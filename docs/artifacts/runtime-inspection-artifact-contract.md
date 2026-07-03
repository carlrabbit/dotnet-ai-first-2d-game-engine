# Runtime Inspection Artifact Contract

## Authority

Authoritative for `agentic2d runtime inspect` artifacts.

## Required files

```text
<output>/result.json
<output>/diagnostics.json
<output>/runtime-summary.json
<output>/entities.json
<output>/commands.jsonl
<output>/events.jsonl
<output>/final-state.json
<output>/assertions.json
<output>/content-references.json
```

Schemas use `agentic2d.runtime-inspection.*.v1`.

Required content:

- result: command, scenario, optional map, status, exit code, summary, diagnostics, artifacts;
- summary: final tick, counts, completion, identities;
- entities: stable IDs and supported state;
- commands: submission order and outcomes;
- events: runtime occurrence order;
- final state: full supported smoke projection;
- assertions: stable IDs, expected/actual where useful, outcomes;
- content references: scenario, map, asset, and other references;
- diagnostics: structured diagnostics.

Failure paths emit result/diagnostics and safe partial projections. Absolute paths, elapsed duration, process identity, and wall-clock timestamps are not semantic fields.
