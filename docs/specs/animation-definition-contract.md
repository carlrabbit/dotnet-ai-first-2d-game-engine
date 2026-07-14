# Animation Definition Contract

## Authority

Authoritative for authored animation definitions, clips, tracks, keyframes, sprite-sequence shorthand, property registry, validation, and canonical compilation.

## Model

```text
animation definition → clips → tracks → keyframes + presentation markers
```

Time uses integer runtime ticks. Loop modes are `once` and `repeat`. Track value types are `scalar` and `asset-region`. Interpolation is `step` or `linear`; asset-region supports step only.

## Property registry

```text
visual.region
visual.offset.x
visual.offset.y
visual.scale.x
visual.scale.y
visual.rotation-degrees
visual.tint.red
visual.tint.green
visual.tint.blue
visual.opacity
```

Targets use stable visual part IDs. Arbitrary object/property paths are forbidden.

## Time boundaries

`durationTicks > 0`. Valid keyframe/marker ticks are `0 .. durationTicks - 1`. Every track requires a keyframe at tick zero.

## Sprite shorthand

`kind: sprite-sequence` expands to one canonical `visual.region` step track. Canonical compiled definitions are runtime sampling authority.

## Validation

Reject unresolved visual/part/region IDs, duplicate IDs, duplicate targets in a clip, invalid ranges, non-finite values, unsupported interpolation/type/property combinations, and gameplay/runtime targets. Canonical output and fingerprints are deterministic.
