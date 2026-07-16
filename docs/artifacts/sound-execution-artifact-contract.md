# Sound Execution Artifact Contract

## Authority

Authoritative for sound validation, cue selection, command, frame, playback-state, diagnostic, and replay evidence.

Required files:

```text
sound-result.json
sound-definitions.json
sound-cue-selections.jsonl
sound-commands.jsonl
sound-command-frames.jsonl
sound-playback-state.jsonl
sound-diagnostics.json
```

Evidence reconstructs:

```text
marker/event/request
→ mapping
→ cue definition
→ selected variant
→ effective values
→ command
→ loop state
```

Exclude wall-clock timestamps, process IDs, device identities, and physical playback timing from semantic fingerprints. Audible output is optional review evidence.
