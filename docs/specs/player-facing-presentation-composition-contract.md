# Player-Facing Presentation Composition Contract

## Authority

Authoritative for composing world rendering, animation, effects, particles, camera transformation, UI, text, prompts, notifications, layers, clipping, and final fingerprints.

Order:

```text
world state
→ animation and world effects
→ camera transform and clipping
→ world layers
→ screen-space UI/text/prompts
→ final composed frame
```

World and screen spaces are explicit. Stable layer and item ordering is mandatory.
