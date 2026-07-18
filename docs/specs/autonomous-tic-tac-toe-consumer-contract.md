# Autonomous Tic-Tac-Toe Consumer Contract

## Authority

Authoritative for the second consumer game's identity, board state, participant controllers, deterministic AI, thinking delay, interaction, persistence, presentation, sounds, scenarios, and export proof.

```text
ID: game.autonomous-tic-tac-toe
display name: Autonomous Tic-Tac-Toe
board: 3 × 3
default: AI X versus AI O
```

State: `cells[9]`, current mark, X/O controller, phase, thinking ticks remaining, winner, round, X/O scores, draw count.

Controllers: `ai`, `human`.

Phases: `round-starting`, `thinking`, `awaiting-human-input`, `placing-mark`, `round-complete`, `resetting`.

AI enumerates free cells by stable index and selects through deterministic engine randomness. Thinking delay is 30–90 simulation ticks. No wall-clock or host randomness.

Semantic actions: `participant.take-x`, `participant.take-o`, `participant.release-x`, `participant.release-o`, `board.select-cell`, `round.restart`.

Standard rules apply. X starts unless an explicitly tested alternative is selected. Reset clears board/transients; scores persist.

Presentation uses geometric board/highlight/winner line, existing mark animation, thinking animation, existing deterministic particle effect, controller labels/prompts, and explicitly linked synthesized sounds. Presentation does not determine state.

Persist board, turn, controllers, phase, remaining thinking ticks, winner, round, scores, and draws. Do not persist transient audio, particles, prompt instances, or completed animation playback. Load must not replay old transient feedback.

The game validates under existing workspace and standalone Linux export contracts.
