# Deterministic Particle Projection Contract

## Authority

Authoritative for bounded particle emitters, deterministic spawn values, scalar curves, per-tick linear motion, render-item projection, ordering, and replay evidence.

Initial model:

- fixed particle count;
- fixed lifetime;
- stable seed inputs;
- bounded position, velocity, scale, rotation, tint, and opacity ranges;
- constant, linear, and linear-inverse curves;
- no collision, physics, GPU simulation, force fields, or scripts.

Particles are transient presentation state and are excluded from saves.
