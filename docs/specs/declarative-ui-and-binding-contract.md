# Declarative UI and Binding Contract

## Authority

Authoritative for UI documents, element vocabulary, deterministic screen-space layout, finite semantic bindings, binding providers, ordering, visibility, and UI render projection.

Elements:

```text
root
panel
text
image
horizontal-stack
vertical-stack
progress-bar
```

Bindings use a registered finite semantic vocabulary. Arbitrary property paths, reflection, expressions, callbacks, and scripts are invalid.

Binding providers read immutable prepared projection state and never mutate gameplay.
