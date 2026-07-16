# ADR-0022 — Sound Is Deterministic Presentation Projection

## Status

Proposed for Milestone 019. Accept when implementation is accepted.

## Context

The engine needs sound feedback from animation markers and gameplay events without making presentation gameplay authority or requiring physical audio for validation.

## Decision

Authored sound definitions map semantic cues to deterministic variants. A projector consumes immutable markers, domain events, or explicit presentation requests and emits backend-neutral commands. Loop ownership uses explicit keys and explicit start/replace/stop operations. Physical playback is optional and isolated in the raylib debug adapter.

## Consequences

Headless validation, replayable selection, and clear provenance are gained. Selection and loop semantics are intentionally explicit; advanced mixing and DSP remain unsupported.

## Rejected alternatives

Gameplay calling audio APIs, animation-driven gameplay, ambient randomness, implicit loop replacement, mandatory playback, and a general audio graph.
