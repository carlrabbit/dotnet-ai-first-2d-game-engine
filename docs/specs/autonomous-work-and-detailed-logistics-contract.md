# Autonomous Work and Detailed Logistics Contract

## Authority

Authoritative for M032 designations, work-opportunity derivation, generic worker selection, logistics, fixed basic needs, and decision explanation. M031 identity, time, commands/events, activities, reservations, persistence, and inspection remain authoritative.

## Model

```text
world facts + designations + fixed policy
→ derived opportunities
→ deterministic selection
→ atomic reservations/activity
→ detailed executor
→ authoritative command outcome
```

## Designations

A designation has stable ID, kind, region, canonical cells/shape, priority, enabled state, revision, timestamps, and source. Required kinds: extraction, storage, farmland-definition, construction-definition. Only extraction/storage execute fully in M032.

Operations use commands/events. Compatible overlap may coexist; conflicts require explicit precedence/diagnostics. Overlap cannot duplicate work. Screen/pointer state is not authority.

## Work opportunities

Derived inspectable records contain deterministic key, family, region, target/destination, quantity, source designation/policy, priority, requirements, prerequisites, derivation fingerprint, and blocking reason.

Families: harvest, haul, deposit, eat, drink, rest.

Regeneration is deterministic and cannot duplicate authoritative state. Opportunities are not persistent ECS entities by default.

## Generic workers and eligibility

Workers differ only by identity and runtime mechanical state. Selection cannot use personality, preference, mood, relationship, skill progression, hidden randomness, or individual player priority.

Eligibility requires active/available status, active region, capability, valid target/destination, reservability, potential reachability, need permission, and matching revisions.

Evaluation is read-only until assignment.

## Selection

Use explicit inspectable ordering:

1. mandatory need class;
2. emergency/system blocking class if bounded policy introduces one;
3. designation/work priority;
4. continuation/interruption cost;
5. estimated path cost;
6. stable opportunity key;
7. stable worker ID where needed.

Prefer typed integer/lexicographic factors over floating-point utility scores.

## Assignment transaction

Atomically revalidate opportunity/worker, acquire target/quantity/capacity reservations, create/start M031 activity, bind worker, emit facts, and invalidate derived opportunities. Failure commits nothing.

## Decision explanation

Schema: `agentic2d.work-decision-explanation.v1`.

Include worker, instant, current activity, selected opportunity or idle reason, ordered candidates, eligibility, factor values, rejection codes, tie-break, reservations, path estimate, interruption decision, and causal references.

## Interruption

Causes: mandatory need, designation invalidation, target/destination invalidation, blocked route, worker deactivation, system cancellation.

Interruption uses commands, preserves/releases reservations by rule, preserves carried inventory, increments revision, emits facts, and exposes replanning/resumption. Completed/cancelled activities cannot leak reservations.

## Logistics

Harvest targets contain resource kind, integer quantity, interaction requirement, depletion behavior, and reservations. Completion validates and transfers integer quantity once.

Resource stacks have stable entity, kind, integer quantity, region, position, lifecycle, and quantity reservations. Pickup atomically transfers quantity into worker inventory subject to capacity.

Storage has stable identity/designation authority, cells/interaction positions, acceptance policy, integer capacity, contents, capacity reservations, and enabled state.

Available capacity is capacity minus stored quantity minus active reservations. Deposit validates compatibility, capacity, inventory, activity, and destination atomically.

## Conservation

For each resource kind:

```text
source + loose + carried + stored + consumed
= initial + explicitly produced
```

Reservations never add quantity. Artifact validation calculates conservation independently.

## Fixed needs

Kinds: food, water, comfort. State includes integer level, last integrated instant, warning/mandatory thresholds, satisfaction target, and revision. All generic workers use one fixed authored policy.

Accumulation uses semantic time and deterministic integration. Need opportunities use the common selector, outrank work at mandatory threshold, and reserve finite source/capacity. Satisfaction occurs through commands/events.

At mandatory threshold, interrupt at the next allowed semantic boundary; bounded commit stages may finish. Release target reservations unless safe continuation requires otherwise; preserve carried inventory; re-evaluate after satisfaction.

No death, health, mood, preference, or player-controlled thresholds.

## Persistence classification

Authoritative: designations, capabilities, needs, sources/stacks, inventory, storage, activities, reservations.

Derived/rebuildable: opportunities, candidate lists, path estimates, explanation caches.

Transient/derived: exact route, search frontier, animation, pointer drag state.

## Diagnostics

Stable categories: `WORK-DESIGNATION`, `WORK-DERIVATION`, `WORK-ELIGIBILITY`, `WORK-SELECTION`, `WORK-INTERRUPTION`, `LOGISTICS-SOURCE`, `LOGISTICS-PICKUP`, `LOGISTICS-STORAGE`, `LOGISTICS-CONSERVATION`, `NEED-INTEGRATION`, `NEED-SOURCE`.

## Non-goals

No abstract scheduling, cross-region execution, personalities/skills, direct assignment, production networks, construction execution, advanced economics, or forest-specific reusable APIs.
