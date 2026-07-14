# Animation Execution Artifact Contract

## Authority

Authoritative for animation validation, compilation, selection, playback, sampling, marker, animated-render, and replay evidence.

## Required files

```text
animation-result.json
compiled-animation.json
animation-selections.jsonl
animation-playback.jsonl
animation-samples.jsonl
animation-markers.jsonl
animated-render-items.jsonl
animation-diagnostics.json
```

Artifacts must reconstruct:

```text
runtime state → selection reason/key → playback → sampled tracks/keyframes → markers → composition → animated render item
```

Canonical compiled definitions and fingerprints are included. Selection, playback, samples, markers, and winning-property provenance are explicit.

Exclude wall-clock timestamps, durations, process IDs, absolute paths, GPU state, and undefined enumeration order. Replay compares selections, playback, samples, markers, animated items, and final render fingerprint.
