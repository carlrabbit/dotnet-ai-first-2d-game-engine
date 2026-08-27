# ADR-0058 — Continuous Kinematic Resolution Classifies Safe Displacement Truthfully

## Status

Accepted for M046.

## Context

The historical M013 resolver can classify nonzero partial movement on both axes as blocked, can attribute a collision constraint to the wrong obstacle, and can produce a success-looking resolution for non-finite input that the runtime later rejects.

## Decision

Continuous resolution validates input finiteness before calculation.

The reference module uses deterministic X-then-Y AABB resolution and returns the actual limiting source from the axis algorithm.

`blocked` means a nonzero requested displacement has zero valid applied displacement. Any nonzero safe applied displacement is accepted, slid or clipped. Zero direction is a distinct no-op.

`KinematicMotion2` represents coherent MaxSpeed movement policy rather than unused velocity fields.

A factual transform-changed event is emitted only after the M045 runtime mutation proposal commits successfully.

Unsupported scenario assertions fail rather than defaulting to pass.

## Consequences

Collision evidence becomes suitable for agent diagnosis; valid clipped movement is not discarded; resolver/runtime evidence cannot contradict each other on invalid numeric input; current smoke content may require a bounded component-shape update; and the module remains a kinematic reference rather than a physics engine.
