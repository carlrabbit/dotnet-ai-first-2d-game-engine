# Minimal Deterministic Runtime

## Authority

This document is authoritative for the minimal runtime semantics introduced by Milestone 002.

This document is not authoritative for:

- renderer design;
- asset pipeline design;
- map or animation authoring;
- full ECS/component storage strategy;
- source generator strategy;
- packaged runtime representation;
- behavior module APIs beyond the command/event/query principle already defined elsewhere.

## Purpose

The minimal deterministic runtime proves the first executable engine slice:

```text
command input
→ fixed-tick runtime execution
→ factual events
→ queryable final state
→ machine-readable result artifact
```

The implementation may be intentionally small, but it must execute real deterministic behavior rather than only building placeholder projects.

## Required runtime model

The runtime must support these concepts.

| Concept | Required meaning |
|---|---|
| Tick | Non-negative integer simulation step. The first run starts at tick `0`. |
| Entity ID | Stable string-debug identifier for a runtime object, for example `entity.player`. |
| Command | Requested mutation submitted to the runtime. |
| Command result | Accepted or rejected outcome for a submitted command. |
| Event | Factual result emitted by the runtime. |
| Query | Read-only inspection of final runtime state. |
| Diagnostic | Structured warning/error/info entry associated with CLI or runtime behavior. |
| Runtime result | Serializable summary of command execution, events, assertions, final state, and diagnostics. |

## Required smoke behavior

The minimal runtime must support one deterministic smoke scenario.

Required semantic behavior:

```text
initial state:
  entity `entity.player` exists at logical position 0

command:
  move `entity.player` by +1

execution:
  run for N fixed ticks, where N is supplied by the CLI and defaults to 3

expected final state:
  final tick == N
  entity `entity.player` position == 1

minimum events:
  runtime.started
  entity.created
  command.accepted
  entity.moved
  runtime.completed
```

The implementation may include additional events if they are deterministic and documented in the result artifact.

## Tick semantics

The runtime starts at tick `0`.

Running for `N` ticks means:

```text
N must be a positive integer.
After completion, final tick == N.
```

The smoke command may apply the move command during the first executed tick. It must not apply the same move once per tick unless the command name and scenario explicitly describe repeated movement.

## Command semantics

A move command must include:

```text
entityId
amount
```

For the smoke scenario:

```text
entityId = entity.player
amount = 1
```

The runtime must accept the command when:

```text
entity exists
amount is deterministic and valid
runtime is in a state where commands may be applied
```

The runtime must reject a command when:

```text
entity does not exist
amount is invalid
runtime cannot apply the command safely
```

Rejected commands must produce diagnostics or command-result data sufficient to explain rejection.

## Event semantics

Events must be ordered by runtime occurrence.

Each event must include:

```text
event type or ID
tick
message or payload sufficient for the smoke result
```

For the smoke scenario, at minimum:

```text
runtime.started: emitted when execution begins
entity.created: emitted when the smoke entity exists or is created
command.accepted: emitted when the move command is accepted
entity.moved: emitted when player position changes from 0 to 1
runtime.completed: emitted when execution completes
```

## Query semantics

The runtime must expose a read-only query path for the smoke scenario final state.

At minimum, the implementation must be able to query:

```text
current tick
entity position by entity ID
whether expected events occurred
whether the runtime completed without errors
```

The CLI may transform query results into `result.json`.

## Determinism requirements

For the same CLI arguments and same source revision, repeated smoke runs must produce the same meaningful runtime result values.

Must not use:

```text
Random.Shared
DateTime.Now or wall-clock decisions for runtime behavior
network access
filesystem input except writing the requested artifact
thread scheduling as a gameplay/runtime input
async command dispatch inside the simulation tick
reflection-based runtime dispatch
JSON parsing in the runtime hot path
```

Allowed:

```text
System.Text.Json serialization at the CLI/artifact boundary
ordinary file IO to write the requested artifact
simple in-memory collections
small hand-written dispatch for the initial command
```

## Debug-oriented representation

Milestone 002 uses debug-oriented string identifiers.

Examples:

```text
entity.player
runtime.started
command.accepted
entity.moved
```

Do not introduce packaged-runtime integer IDs, binary resource formats, generated registries, or source-generated dispatch in this milestone.

## Runtime result status

The runtime result status must be one of:

```text
passed
failed
error
```

Use:

- `passed` when all smoke assertions pass and no error diagnostics exist;
- `failed` when runtime execution completes but scenario assertions fail;
- `error` when CLI argument handling, runtime execution, or artifact writing fails unexpectedly.

## Required assertions

The smoke result must evaluate at least these assertions:

```text
final tick equals requested tick count
entity.player exists
entity.player position equals 1
runtime.started event exists
entity.created event exists
command.accepted event exists
entity.moved event exists
runtime.completed event exists
```

Assertions must be represented in the result artifact.

## Testing requirements

Milestone 002 implementation tests must prove:

- identical smoke runs produce equivalent meaningful results;
- final tick equals requested tick count;
- the player position changes exactly once from 0 to 1;
- expected events are emitted in deterministic order;
- invalid tick input is rejected by the CLI or command parser;
- result artifact serialization includes required fields.
