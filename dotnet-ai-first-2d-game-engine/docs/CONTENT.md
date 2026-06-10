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
