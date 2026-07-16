# Player-Facing Presentation Artifact Contract

## Authority

Authoritative for effect, particle, camera, UI, text, prompt, notification, composition, diagnostic, replay, and post-load reconstruction evidence.

## Required files

```text
player-facing-presentation-result.json
effect-definitions.json
effect-requests.jsonl
effect-instances.jsonl
particle-emitters.json
particle-instances.jsonl
particle-samples.jsonl
camera-definitions.json
camera-states.jsonl
camera-requests.jsonl
ui-definitions.json
ui-binding-values.jsonl
ui-layout.jsonl
text-resources.json
font-resources.json
text-commands.jsonl
interaction-prompts.jsonl
notifications.jsonl
presentation-composition.jsonl
player-facing-presentation-diagnostics.json
```

Do not emit empty placeholder records for unexercised families.

Evidence must link every projected element to authored definition and immutable source state/event.
