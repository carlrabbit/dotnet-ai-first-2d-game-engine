# Migration — Guide System v0.5.0 to v0.5.1

## Purpose

Restore complete prompt templates that were unintentionally shortened in the v0.5.0 update package.

## Scope

This migration affects the guide repository itself and, conditionally, product repositories where the shortened prompts were already used.

## Required guide-repository changes

Replace these prompt templates with the complete v0.5.1 versions:

```text
templates/prompts/update-to-latest-guide-system.md
templates/prompts/execute-planned-milestone.md
templates/prompts/milestone-ai-executed-human-reviewed.md
templates/prompts/milestone-ai-executed-broad.md
templates/prompts/milestone-engineering-migration.md
```

Update:

```text
README.md
CHANGELOG.md
templates/PROMPTS.md
templates/guide-profile/guide-profile.json
```

Add:

```text
migrations/repair-shortened-prompt-application.md
templates/prompts/repair-v0.5.0-shortened-prompt-application.md
```

## Conditional product-repository migration

A product repository needs corrective assessment only if one of the shortened v0.5.0 prompts was already used to create or apply a migration or milestone package.

Potentially affected outcomes include:

- missing repository inspection;
- incomplete authority-document selection;
- missing `.guide-sync/pending/` hints;
- missing provider/consumer classification;
- missing human-review requirements;
- missing resumable-validation requirements;
- overly broad or underspecified implementation scope;
- missing execution prompt;
- incomplete chat handoff.

Use the dedicated repair prompt rather than applying a fixed patch blindly.

## Validation

- all five affected prompts contain full operational sections;
- all affected prompts include human-review handling where applicable;
- all affected prompts include constrained-execution and resumable-validation handling where applicable;
- `templates/PROMPTS.md` lists the repair prompt;
- guide profile version is `0.5.1`.
