# Environmental Resource and Flow Contract

## Authority

Authoritative for M034 water and bounded environmental production, storage, consumption, thresholds, and flow evidence.

Use integer or explicitly scaled fixed-point quantities. Reservations never create quantity.

A producer defines resource kind, source requirement, cycle/rate, output, capacity/blocking conditions, enabled/operational/condition state, and next threshold/production trigger.

Storage defines accepted kind, capacity, quantity, reservations, enabled/condition state, and interaction rules.

A consumer defines demand, threshold, priority, quantity, and shortage behavior.

Bounded water flow:

```text
source
→ collector
→ stack/container
→ haul
→ storage
→ consume
```

Policies: minimum reserve, desired reserve, and capacity target.

Conservation:

```text
source extraction
= loose + carried + stored + consumed + explicitly lost
```

M034 has no implicit loss.

Abstract mode schedules production, full/empty, reserve, and need thresholds. Detailed mode uses the same semantic commands.

Diagnostics:

```text
FLOW-SOURCE
FLOW-PRODUCER
FLOW-STORAGE
FLOW-CONSUMER
FLOW-RESERVE
FLOW-SHORTAGE
FLOW-CONSERVATION
```
