# Minimal Runtime Scenarios

## Authority

This document is authoritative for scenario semantics introduced by Milestone 002.

This document is not authoritative for the full future scenario runner or asset/content scenarios.

## Purpose

Milestone 002 does not create the full `Agentic2D.ScenarioRunner` project. It introduces one built-in smoke scenario through the product CLI so the runtime can produce deterministic evidence early.

## Scenario: `runtime.smoke`

### ID

```text
runtime.smoke
```

### Category

```text
smoke
```

### Purpose

Prove that the minimal runtime can initialize deterministic state, accept a command, execute fixed ticks, emit events, answer a final-state query, evaluate assertions, and write a result artifact.

### Initial state

```text
current tick: 0
entity.player position: 0
```

The implementation may create `entity.player` during initialization if it emits or records `entity.created` deterministically.

### Inputs

```text
ticks: positive integer, default 3
output: required artifact directory
```

### Random seed policy

No random input is used in this scenario.

The result artifact may include:

```text
seed: none
```

or omit seed fields entirely.

### Command sequence

```text
move entity.player by +1
run fixed ticks until final tick equals requested tick count
```

### Expected events

At minimum, in deterministic order:

```text
runtime.started
entity.created
command.accepted
entity.moved
runtime.completed
```

Additional deterministic events are allowed only if they do not obscure the required events and are represented consistently.

### Expected assertions

```text
final tick equals requested tick count
entity.player exists
entity.player position equals 1
runtime.started event exists
entity.created event exists
command.accepted event exists
entity.moved event exists
runtime.completed event exists
result status is passed when all assertions pass
```

### Expected artifacts

```text
<output>/result.json
```

The artifact must conform to:

```text
docs/artifacts/runtime-result-contract.md
```

### Human review requirements

None.

This scenario has no visual, UX, asset-semantic, gameplay-feel, or review-gated output.

### Debug-mode applicability

Required.

Milestone 002 is a debug-oriented runtime slice using stable string identifiers.

### Packaged-mode applicability

Not applicable in Milestone 002.

Packaged runtime validation is explicitly deferred.

## Scenario validation command

```bash
dotnet run --project src/Agentic2D.Tools -- runtime smoke --ticks 3 --output artifacts/runtime-smoke
```

Required result:

```text
exit code: 0
artifact: artifacts/runtime-smoke/result.json
status: passed
finalTick: 3
entity.player position: 1
```
