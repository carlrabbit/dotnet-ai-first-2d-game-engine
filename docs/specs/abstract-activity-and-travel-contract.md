# Abstract Activity and Travel Contract

## Authority

Authoritative for M040 discrete-event abstract execution over the shared semantic contract.

Shared semantics:

```text
docs/specs/shared-work-logistics-and-needs-semantics-contract.md
```

Queue semantics:

```text
docs/specs/discrete-event-simulation-contract.md
```

Fidelity switching is outside M040.

## Pipeline

```text
shared semantic state
→ shared derive/select/assign
→ abstract continuation planner
→ one guarded next trigger
→ due semantic instant
→ revalidate
→ shared semantic command
→ committed state/event
→ plan next transition
```

## Abstract continuation

Executor-owned state may include:

- actor/activity reference;
- abstract node/edge;
- origin/destination;
- departure/arrival or due instant;
- graph revision;
- duration-policy inputs/version;
- next-trigger identity.

Gameplay quantities remain in shared typed state.

## Travel

Use a bounded deterministic coarse graph with stable nodes/edges, integer costs, access/revision and explicit modifiers.

Support multi-edge routes.

Ordinary abstract travel must not call detailed grid pathfinding.

## Duration

Typed deterministic models drive actual due instants for travel, harvest, pickup, deposit, eat, drink, rest and bounded retry.

No wall clock, hidden randomness, personality, skill or presentation state.

## Stage execution

A work activity progresses through multiple scheduled semantic stages.

One trigger cannot represent a scripted complete workday.

Delivery revalidates applicable activity, lifecycle, target, reservation, graph, need and revision guards.

Stale/cancelled/duplicate delivery cannot mutate factual success state.

## Needs

Integrate fixed needs lazily from semantic time and schedule threshold transitions.

Mandatory need can invalidate/interupt ordinary work through the shared interruption command.

After satisfaction, shared opportunity derivation/selection runs again.

## Persistence

Persist scheduler and abstract continuation required for deterministic resume.

Fresh-process proof must load and then advance beyond the checkpoint to a common semantic target matching uninterrupted abstract execution.

## Exclusions

No detailed pathfinding, fidelity switching/materialization, cross-mode equivalence tolerance, observer neutrality, cross-region travel, rendering or environmental infrastructure.
