# M031 Headless Wood Workflow

## Purpose

Provide bounded dogfood proving the M031 capability-provider foundation.

This scenario is not autonomous gameplay and is not a substitute for later work-coordination, pathfinding, abstract simulation, or multi-fidelity milestones.

## Scenario identity

```text
scenario.m031.simulation-foundation.wood-workflow
```

## Initial state

World:

```text
world.m031.proof
simulation instant: 08:00:00
```

Regions:

```text
region.forest
region.settlement
```

Entities:

```text
worker.001
  region: region.forest
  inventory capacity: at least 3 wood
  available: true

tree.001
  region: region.forest
  harvestable wood: 3
  active: true

storage.001
  region: region.settlement
  accepted resource: wood
  available capacity: at least 3
```

Initial conservation:

```text
tree wood: 3
worker wood: 0
storage wood: 0
active wood reservations: 0
```

## Activity

```text
activity.harvest-and-deposit.001
actor: worker.001
target: tree.001
destination: storage.001
```

Required stages:

```text
planned
target-reserved
at-target
harvesting
harvested
storage-capacity-reserved
carrying
at-storage
deposited
completed
```

The implementation may use additional internal stages, but artifacts must project these semantic checkpoints or a documented equivalent.

## Command sequence

A deterministic proof driver issues commands equivalent to:

1. create activity;
2. reserve `tree.001`;
3. start activity;
4. mark arrival at tree;
5. begin harvest;
6. complete harvest for three wood;
7. reserve three units of storage capacity;
8. begin carrying;
9. transfer worker from `region.forest` to `region.settlement` while preserving identity;
10. mark arrival at storage;
11. deposit three wood;
12. release tree and capacity reservations;
13. complete activity.

Commands use expected activity/reservation revisions.

## Save/load branch

Create two execution branches from the same initial authored input.

### Direct branch

Execute all commands in one fresh process.

### Round-trip branch

Execute through `harvested`, then:

1. write canonical save;
2. terminate the process;
3. load into a fresh process;
4. validate activity, inventory, regions, reservations, clock, and revisions;
5. execute remaining commands;
6. issue one additional deterministic inspection/no-op-safe continuation command if required to prove dispatch continuity.

## Required events

Artifacts must contain factual events equivalent to:

```text
ActivityCreated
ReservationAcquired
ActivityStarted
ActivityStageChanged
ResourceHarvested
EntityTransferredRegion
ResourceDeposited
ReservationReleased
ActivityCompleted
```

No successful factual event may exist for a failed or rolled-back command.

## Required assertions

### Identity

- stable world, region, entity, activity, and reservation IDs;
- `worker.001` retains identity during region transfer;
- no entity exists in two regions.

### Conservation

Final state:

```text
tree wood: 0
worker wood: 0
storage wood: 3
active wood reservations: 0
active storage-capacity reservations: 0
```

No duplicated or missing wood.

### Activity

- exactly one activity completes;
- stage revisions are monotonic;
- duplicate completion is rejected without mutation;
- all successful-path reservations are released;
- completion contains causal references to the command chain.

### Persistence

- direct and round-trip branches end with the same canonical world fingerprint;
- save contains no presentation/native/external-handle state;
- load occurs in a fresh process;
- malformed and unknown-version variants fail transactionally.

### Determinism

Repeat each branch at least twice from the same inputs.

Each repetition produces the same:

- command results;
- factual event sequence;
- final state;
- canonical fingerprint.

## Negative variants

Required focused variants:

1. second activity competes for `tree.001`;
2. stale activity revision attempts a stage transition;
3. duplicate harvest completion;
4. tree destroyed after reservation and before harvest;
5. insufficient storage capacity;
6. failed region transfer;
7. save during each semantic stage;
8. unknown persistent component key;
9. malformed reservation reference;
10. interrupted atomic save.

Each variant must produce stable diagnostics and preserve invariants.

## Artifacts

Write under:

```text
artifacts/simulation/M031/wood-workflow/
```

Required outputs are defined in `docs/artifacts/simulation-foundation-artifact-contract.md`.

## Non-goals

- automatic worker selection;
- pathfinding or travel-time estimation;
- real-time movement;
- scheduled event queue;
- rendering;
- audio;
- player UI;
- economic balancing.
