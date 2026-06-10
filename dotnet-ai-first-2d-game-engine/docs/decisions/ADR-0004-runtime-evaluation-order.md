# ADR-0004 — Runtime evaluation order

## Status

Accepted as initial direction.

## Decision

Evaluate runtime foundations in this order:

1. raylib-cs spike for rapid proof.
2. MonoGame prototype for serious .NET 2D runtime viability.
3. SDL3/Silk.NET only if lower-level control is required.

## Consequences

Runtime packages are not added during repository initialization. Runtime choice is milestone-driven.
