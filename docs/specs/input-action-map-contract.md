# Input Action Map Contract

## Authority

Authoritative for authored input maps, semantic controls, action types, binding validation, combination, and dead-zone processing.

## Principle

Authored gameplay input uses semantic actions and controls. Backend enums and native APIs are adapter concerns.

## Required identities

```text
input-map.player.default
input-source.player-1
action.move
action.interact
action.zoom
```

Physical device identity is distinct from logical input-source identity.

## Action types

```text
digital
scalar
vector2
```

Digital phases:

```text
inactive
pressed
held
released
```

## Required binding kinds

```text
keyboard-key
mouse-button
mouse-wheel-x
mouse-wheel-y
controller-button
controller-axis
controller-stick
composite-vector2
```

## Combination

```text
digital: logical OR
scalar: greatest absolute magnitude, preserve sign, binding-ID ordinal tie-break
vector2: sum, then clamp magnitude to 1.0
```

Binding evaluation is binding-ID ordinal.

## Dead zones

Sticks use radial dead zones and rescale the remaining magnitude to `[0,1]`.

Scalar axes use axial dead zones with equivalent rescaling.

Dead zones must be finite and in `[0,1)`.

## Validation

Reject unsupported schemas, duplicate IDs, incompatible binding/action types, unknown semantic controls, backend enum names, malformed composites, invalid scales/dead zones, non-finite values, and invalid source IDs.

Equivalent maps canonicalize deterministically.
