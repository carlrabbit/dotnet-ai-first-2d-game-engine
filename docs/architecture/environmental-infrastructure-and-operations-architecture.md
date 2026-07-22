# Environmental Infrastructure and Operations Architecture

```text
Authored structure/resource definitions
→ construction plans and designations
→ derived work opportunities
→ M031–M033 activities/executors
→ authoritative infrastructure/resource state
→ alerts, flows, backlog, journal projections
→ read-only operations UI + commands
```

Boundaries:

- infrastructure is domain state;
- work remains shared autonomous work;
- detailed and abstract executors use identical semantic commands;
- UI reads projections and issues commands;
- alerts derive from state/events;
- rendering never repairs or advances state.

Reusable conceptual families: construction, infrastructure lifecycle, producer/consumer/storage, settlement production, maintenance, operations projections, alerts/journal.

State changes invalidate work opportunities, reserve alerts, flow estimates, maintenance demand, and linked detailed/abstract travel costs.

Persist domain/policy state. Rebuild dashboards, recent-window flow summaries, and presentation projections.

Invariants:

1. quantities conserved;
2. plans consume only delivered material;
3. structures complete once;
4. one rule across fidelities;
5. UI does not own state;
6. alerts are explainable;
7. failures are explicit;
8. M035 owns broad hardening.
