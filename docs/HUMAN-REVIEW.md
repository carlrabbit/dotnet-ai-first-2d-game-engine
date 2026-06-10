# Human Review

## Authority

This document is authoritative for human acceptance gates until more specific review contracts exist.

## Purpose

Automation can validate structure, references, determinism, schemas, and many invariants. It cannot fully judge visual quality, gameplay feel, UX clarity, semantic appropriateness, or design intent.

## Initial review classes

| Review class | Requires human review |
|---|---|
| Visual semantics | Uncertain labels, new taxonomy labels, visually ambiguous tiles/sprites. |
| Physical semantics | Walkability, collision, navigation cost, damage, blockers. |
| Gameplay semantics | Interactability, harvestability, collectibility, spawn relevance, quest relevance. |
| Visual output | Previews, overlays, animation hitboxes, shader/material previews. |
| UX/game feel | Controls, UI flow, readability, pacing, animation feel. |

## Approval record concept

A future approval record should include:

```text
reviewId
artifactPath
reviewClass
reviewer
status: approved | rejected | changes-requested
notes
timestamp
related content IDs
```
