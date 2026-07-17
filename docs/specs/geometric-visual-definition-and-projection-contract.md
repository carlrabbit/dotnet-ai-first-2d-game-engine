# Geometric Visual Definition and Projection Contract

## Authority

Authoritative for bounded geometric visual parts, validation, canonicalization, backend-neutral render projection, ordering, fingerprints, and graphical adapter behavior.

## Boundary

Geometry is presentation-only.

A geometric visual never defines:

- entity identity;
- collision;
- spatial bounds;
- interaction;
- lifecycle;
- behavior;
- gameplay state.

Gameplay and collision geometry must remain explicitly authored through their own contracts.

## Source kinds

A visual part uses exactly one source kind:

```text
asset-region
geometry
```

## Supported geometry kinds

```text
circle
rectangle
triangle
diamond
regular-polygon
ring
line
```

## Common fields

- stable part ID;
- geometry kind;
- anchor;
- finite offset;
- finite positive world size;
- finite rotation;
- fill color where applicable;
- optional outline color and width;
- opacity;
- layer;
- integer order;
- sort mode;
- provenance.

## Bounded geometry-specific fields

- regular-polygon side count: validated finite integer range;
- ring: validated inner ratio or thickness;
- line: validated endpoints or length/width representation;
- outline width: finite and non-negative.

Unsupported or meaningless field combinations fail validation.

## Canonicalization and determinism

Equivalent definitions produce equivalent:

- normalized geometry records;
- render-item ordering;
- fingerprints;
- diagnostics.

Colors, angles, dimensions, and ratios use the repository's canonical finite-number policy.

## Rendering

Headless artifacts retain semantic geometry kind and normalized parameters.

Graphical adapters translate geometric render commands without becoming authority.

Geometry must render in the debug client and exported Linux game host.
