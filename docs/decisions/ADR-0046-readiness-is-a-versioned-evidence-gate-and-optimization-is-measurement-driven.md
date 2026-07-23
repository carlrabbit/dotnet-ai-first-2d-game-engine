# ADR-0046 — Readiness Is a Versioned Evidence Gate and Optimization Is Measurement-Driven

## Status

Accepted for M035 implementation.

## Context

M034 provides a coherent internal-play game loop, but bounded milestone proofs do not establish readiness for sustained failure-oriented testing.

The project needs a clear point where “works in scenarios” becomes “fit for heavy internal testing.” It also needs to avoid speculative ECS, threading, and storage rewrites.

## Decision

Heavy internal testing readiness is a versioned evidence gate tied to an explicit support envelope.

Readiness requires automated campaign verification and milestone-owned human approval.

Performance optimization is authorized only by measured failed budgets or material regressions and must preserve semantic equivalence.

## Consequences

### Positive

- readiness claims are reproducible and bounded;
- failures produce triage evidence;
- optimization effort targets actual constraints;
- future changes can define new readiness envelopes without rewriting history;
- testers receive a supported workflow.

### Negative

- long campaign execution and evidence maintenance are required;
- some performance results are host-class-specific;
- retained reference saves and compatibility policy add maintenance cost.

## Rejected alternatives

- subjective “seems stable” readiness;
- one monolithic non-resumable soak;
- silent baseline replacement;
- speculative archetype/multithreading rewrite;
- treating human play alone as semantic validation;
- permanent project-wide approval that automatically applies to future versions.
