# ADR-0002 — Headless-first and artifact-first

## Status

Accepted.

## Decision

The engine must be operable through CLI/API commands and must produce structured diagnostics and artifacts.

## Consequences

- Product commands must define machine-readable outputs.
- Failures must produce evidence.
- Agents should inspect structured artifacts rather than guess from source or screenshots alone.
