# Unified Agent Execution Workflow Contract

## Authority

Authoritative for workspace validation, project validation, unified project execution, run inspection, run review, and recommended next actions.

## Commands

```text
agentic2d workspace validate <workspace>
agentic2d project validate <project-or-workspace>
agentic2d project run <project-or-workspace> --scenario <id>
agentic2d run inspect <run-directory>
agentic2d run review <run-directory>
```

## Workflow

```text
workspace validation
→ project/content validation
→ scenario execution
→ subsystem artifacts
→ run manifest
→ inspection
→ review pack
```

Unified commands orchestrate existing subsystem authority. They do not replace or hide subsystem diagnostics.

## Run identity

Run identity is explicit or deterministic and must not depend on wall-clock time.

## Recommended actions

Failures emit structured recommended commands tied to known diagnostic/object IDs and available artifact paths.

## Consumer boundary

Consumer agents should operate through workspace/project/run commands and generated wrappers without reading provider repository engineering scripts.

## Project-aware headless render projection

A successful renderable `project run` resolves maps, definitions, visuals, animations, assets, and the final runtime snapshot from the declared external game project. It performs headless structural projection without raylib or a graphics environment. The render family records `present`, `failed`, or `absent`; supported absence reasons are `render-disabled-by-explicit-option`, `scenario-has-no-renderable-snapshot`, `project-has-no-visual-content`, and `execution-failed-before-snapshot`. Render failure is not a successful absence. The manifest declares `same-execution` or `deterministic-replay`; the minimal-game template requires `same-execution` render evidence.
