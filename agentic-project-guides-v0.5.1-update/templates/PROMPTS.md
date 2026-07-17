# Prompt Templates

## Purpose

This index lists standard prompts for planning, migration, execution, documentation synchronization, review, and constrained validation workflows.

Prompt templates are guide-system methodology. They live in this guide repository, not in product repositories.

## Planning and migration prompts

| Task | Prompt |
|---|---|
| Adopt latest guide system from any old local guide model | `templates/prompts/adopt-latest-guide-system.md` |
| Update from any existing guide-system version to latest | `templates/prompts/update-to-latest-guide-system.md` |
| Repair a repository affected by shortened v0.5.0 prompts | `templates/prompts/repair-v0.5.0-shortened-prompt-application.md` |
| Prepare a completely empty project | `templates/prompts/new-empty-project.md` |
| Plan a human-led milestone | `templates/prompts/milestone-human-led.md` |
| Plan an AI-assisted milestone | `templates/prompts/milestone-ai-assisted.md` |
| Plan an AI-executed, human-reviewed milestone | `templates/prompts/milestone-ai-executed-human-reviewed.md` |
| Plan a broad AI-executed milestone | `templates/prompts/milestone-ai-executed-broad.md` |
| Plan an engineering migration milestone | `templates/prompts/milestone-engineering-migration.md` |
| Plan documentation synchronization | `templates/prompts/milestone-documentation-sync.md` |
| Plan release readiness | `templates/prompts/milestone-release-readiness.md` |

## Execution prompts

| Task | Prompt |
|---|---|
| Execute a planned milestone | `templates/prompts/execute-planned-milestone.md` |
| Execute an engineering migration | `templates/prompts/execute-engineering-migration.md` |
| Execute documentation synchronization | `templates/prompts/execute-documentation-sync.md` |
| Execute release readiness | `templates/prompts/execute-release-readiness.md` |

## Common rules

Planning prompts may read the external guide repository.

Implementation agents must not be required to read the guide repository.

Planning prompts should emit a filled execution prompt in chat.

When validation exposes a resumable plan, execution prompts should instruct constrained agents to run shards separately and finish with the verifier.

Deferred documentation synchronization work that must survive handoff belongs under:

```text
.guide-sync/pending/
```

Human review state belongs under:

```text
.review/
```
