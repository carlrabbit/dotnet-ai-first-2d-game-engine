# Signal Passage Consumer Vertical Slice Contract

## Authority

Authoritative for the first reference consumer game's identity, workspace ownership, visual language, objective loop, persistent state, scenarios, and bounded dogfood responsibilities.

## Game identity

```text
ID: game.signal-passage
display name: Signal Passage
tone: abstract, calm, mechanical
target play length: 3–5 minutes
violence: abstract damage only
```

## Workspace ownership

Signal Passage owns:

- consumer game code;
- authored game content;
- game scenarios;
- geometry/color assignments;
- synthesis definitions;
- game-specific objective semantics;
- review artifacts;
- extension-discovery evidence.

The engine owns reusable runtime and authoring capabilities.

## Visual language

```text
player: cyan circle
container: orange diamond
hazard: red triangle
energy fragment: yellow regular polygon
switch: violet square, bright inner mark when active
closed exit: solid green-tinted gate
open exit: separated green gate parts
objective zone: green ring
walls: muted blue-gray rectangles
background: dark blue-gray
```

## Objective loop

```text
collect three energy fragments
→ activate mechanism
→ open exit
→ enter destination zone
→ complete run
```

## Required state

- player health;
- fragment progress;
- container state;
- mechanism state;
- exit state;
- objective completion;
- persistent save/resume state.

Transient sound/effect/notification state is not persisted.

## Required sounds

```text
fragment collected
container opened
player damaged
switch activated
exit opened
objective completed
```

All are produced from deterministic synthesis definitions and consumed through existing sound commands.

## Required evidence

- workspace/project validation;
- complete deterministic journey;
- save/resume;
- structural presentation;
- synthesized asset generation;
- isolated workspace relocation;
- Linux export and equivalence;
- performance report;
- extension-discovery report;
- approved human review.
