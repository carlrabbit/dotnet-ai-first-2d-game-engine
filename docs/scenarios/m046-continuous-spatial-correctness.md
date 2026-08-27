# M046 Continuous Spatial Correctness Scenarios

## Required case matrix

### Numeric

```text
zero direction
NaN X
NaN Y
+Infinity
-Infinity
```

### Outcome

```text
unobstructed axial accepted
unobstructed diagonal accepted
axial clipped by object
axial clipped by map bound
diagonal full-axis slide
diagonal partial-axis slide
diagonal both-axes shortened -> clipped
fully blocked
```

### Geometry

```text
several obstacles on one axis
different obstacle limits X and Y
corner approach
narrow corridor
map bounds + static object
blocked cell + static object
equal limiting candidates
initial penetration
```

## Required observations

Each case records initial transform/AABB, requested direction/displacement, deterministic candidate set, per-axis applied displacement, actual constraint source, final transform, no-penetration predicate, outcome, mutation proposal, runtime commit result, factual mutation events and diagnostics.

## Assertion rule

Unknown assertion type must fail rather than pass by fallback.
