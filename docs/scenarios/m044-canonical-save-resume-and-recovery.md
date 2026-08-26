# M044 Canonical Save Resume and Recovery Scenarios

## Purpose

Define the bounded scenario/checkpoint matrix used to prove the M043 canonical persistence service across current engine continuation states.

## Required checkpoint classes

| Checkpoint | Required semantic state | Required continuation |
|---|---|---|
| `typed-world-active-reservation` | typed authoritative components; active activity; active legal reservation | complete activity without duplicate completion or reservation leak |
| `destroyed-entity-tombstone` | entity destroyed/tombstoned | advance beyond possible reconstruction point; entity remains absent |
| `abstract-travel` | M040 abstract travel continuation | finish travel and subsequent semantic work |
| `abstract-carrying` | M040 abstract executor carries authoritative resource | deliver/store resource exactly once |
| `mandatory-need-interruption` | stable mandatory-need interruption continuation | resume need handling and subsequent work |
| `detailed-carrying` | M040 detailed executor carries authoritative resource | deliver/store resource exactly once |
| `immediately-after-materialization` | completed M041 abstract→detailed transition | execute detailed continuation under new epoch |
| `immediately-after-abstraction` | completed M041 detailed→abstract transition | deliver abstract continuation under new epoch |
| `equal-time-trigger-and-switch-boundary` | stable state after M042 same-instant switch/trigger ordering | continue with no stale old-epoch mutation or duplicate trigger |

## Process pattern

For each checkpoint:

```text
A: initial -> target

B: same initial -> checkpoint -> canonical save -> exit

C: load B save -> validate external schedule/policy -> target -> exit

independent comparer: A vs C
```

## Required comparison dimensions

- canonical final world fingerprint;
- semantic clock;
- command/event sequence and IDs;
- ordered semantic facts;
- component authority;
- activity/reservation state;
- resource/storage/inventory/needs state;
- tombstones;
- scheduler continuation;
- fidelity owner/epoch/transition revision when applicable.

## Product flow

One bounded scenario covers:

```text
manual save
→ later autosave
→ invalid newer candidate
→ process exit
→ Continue selection
→ canonical load
→ semantic advance
```

with M037 ordering/retention rules.

## Recovery flow

One bounded scenario writes a previous-good canonical save, attempts a later replacement, injects corruption, recovers the previous-good save, launches a new consumer, and continues to a deterministic target.

## Prohibited shortcuts

- same-process load as fresh-process evidence;
- loading without continuing;
- comparing only producer-authored booleans;
- omitting event IDs from equivalence;
- persisting half a fidelity transition;
- creating a special serializer for the scenarios.
