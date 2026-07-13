# Pluggable Spatial Runtime Contract

## Authority

Authoritative for the narrow engine-facing spatial-resolution boundary. It does not define a universal map, movement, physics, collision, or navigation model.

## Principle

```text
behavior intent → spatial resolver → accepted command or rejected resolution → runtime
```

## Initial capabilities

- movement-intent resolution;
- spatial-position query;
- authored spatial-semantic query.

The core must not require grid coordinates, velocity, gravity, grounding, raycasts, pathfinding, or tile APIs.

## Registration

Modules use stable IDs and explicit registration. Initial module: `spatial.grid`. Unknown IDs fail validation.

## Resolution

A resolution contains intent ID, module ID, entity ID, accepted/rejected status, stable reason, queried facts, resulting command when accepted, events, and diagnostics. Rejection is a valid domain result.

## Ownership

Spatial state is module-owned. The core transports inspectable state but does not standardize one position model.

## Future compatibility

Continuous top-down and platformer modules must be addable without changing behavior/runtime phase semantics.
