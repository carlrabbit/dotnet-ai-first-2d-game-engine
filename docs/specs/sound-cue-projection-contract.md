# Sound Cue Projection Contract

## Authority

Authoritative for deterministic projection from animation marker occurrences, gameplay domain events, and explicit presentation requests into cue selections and sound commands.

Required mappings:

```text
presentation.footstep → cue.player.footstep
entity.damaged        → cue.entity.damage
entity.defeated       → cue.entity.defeat
item.collected        → cue.item.collection
```

Selection derives from stable cue/source/tick/ordinal/seed context or an explicit variant. Ambient randomness is forbidden.

Evidence records definition, variant, source, mapping, reason, effective values, and fingerprint.

Ordering is deterministic by tick, source priority, source ID, occurrence ordinal, cue ID, and command ID.

Projection consumes immutable events/markers and cannot mutate runtime state.
