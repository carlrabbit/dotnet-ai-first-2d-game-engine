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
