# Simulation and Settlement Evolution — Reconstructed Planning Context

## Status

Current reconstructed planning knowledge; non-authoritative.

## Question

What simulation/gameplay capability is already established, which historical assumptions have been superseded, and what future planning should build on rather than recreate?

## Reconstructed capability progression

### Foundation

The engine moved from a deterministic runtime and semantic command/event model into a persistent simulation world with explicit activities, reservations, region ownership, semantic time, inspection, persistence, and deterministic evidence.

That foundation is now project authority. Future planning should not treat simulation persistence, activity identity, or runtime mutation ownership as open greenfield questions.

### Autonomous detailed-region simulation

M032 established the first coherent autonomous detailed-region loop:

```text
player designation/policy
→ derived work opportunities
→ deterministic worker evaluation
→ atomic assignment/reservation
→ detailed path/travel/activity execution
→ logistics
→ needs interruption
→ explanation/evidence
```

Important planning conclusion:

Potential work is derived inspectable state. Accepted work becomes authoritative activity/reservation state. Detailed path/progress is an execution strategy, not shared domain truth.

Direct per-worker player assignment was deliberately rejected as the normal game-control model.

### Multi-fidelity simulation

M033 introduced the defining multi-region thesis:

```text
one persistent semantic world
├── detailed fixed-step executor for the active region
└── standalone-capable discrete-event executor for inactive regions
```

Region fidelity and executor ownership are explicit authoritative state.

Shared rules are intended to be executor-neutral. Fidelity switching is not a second game simulation; it is a change in execution strategy over one semantic world.

Later corrective work M040–M042 materially strengthened this claim by:

- moving shared rules out of detailed-only behavior;
- making the abstract executor real rather than a shortcut façade;
- making fidelity ownership/reconciliation transactional;
- defining exact and bounded equivalence classes;
- requiring observer-neutrality and fresh-process continuation proof.

Historical assumption superseded:

"Detailed execution is the real game and abstract execution is an approximation layer around it" is too weak. Current direction is one semantic model with multiple execution strategies and explicit equivalence policy.

### Settlement infrastructure and indirect control

M034 built the first coherent settlement game loop:

```text
plans/designations/policies
→ construction + resource production
→ water/food/storage/shelter
→ wear/maintenance/failure
→ autonomous work and needs
→ alerts/operations projections
→ player prioritization and repair
```

This established:

- multi-region settlement persistence;
- construction lifecycle;
- environmental resource production and conservation;
- farming/growth;
- finite comfort/shelter capacity;
- infrastructure condition/wear/repair;
- roads affecting detailed and abstract travel;
- an operations surface based on projections + commands;
- indirect player control rather than direct worker micromanagement.

The operations UI is a read-only projection/command surface over domain state, not a second simulation authority.

### Scale and internal-testing readiness

M035 treated M031–M034 as feature-complete for bounded internal play and focused on:

- declared supported scale envelopes;
- performance/capacity measurements;
- continuous invariant monitoring;
- deadlock/livelock/starvation diagnostics;
- long-horizon soak/stress/fidelity-switch campaigns;
- save compatibility/recovery;
- reproduction bundles and tester workflows.

Planning implication:

Future gameplay milestones should assume there is already a substantial settlement simulation to extend. They should not reopen foundational queue/activity/persistence/fidelity choices without explicit contradictory evidence.

### Player-facing application shell

M037 wrapped the simulation in a coherent player application:

- main menu and pause flow;
- New/Continue/Load/Tutorial entry;
- save catalog/manual saves/autosaves;
- settings/display rollback/safe mode;
- remappable semantic input;
- player/diagnostics client separation;
- a retained UI foundation that is now itself being reconsidered by the M052–M054 agent-oriented UI track.

The target state after M037 was explicitly "player-shell-capable and ready for terrain/map presentation work."

## What remains intentionally simple

The current simulation direction deliberately avoided or bounded:

- personalities/traits/relationships;
- skills and individualized schedules;
- health/disease/combat as settlement systems;
- research/economy/trade;
- cross-region hauling/trade;
- complex environmental networks;
- seasons/weather/temperature;
- direct worker control;
- multiple simultaneously detailed regions;
- arbitrary simulation plug-ins.

These exclusions are historical design boundaries, not permanent prohibitions. Reopening one requires a concrete gameplay/product reason.

## Durable planning lessons

### Indirect control is part of the game identity

The settlement loop is built around plans, designations, priorities, reserves, infrastructure and explanation rather than direct assignment.

A future feature that relies primarily on selecting individual workers and issuing direct tasks would materially change the game-control thesis and should be treated as an explicit design decision.

### Explainability is not just diagnostics

Worker choice, blocked work, shortages, maintenance and fidelity transitions were designed to be inspectable because an autonomous settlement is otherwise difficult for both humans and agents to reason about.

Future gameplay systems should expose semantic causes, not only visible symptoms.

### Detailed versus abstract should remain a strategy distinction

New domain rules should normally be defined once and executed by both strategies where applicable.

Do not introduce a new subsystem whose semantics exist only in the detailed executor unless it is explicitly presentation/local-detail-only.

### Presentation is downstream of committed simulation state

Animation, sound, UI and rendering should explain or present authoritative state/events. They should not become hidden repair mechanisms for simulation.

## Open planning surfaces

The main future simulation/game questions are no longer foundational plumbing. They are product/game design questions such as:

- what terrain/world structure should the settlement inhabit;
- whether procedural generation materially improves the intended game;
- how richer population dynamics should work;
- whether departure/abandonment becomes a meaningful consequence model;
- how richer individuality, economy, research, social systems or trade fit the indirect-control thesis;
- which of these are useful engine-provider capabilities versus bounded game dogfood.

## Related authority

- `docs/architecture/simulation-foundation-architecture.md`
- `docs/architecture/autonomous-detailed-region-execution-architecture.md`
- `docs/architecture/multi-fidelity-simulation-architecture.md`
- `docs/specs/multi-fidelity-equivalence-contract.md`
- `docs/specs/runtime-snapshot-and-mutation-authority-contract.md`
- `docs/specs/canonical-runtime-persistence-contract.md`
- M032–M035 and corrective M039–M046 authority

## Freshness

Last reconstructed: 2026-10-12.

Revalidate when a future gameplay milestone changes indirect control, fidelity count/ownership, population semantics, or the settlement's core game loop.
