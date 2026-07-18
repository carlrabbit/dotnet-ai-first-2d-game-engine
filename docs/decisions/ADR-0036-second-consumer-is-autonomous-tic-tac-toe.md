# ADR-0036 — Second Consumer Is Autonomous Tic-Tac-Toe

## Status

Proposed for M026. Accept when implementation is accepted.

## Decision

Create a committed standard 3 × 3 tic-tac-toe consumer. AI X and AI O play by default; a human may take over either participant. AI uses deterministic randomness and simulation-tick thinking delays. Existing animation, particle, sound, input, persistence, workspace, and export capabilities are reused.

## Consequences

The game tests turn sequencing, delayed decisions, controller switching, board interaction, dynamic presentation, save during pending action, repeated rounds, and scenario assertions without creating a general board-game, AI, or turn-scheduler framework.
