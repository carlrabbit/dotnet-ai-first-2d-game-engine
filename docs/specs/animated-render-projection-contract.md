# Animated Render Projection Contract

## Authority

Authoritative for applying sampled animation values to visual definitions before backend-neutral render-item generation.

## Pipeline

```text
visual definition defaults
+ base animation patch
+ optional overlay patch
→ animated visual instance
→ semantic render items
→ existing render commands
```

Animation remains read-only presentation state. Runtime transforms remain authoritative; animated visual offsets do not mutate them.

## Provenance

Animated render evidence retains source, visual definition, animation definition, base/overlay selections, local ticks/status, contributing tracks/keyframes, winning layer per property, final values, runtime tick, and fingerprints.

## Adapter boundary

No raylib types appear in animation or animated projection contracts. The existing raylib adapter renders the resulting backend-neutral commands.
