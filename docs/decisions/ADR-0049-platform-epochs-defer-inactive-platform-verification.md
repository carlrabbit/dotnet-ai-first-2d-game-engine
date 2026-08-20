# ADR-0049 — Platform Epochs Defer Inactive-Platform Verification

## Status

Accepted.

## Context

The engine targets native Linux and Windows development.

In practice development occurs on one workstation/platform for several consecutive milestones before switching.

Requiring every milestone to execute native graphical/integration validation on both platforms would:

- block otherwise complete milestones on an inactive machine;
- encourage fabricated or stale platform evidence;
- keep human reviews open for months;
- replay historical milestone suites instead of validating the current repository.

M036 began a Windows development epoch. M036 and M037 both accumulated Linux-specific verification gaps while Windows validation was available.

## Decision

Use a **platform epoch** model.

Exactly one supported development platform is active for ordinary milestone execution.

Each normal milestone requires:

1. portable/headless validation on the active platform;
2. native/platform-specific validation on the active platform;
3. milestone human review on the active platform when declared.

Platform-sensitive checks for inactive supported targets may be deferred.

Deferred checks:

- are explicit project engineering state;
- are not labeled as passed;
- do not keep the normal milestone open;
- are validated cumulatively against the current repository when that platform becomes active.

Completed milestone reviews are never reopened merely because the active platform changes.

A platform catch-up task owns any new platform-compatibility human review.

## Current epoch

```text
active platform: Windows
starts at milestone: M036
inactive supported target: Linux
```

Current deferred state is stored in:

```text
eng/platform-verification.json
```

## Consequences

### Positive

- milestone completion matches the real development workflow;
- platform evidence stays honest;
- cross-platform architecture remains a product constraint;
- inactive-platform debt is explicit;
- platform switches validate current code instead of historical commits;
- human review remains milestone-scoped.

### Negative

- cross-platform regressions may be discovered later at epoch transition;
- deferred verification debt must remain visible and bounded;
- platform-switch catch-up can become substantial after a long epoch.

## Guardrails

Do not:

- call deferred evidence `passed`;
- weaken platform-neutral architecture to favor the active platform;
- silently drop deferred obligations;
- treat supported targets as permanently verified by old evidence;
- reopen completed milestone reviews during catch-up.

Release/distribution gates may independently require simultaneous multi-platform evidence.
