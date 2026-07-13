# Entity Definition and Instantiation Contract

## Authority

This document is authoritative for authored entity definitions, entity spawns, override precedence, bundle validation, deterministic runtime instantiation, and provenance.

## Identities

Three identities are distinct:

```text
definitionId
spawnId
entityId
```

They must be authored explicitly and must not be implicitly derived from one another.

## Entity definitions

A definition describes reusable defaults.

It may contain:

- stable definition ID;
- controlled semantic tags;
- component defaults;
- zero or one default behavior assignment;
- interaction capability components;
- optional opaque visual asset reference.

It must not contain:

- runtime entity ID;
- map/world position;
- current mutable transform;
- scenario-specific state;
- inheritance reference.

Definitions are complete. Inheritance and fragments are unsupported.

## Spawn contract

A spawn selects:

- definition ID;
- spawn ID;
- runtime entity ID;
- source map/scenario;
- whole-component overrides.

Maps and scenarios use one semantic spawn model.

## Override precedence

```text
definition defaults
→ map spawn overrides
→ scenario overrides
```

Each override replaces one whole component value.

Partial field patches and arbitrary JSON Patch operations are unsupported.

The final bundle must be validated after all layers are applied.

## Bundle compatibility

Validation is explicit and hand-written.

Required initial relationships include:

- kinematic motion requires continuous transform;
- continuous collision shape requires continuous transform;
- trigger volume requires continuous transform;
- interactable requires continuous transform;
- continuous movement behavior requires transform, motion, collision shape, and continuous spatial membership;
- grid position requires grid spatial membership;
- ambiguous simultaneous grid and continuous spatial authority is rejected by default.

## Transactional instantiation

Required sequence:

```text
resolve definition
→ merge overrides
→ validate complete bundle
→ validate behavior
→ generate commands
→ apply commands atomically for the spawn
```

Generated command order:

```text
CreateEntity
→ provenance
→ components ordered by stable component type ID
→ behavior assignment
```

Definition and spawn loaders must not mutate runtime stores directly.

## Failure policy

Invalid definition, spawn, override, component bundle, or behavior assignment rejects instantiation.

Rejected instantiation must not leave a partially created entity or partial component set.

World-level initial loading should validate all required spawns before commit where practical. Any weaker atomicity boundary must be explicit in evidence and documentation.

## Provenance

Every instantiated entity retains immutable provenance:

- definition ID;
- spawn ID;
- source kind;
- source map/scenario ID;
- source location/path when stable and portable;
- override summary;
- behavior source.

Provenance is inspectable but not gameplay-mutable.

## Dynamic instantiation

The same service supports a focused runtime spawn/despawn test.

No pooling, respawn policy, waves, or scheduler is defined.

## Determinism

Equivalent definitions, spawns, overrides, runtime revision, and command inputs must produce equivalent:

- merged bundles;
- validation results;
- generated command order;
- component values;
- behavior assignment;
- provenance;
- events;
- diagnostics;
- final runtime state.
