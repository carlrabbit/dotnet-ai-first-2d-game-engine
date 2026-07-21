# Deferred synchronization hint — M031 simulation foundation

## Classification

```text
guide-system metadata only
ordinary implementation agents: must not read
owning implementation milestone: none
source milestone: M031
```

## Project truth introduced by M031 planning

M031 defines a reusable capability-provider foundation with:

- one authoritative world and explicit region partitions;
- deterministic game-defined component registration;
- semantic simulation time;
- commands as mutation requests and domain events as completed facts;
- explicit activities and reservations;
- canonical persistence, inspection, fingerprints, and artifacts;
- explicit compile-time composition;
- a later optional standalone discrete-event subsystem;
- bounded headless dogfood rather than a complete consumer game.

## Potential future synchronization topics

When a dedicated documentation-synchronization task is scheduled, assess whether the external guide system needs generalized guidance for:

- broad AI-executed simulation-foundation milestones;
- capability-provider versus bounded dogfood scope;
- semantic separation of commands, factual events, and future scheduled triggers;
- milestone-scoped architecture/semantic review;
- resumable validation fingerprints that include review state;
- direct-versus-round-trip equivalence evidence;
- explicit exclusion of speculative ECS rewrites.

## Completion criteria

This hint may be removed only by a dedicated documentation-synchronization task that:

1. compares current guide material against the generalized lessons;
2. updates the external guide repository where warranted or records that no change is needed;
3. does not copy guide documents into the target repository;
4. records the synchronization disposition.

M031 implementation does not depend on this file.
