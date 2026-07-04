# Content

## Authority

This document is authoritative for structured non-code project data until more specific content contracts exist.

## Source content principles

Source content must be:

- schema-validated;
- diff-friendly;
- merge-friendly;
- addressable by stable ID;
- inspectable by agents;
- reviewable by humans;
- separated from generated outputs.

## Current validation surface

The current content validation foundation supports authored scenario JSON, authored asset metadata JSON, and authored map JSON.

Supported product CLI forms:

```bash
dotnet run --project src/Agentic2D.Tools -- content validate scenarios --output artifacts/content/scenarios
dotnet run --project src/Agentic2D.Tools -- content validate game/scenarios/smoke/runtime-smoke.json --output artifacts/content/runtime-smoke
dotnet run --project src/Agentic2D.Tools -- content validate assets --output artifacts/content/assets
dotnet run --project src/Agentic2D.Tools -- content validate game/assets/metadata/tile-atlas-smoke.asset.json --output artifacts/content/tile-atlas-smoke
dotnet run --project src/Agentic2D.Tools -- content validate maps --output artifacts/content/maps
dotnet run --project src/Agentic2D.Tools -- content validate game/maps/smoke/map-smoke.map.json --output artifacts/content/map-smoke
```

Supported engineering wrapper:

```bash
./eng/content-validate.sh scenarios
./eng/content-validate.sh assets
./eng/content-validate.sh maps
```

The validator writes generated evidence under the requested output directory:

```text
result.json
diagnostics.json
validated-items.json
```

## Game content domains

```text
entities
components
items
quests
dialogue
UI
maps
tilesets
materials
animations
shaders
taxonomies
rules
scenarios
```

## Asset semantics rule

Visual interpretation and gameplay behavior are separate.

Examples:

```text
grass      -> visual label
walkable   -> physical behavior
flower     -> visual label / decoration
collision  -> physical behavior
butterfly  -> visual label / possible decorative overlay
```

AI may propose labels. Approved metadata is source-of-truth content.

## Human review requirement

Human review is required for high-impact semantics such as collision, walkability, interactability, quest relevance, damage zones, collectibility, progression blockers, and navigation-critical metadata unless explicit project rules say otherwise.
