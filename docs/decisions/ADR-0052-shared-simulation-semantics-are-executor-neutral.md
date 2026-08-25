# ADR-0052 — Shared Simulation Semantics Are Executor-Neutral

## Status

Accepted for M040.

## Context

M032 implemented detailed autonomous work/logistics/needs.

M033 intended to execute the same semantics through discrete events, but instead introduced a separate miniature component model and one synthetic cycle method used by both "detailed" and "abstract" paths.

M039 fixed the lower component/transaction foundation. Before reconciliation can be repaired, both executor strategies must be independently real over one semantic model.

## Decision

### Shared semantics

Work opportunity derivation, worker selection, assignment, activity legality, reservations, resource/inventory/storage rules, fixed needs, interruption/resumption and semantic commands/events are executor-neutral.

Shared rules do not branch on detailed/abstract executor identity.

### Executors own continuation only

Detailed owns grid/path/fixed-step continuation.

Abstract owns abstract graph/duration/scheduled-trigger/lazy-integration continuation.

Both call shared semantic commands at semantic boundaries.

### One gameplay model

Both executors use the same authoritative typed component/game state.

A separate M033/M040 worker/resource/storage/need model is rejected.

### Real abstract DES

Abstract execution schedules one next meaningful transition, revalidates at delivery, commits through shared semantic commands, then plans the next transition.

A scripted daily cycle is not accepted.

### Staged closure

M040 fixes independent executor reality.

M041 owns fidelity transfer/reconciliation.

M042 owns mixed-fidelity equivalence and observer neutrality.

### Evidence

Executor separation and semantic behavior are established by executed observations, not constant artifact claims.

## Consequences

M032 semantic code may require refactoring. The existing scheduler is retained. Synthetic M033 proof logic and duplicate gameplay components must be removed or demoted.

This creates a clean test boundary before transition and equivalence work.
