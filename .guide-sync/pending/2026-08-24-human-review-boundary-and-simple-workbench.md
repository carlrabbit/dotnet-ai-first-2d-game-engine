# Deferred Guide Sync — Human Review Boundary and Simple Review UX

## Source

M038 in `carlrabbit/dotnet-ai-first-2d-game-engine`.

## Project conclusion to consider for generic guide authority

The project found two recurring human-review failure modes:

1. humans were asked to inspect machine-verifiable JSON/reports instead of automation deciding those predicates;
2. genuine UX/visual questions could be declared reviewed even when no executable experience existed that allowed the reviewer to answer them.

Project-local M038 adopts:

- machine acceptance before human review;
- human review only for irreducibly perceptual/experiential criteria;
- a live experience as human evidence when behavior is interactive;
- a deliberately small Restart/Reject/Accept review UI;
- no reviewer comments/history/queue as part of the normal human workflow;
- machine suite verification independent of pending human approval.

## Sync scope

A future guide-system documentation pass may update generic planning/execution guidance so it does not encourage humans to manually validate objective evidence and so a required human gate must identify evidence/experience sufficient to answer its subjective question.

The project-specific two-interaction Review Workbench and Raylib implementation do not need to become generic guide requirements.

## Completion

Resolve this hint only when the external guide authority has explicitly considered the machine-versus-human boundary and missing-human-experience failure mode.

M038 implementation does not depend on this sync.
