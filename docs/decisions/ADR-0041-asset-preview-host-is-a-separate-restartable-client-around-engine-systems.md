# ADR-0041 — Asset Preview Host Is a Separate Restartable Client Around Engine Systems

## Status

Proposed for M029. Accept when M029 completes.

## Decision

Use one separate restartable preview-host process per workbench session. It communicates through versioned local IPC and uses actual engine content, rendering, animation, sound projection, and adapters.

The workbench owns UI input, sessions, aliases, decisions, and promotion. The preview host owns temporary preview/playback/capture state only.

## Consequences

One normal window persists, preview crashes do not lose input usability or decisions, reconnect is required, and no second audiovisual/content implementation is introduced.
