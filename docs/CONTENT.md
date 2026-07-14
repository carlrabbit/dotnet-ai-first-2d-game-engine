# Content

## Authority

This document indexes authored non-code project data.

## Principles

Source content is schema-validatable, diff-friendly, stable-ID-addressable, inspectable, reviewable, and separate from generated outputs.

## Current authored roots

```text
game/scenarios/
game/assets/metadata/
game/assets/raw/
game/assets/reviews/
game/maps/
game/entities/
game/visuals/
game/input/maps/
game/input/sequences/
game/animations/
```

## Current validation domains

```text
scenarios
assets
maps
entities
visuals
input-maps
input-sequences
animations
```

Examples:

```bash
dotnet run --project src/Agentic2D.Tools -- content validate scenarios --output artifacts/content/scenarios
dotnet run --project src/Agentic2D.Tools -- content validate assets --output artifacts/content/assets
dotnet run --project src/Agentic2D.Tools -- content validate maps --output artifacts/content/maps
dotnet run --project src/Agentic2D.Tools -- content validate entities --output artifacts/content/entities
dotnet run --project src/Agentic2D.Tools -- content validate visuals --output artifacts/content/visuals
```

## Current domains

- scenarios;
- PNG-backed asset metadata and atlas regions;
- maps, static objects, and entity spawns;
- entity definitions and bounded whole-component overrides;
- visual definitions and multi-part visual bindings;
- authored asset review decisions.

Runtime entities, component state, trigger overlap state, interactions, and render frames are runtime or generated evidence, not authored source.

## Ownership boundaries

- A visual definition does not make a static object an entity.
- An entity definition is not a runtime entity.
- Maps remain authored static content plus explicit entity spawn declarations.
- Generated artifacts are not source truth unless explicitly declared otherwise.
- AI may propose visual labels; approved gameplay-relevant semantics remain review-gated.
