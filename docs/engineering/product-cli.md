# Product CLI

## Authority

This document is the placeholder authority for the future `agentic2d` product CLI.

## Product CLI principle

The product CLI is the engine/runtime API for agents, CI, and humans. It is separate from `eng/` repository scripts.

## Initial command candidates

```text
agentic2d validate
agentic2d scenario run <scenario-id>
agentic2d asset inspect <path>
agentic2d map preview <map-id>
agentic2d content validate <scope>
```

## Required command contract fields

Each command must eventually define:

- purpose;
- input syntax;
- deterministic behavior;
- output path;
- artifact schema;
- diagnostics schema;
- exit codes;
- examples;
- validation command.
