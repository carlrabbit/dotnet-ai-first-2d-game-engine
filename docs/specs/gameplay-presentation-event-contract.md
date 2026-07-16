# Gameplay Presentation Event Contract

## Authority

Authoritative for the boundary between gameplay domain events and presentation projections.

Required post-commit events:

```text
resource.changed
entity.damaged
entity.defeated
item.collected
entity.removed
```

Events retain source, target, runtime tick, correlation, command/transaction linkage, before/after values, and provenance.

Animation, sound, rendering, review, and diagnostics may consume these events. Consumers cannot mutate components, issue damage/collection commands, alter lifecycle, or make gameplay depend on presentation timing.

Derived presentation requests record the originating event ID. Equivalent domain event streams must produce equivalent presentation fingerprints.
