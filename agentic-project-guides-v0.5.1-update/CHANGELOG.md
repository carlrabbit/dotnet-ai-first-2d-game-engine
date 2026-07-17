# Changelog

## 0.5.1

Migration required: recommended for guide repositories updated to v0.5.0; conditional for product repositories where shortened prompts were already used.

Affected areas:

- planning prompt completeness;
- migration prompt completeness;
- execution handoff prompt completeness;
- documentation-sync handoff;
- human-review requirements;
- constrained-execution validation requirements.

Fixed:

- restored full repository inspection requirements;
- restored ZIP deliverable boundaries;
- restored required milestone content;
- restored authority-document routing;
- restored provider/consumer distinction;
- restored direct versus deferred documentation impact;
- restored `.guide-sync/pending/` handling;
- restored chat response requirements;
- restored quality-bar checks;
- retained v0.4.0 human-review and engineering-command requirements;
- retained v0.5.0 constrained-execution and resumable-validation requirements.

Added:

- `migrations/guide-system-v0.5.0-to-v0.5.1.md`;
- `migrations/repair-shortened-prompt-application.md`;
- `templates/prompts/repair-v0.5.0-shortened-prompt-application.md`.

Special migration path:

If a product repository was already planned or migrated using the shortened v0.5.0 `update-to-latest-guide-system.md` prompt, run the repair planning prompt. It inspects the repository for omitted migration work and creates a focused corrective milestone package.

Deprecated:

- abbreviated prompt templates that depend on the planning agent reconstructing omitted methodology from context.

## 0.5.0

Added constrained-execution and resumable-validation guidance.

## 0.4.0

Added repository-local human review and cross-platform engineering command guidance.

## 0.3.0

Added execution prompts and generic latest-version adoption/update prompts.
