# Settlement Operations Surface Contract

## Authority

Authoritative for M034 operations projections, commands, alerts, journal, and graphical interaction.

Principle:

```text
read-only projection
+ explicit validated command input
```

World projection shows regions, fidelity, population/work availability, key reserves, alerts, and save/time state.

Region projection shows plans, designations, infrastructure, stocks, capacities, recent flows, backlog, blocked work, needs, maintenance, alerts, and journal.

Commands support plan/cancel, designation changes, priority/reserve policy, enable/disable, region activation, time control, save, and load.

Alerts contain stable key, severity, status, region/entity, first/last instant, explanation, cause references, and suggested action category. Alerts derive from authoritative state/events.

Journal is bounded recent factual history with stable references.

Explanations cover worker selection, blocked plans, full storage, idle producers, stalled crops, degraded/failed structures, and alert state.

Use existing semantic input and backend-neutral rendering. Ordinary operation must remain RDP-safe where the current client supports it.

Simulation policies persist in world state; local UI preferences are non-authoritative.

Diagnostics:

```text
OPS-PROJECTION
OPS-COMMAND
OPS-ALERT
OPS-JOURNAL
OPS-NAVIGATION
```
