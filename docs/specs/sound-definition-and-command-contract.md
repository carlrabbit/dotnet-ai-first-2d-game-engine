# Sound Definition and Command Contract

## Authority

Authoritative for authored sound definitions, semantic cue identity, deterministic variants, sound groups, backend-neutral commands, keyed loops, command frames, and adapter boundaries.

Definitions contain stable IDs, one semantic cue, one group, variants, raw asset references, bounded defaults, tags, and provenance.

```text
sound-group.effects
sound-group.ambience
```

```text
volume: [0,1]
pitch: [0.25,4]
pan: [-1,1]
```

All values are finite.

Commands:

```text
PlayCue
StartLoop
ReplaceLoop
StopLoop
SetGroupVolume
```

Loops use explicit `loopInstanceKey`. Duplicate start is rejected. Replacement is explicit. Missing stop is an accepted no-op with an explicit result.

One immutable frame per applicable runtime tick contains ordered selections, commands, loop state, diagnostics, and fingerprint.

Sound is presentation-only. Only the raylib debug adapter may translate commands to native audio. Physical playback is optional.
