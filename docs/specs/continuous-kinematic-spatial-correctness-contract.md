# Continuous Kinematic Spatial Correctness Contract

## Authority

Authoritative for `spatial.continuous-kinematic-2d` input validity, deterministic axis-separated AABB resolution, outcome classification, constraint-source evidence and runtime-mutation linkage.

## Runtime boundary

The resolver reads an immutable M045 runtime snapshot plus authored static geometry, returns a domain resolution and optional mutation proposal, and never mutates `EntityComponentWorld`.

## Inputs

Direction, transform, movement policy and collision extents are finite. Non-finite intent rejects before movement success can be reported. Zero finite direction is `no-op`.

## Movement

For one fixed tick:

```text
normalize desired direction
× MaxSpeed
→ requested displacement
→ resolve X
→ resolve Y
```

X precedes Y.

## Motion state

Current reference `KinematicMotion2` represents MaxSpeed movement policy. Do not retain unused velocity fields as authoritative state.

## Axis result

Each axis reports requested/applied displacement, whether constrained, and the actual constraining source. Closest valid limiting boundary wins. Equivalent-limit ties use stable ordinal source ID.

## Outcome

`accepted`: applied equals requested.

`blocked`: requested is nonzero and applied is zero on both axes.

`slid`: diagonal request with constraint on one axis/relative axis progress while meaningful movement continues along the other requested axis.

`clipped`: nonzero safe movement shorter/constrained without slide semantics, including both-axis shortening.

`no-op`: zero request.

No nonzero safe displacement is classified blocked.

## Collision validity

Final AABB remains within map bounds and outside blocked/static AABBs under the documented touch/epsilon policy. Starting penetration rejects; no depenetration is provided.

## Evidence

Resolution records actual initial state, request, candidate geometry, per-axis limiting sources, applied displacement, result, outcome, diagnostics and optional mutation proposal. Factual transform-change is emitted only after successful M045 runtime commit.

## Scenario assertions

Unsupported assertion types fail. No default-success fallback is permitted.
