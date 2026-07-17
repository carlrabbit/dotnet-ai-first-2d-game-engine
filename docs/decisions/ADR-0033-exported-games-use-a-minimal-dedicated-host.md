# ADR-0033 — Exported Games Use a Minimal Dedicated Host

## Status

Proposed for Milestone 024. Accept when implementation is accepted.

## Context

Publishing the product CLI or debug client directly would expose development surfaces and blur standalone game behavior.

## Decision

Exported games use a minimal dedicated game host that loads one bundled startup manifest and delegates runtime behavior to existing engine libraries.

The host runs graphical mode by default and provides a bounded headless mode for isolated validation and semantic equivalence.

## Consequences

The exported executable has a clear product boundary without requiring an SDK or duplicating the engine runtime. Host command-line functionality remains intentionally small.
