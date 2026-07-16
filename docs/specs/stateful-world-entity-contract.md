# Stateful World Entity Contract

## Authority

Authoritative for switch and door runtime entities, states, interaction intents/resolutions, atomic transitions, events, and persistence.

Switch states are `inactive` and `activated`; the primary policy is one-shot. Successful interaction atomically changes switch state and one declared flag, then emits `switch.activated`.

Door states are `locked`, `closed`, and `open`. Door interaction evaluates a bounded condition. Success atomically changes door state and collision participation, then emits door events.

Doors and switches are runtime entities. Authored maps remain structurally static. Presentation derives from committed state/events.
