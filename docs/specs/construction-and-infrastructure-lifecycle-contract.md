# Construction and Infrastructure Lifecycle Contract

## Authority

Authoritative for M034 construction plans, material delivery, completion, condition, maintenance, failure, repair, and deconstruction boundaries.

## Construction plan

Required state: stable plan ID, definition, region, footprint, orientation, priority, required/delivered materials, required work, progress, state, revision, and blocking reason.

States:

```text
planned
awaiting-material
ready
constructing
completed
cancelled
blocked
```

Placement validates bounds, terrain, occupancy, conflicting plans, interaction positions, and access.

Delivery uses authoritative inventory, stacks, reservations, and activities. Cancellation deterministically returns delivered material to world stacks or a declared destination.

Construction completion atomically validates plan/material/work, consumes material, creates the structure, retires the plan, and emits factual events. Duplicate completion is rejected.

Completed infrastructure has stable identity, definition, capacity/throughput, operational state, condition, wear policy, maintenance thresholds, failure behavior, region/footprint, and revision.

Wear uses semantic time or declared use. Maintenance and repair use work, materials, reservations, and commands.

Road/path structures modify registered detailed and abstract travel costs through shared authored modifiers.

Persist all authoritative plan, structure, condition, and maintenance state.

Diagnostics:

```text
BUILD-PLACE
BUILD-MATERIAL
BUILD-PROGRESS
BUILD-COMPLETE
BUILD-CANCEL
INFRA-CONDITION
INFRA-MAINTENANCE
INFRA-FAILURE
INFRA-REPAIR
```

Excluded: structural physics, fluid/electrical networks, skills, construction quality, fire/damage simulation, and scripted direct mutation.
