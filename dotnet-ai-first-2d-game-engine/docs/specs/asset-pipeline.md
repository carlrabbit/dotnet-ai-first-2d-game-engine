# Asset Pipeline

## Authority

This document is authoritative for initial asset pipeline principles.

## Scope

Raw asset creation is out of scope. Asset interpretation, import, metadata authoring, previewing, validation, and provenance are in scope.

## Pipeline model

```text
raw asset
+ asset perception
+ semantic authoring commands
+ provenance
+ generated metadata
+ previews
+ validators
+ dependency graph
= agent-operable asset workflow
```

## Metadata layers

- Structural: tile size, rect, alpha, grid position, animation frames.
- Visual: grass, flower, butterfly, stone, wall, water.
- Physical: walkable, blocked, slow, collision, navigation cost.
- Gameplay: harvestable, damaging, cover, spawnable, interactable.
- Narrative/design: peaceful, magical, village, forest, dangerous.
- Technical: atlas group, render layer, material, shader.

## Review model

AI-proposed semantic tags are proposals. Structural consistency can be validated automatically. Gameplay-relevant semantics require review unless project rules explicitly auto-approve them.
