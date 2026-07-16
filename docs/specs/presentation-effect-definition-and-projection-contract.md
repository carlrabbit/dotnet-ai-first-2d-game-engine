# Presentation Effect Definition and Projection Contract

## Authority

Authoritative for authored effect definitions, event mappings, effect requests, deterministic instances, lifetime, child presentation requests, save/load behavior, and effect evidence.

Effects are presentation only. They consume post-commit events and cannot mutate runtime state.

Instances use stable IDs, runtime-tick start/duration, deterministic seeds, source-event linkage, and explicit world/screen domains.

Required mappings include damage, collection, switch activation, door opening, and successful save completion.

Transient effects are excluded from canonical saves and are not recreated after load.
