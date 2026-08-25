# Deferred Guide Sync — Human Review Boundary and Simple Review UX

## Source

M038 in `carlrabbit/dotnet-ai-first-2d-game-engine`.

## Project conclusions to consider for generic guide authority

The project found recurring human-review failure modes:

1. humans were asked to inspect machine-verifiable reports instead of automation deciding those predicates;
2. genuine UX/visual questions could exist without an executable experience capable of answering them;
3. normal human review becomes unnecessarily manual when each question requires a separate command invocation.

Project-local M038 now adopts:

- machine acceptance before human review;
- human review only for irreducibly perceptual/experiential criteria;
- one simple question per durable review item;
- one normal milestone review-run that sequentially presents all currently open simple items;
- immediate Accept/Reject progression with asynchronous durable persistence;
- visible persistence/reset activity instead of UI freezing;
- a whole-review reset operation rather than automatic process restart;
- no reviewer comments/history or persisted workbench session/queue.

## Sync scope

A future guide-system documentation pass may consider the machine-versus-human boundary, missing-live-experience failure mode, and whether generic milestone review guidance should distinguish durable review items from the human-facing review-run UX.

The project's Raylib layout, background queue implementation, reset command names, and per-item interaction bound do not need to become generic guide requirements.

## Completion

Resolve this hint only when external guide authority has explicitly considered these review-boundary and human-workflow conclusions.

M038 implementation does not depend on this synchronization.
