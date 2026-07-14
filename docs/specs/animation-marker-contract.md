# Animation Marker Contract

## Authority

Authoritative for authored presentation markers and deterministic marker occurrences.

## Boundary

Markers are presentation-only. They must not mutate runtime state, issue gameplay commands, deal damage, alter collision, spawn gameplay entities, or progress interactions/quests.

## Definition

A marker contains stable ID, clip-local tick, registered kind, and structured payload. Initial kinds may include `presentation.footstep`, `presentation.effect`, `presentation.debug`, and `presentation.animation-complete`. No marker is generated automatically.

## Crossing

Emit markers crossed in `(previous local position exclusive, current position inclusive)`. Tick-zero markers emit once on first sampling. Multi-tick advances emit all occurrences. Repeat-loop ordering is loop iteration, marker tick, marker ID ordinal.

Occurrence evidence includes source, animation definition, layer, clip, selection key, marker, runtime observation tick, local tick, loop iteration, and payload.
