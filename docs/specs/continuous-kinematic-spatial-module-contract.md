# Continuous Kinematic Spatial Module Contract

## Authority

This document is authoritative for the first continuous kinematic 2D spatial reference module.

It is not a rigid-body physics contract.

## Module identity

```text
spatial.continuous-kinematic-2d
```

Registration is explicit.

## Owned component types

```text
ContinuousTransform2
KinematicMotion2
CollisionAabb2
```

Stable component IDs:

```text
component.continuous-transform-2d
component.kinematic-motion-2d
component.collision-aabb-2d
```

The runtime owns component instances. This module owns their semantics.

## Numeric policy

Use finite `double` values.

Reject:

```text
NaN
positive infinity
negative infinity
non-positive collision half-extents
negative max speed
```

Normalize serialized negative zero to `0.0`.

Evidence should use one documented invariant numeric format.

## World units and timestep

One tile equals one world unit in each axis.

Movement uses deterministic fixed-tick integration. Wall-clock elapsed time is prohibited.

The contract may define a normalized tick duration of `1.0` simulation unit or another explicit constant. The same value must be used consistently in tests, scenarios, and evidence.

## Intent

Initial intent:

```text
ContinuousMoveIntent
```

Required semantic fields:

- stable intent ID;
- target entity ID;
- requested direction or displacement;
- behavior assignment ID;
- deterministic ordering key.

The intent does not mutate transform or motion state.

## Required entity components

The target must have:

- `ContinuousTransform2`;
- `KinematicMotion2`;
- `CollisionAabb2`;
- compatible `SpatialMembership`.

Missing required state produces a rejected domain resolution with stable diagnostics.

## Resolution flow

```text
read snapshot components
→ normalize/clamp requested motion
→ calculate requested displacement
→ collect static AABB candidates
→ resolve X axis
→ resolve Y axis
→ classify outcome
→ return component mutation command or rejection
```

Axis order is:

```text
X, then Y
```

Changing axis order is a semantic change requiring an updated contract and tests.

## Static collision sources

Initial sources:

- map bounds;
- blocked map-cell rectangles;
- static map-object AABBs.

Dynamic entity/entity collision is excluded.

## Outcome classification

### accepted

Requested displacement is applied exactly.

### clipped

Applied displacement is non-zero but smaller than requested because of collision or bounds.

### slid

One axis is constrained while a non-zero displacement is applied on the other axis.

### blocked

No valid displacement is applied.

All outcomes are normal domain resolutions.

## Penetration rule

Final entity AABB must not overlap any blocking static AABB or lie outside map bounds.

Touching boundaries is allowed according to one consistently implemented inclusive/exclusive policy.

## Events

Recommended IDs:

```text
spatial.continuous-movement-accepted
spatial.continuous-movement-clipped
spatial.continuous-movement-slid
spatial.continuous-movement-blocked
entity.continuous-transform-changed
```

## Diagnostics

Recommended IDs:

| ID | Meaning |
|---|---|
| `CONTINUOUS0001` | Required component missing. |
| `CONTINUOUS0002` | Invalid numeric component or intent value. |
| `CONTINUOUS0003` | Spatial membership incompatible. |
| `CONTINUOUS0004` | Map/static world unavailable. |
| `CONTINUOUS0005` | Initial entity shape penetrates static geometry. |
| `CONTINUOUS0006` | Resolution produced invalid final state. |
| `CONTINUOUS0007` | Unsupported continuous movement intent. |

## Determinism

Equivalent snapshot, intent, fixed timestep, and static world must produce equivalent:

- collision candidates;
- axis resolution;
- applied displacement;
- outcome status;
- commands;
- events;
- diagnostics;
- final transform.
