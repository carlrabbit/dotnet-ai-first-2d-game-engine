# Unified Run Artifact Contract

## Authority

Authoritative for `run-manifest.json` and unified run discovery.

## Required run layout

```text
run-manifest.json
content/
input/
runtime/
animation/
render/
review/
diagnostics/
```

Only present artifact-family directories are required physically; absence is explicit in the manifest.

## Run manifest

Links workspace, project, engine acquisition, scenario, seed/tick configuration, subsystem evidence, diagnostics, screenshots, review evidence, status, fingerprints, and recommended next commands.

It references rather than duplicates subsystem artifacts.

## Integrity

Inspection validates referenced file presence, expected fingerprints, artifact-family status, and diagnostic linkage.

## Determinism

Exclude wall-clock timestamps, process IDs, durations, absolute paths, and transient Git worktree paths from semantic identity.

## Render family

A present render family links `render/render-result.json`, includes the final tick, projection fingerprint, diagnostics path, execution source (`same-execution` or `deterministic-replay`), and optional screenshot links. Structural render artifacts (`render-snapshot.json`, `render-frame.json`, render items, commands, asset bindings, and diagnostics) are automated authority. Screenshots are optional human-review evidence and are not semantic authority. When animation is present, the manifest preserves links to selection, playback, sample, marker, and animated-render-item evidence.
