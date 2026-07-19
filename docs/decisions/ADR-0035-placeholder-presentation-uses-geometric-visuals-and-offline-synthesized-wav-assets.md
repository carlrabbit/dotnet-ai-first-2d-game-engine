# ADR-0035 — Placeholder Presentation Uses Geometric Visuals and Offline Synthesized WAV Assets

## Status

Proposed for Milestone 025. Accept when implementation is accepted.

## Context

The first consumer vertical slice must be readable and reviewable without being blocked by external art or audio production.

## Decision

Placeholder visuals use a bounded geometric visual vocabulary projected through the existing rendering architecture.

Placeholder sounds use deterministic authored synthesis definitions rebuilt offline into ordinary mono PCM WAV assets consumed through the existing sound-definition pipeline.

## Consequences

The reference game has a coherent procedural identity and no external asset dependency. Geometry and synthesis become reusable authoring capabilities without adding arbitrary vector graphics, runtime DSP, or a music system.
