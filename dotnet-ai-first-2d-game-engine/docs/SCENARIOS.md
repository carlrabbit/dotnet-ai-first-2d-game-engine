# Scenarios

## Authority

This document is authoritative for deterministic scenario validation concepts until more specific scenario contracts exist.

## Purpose

Scenarios validate runtime, content, asset import, UI, save/load, packaged-mode equivalence, and other behavior that ordinary unit tests cannot prove alone.

## Initial scenario categories

```text
smoke
gameplay
UI
asset-import
map-validation
animation-validation
shader-material-preview
save-load
performance
soak
regression
```

## Required scenario fields

```text
id
category
purpose
initial state
inputs
random seed policy
expected events
expected assertions
expected artifacts
human review requirements
debug-mode applicability
packaged-mode applicability
```

## Future scenario command shape

```text
agentic2d scenario run <scenario-id> --output artifacts/scenarios/<run-id>
```

or through engineering wrappers:

```text
./eng/scenario.sh <scenario-id>
./eng/scenario-smoke.sh
./eng/scenario-packaged.sh <scenario-id>
```
