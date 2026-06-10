# Architecture

## Authority

This document indexes structural design documents for the engine.

## Current architecture documents

- `docs/architecture/repository-profile.md`
- `docs/architecture/system-shape.md`
- `docs/architecture/runtime-evaluation.md`
- `docs/architecture/game-folder-model.md`

## Initial architecture direction

The engine should behave more like a domain-specific compiler plus deterministic runtime than a traditional editor-centric engine.

Primary architectural seams:

```text
contracts
engine core
runtime adapter
debug runtime representation
packaged runtime representation
validation
scenario runner
asset pipeline
source generation
tools / CLI
game content
behavior modules
```
