# Deferred synchronization hint — platform epochs and inactive-platform verification debt

## Classification

```text
guide-system metadata only
ordinary implementation agents: must not read
source repository change: post-M037 platform-epoch overlay
```

## Generalizable model

Assess whether future external guide guidance should distinguish:

```text
supported development targets
active development platform
platform epoch
portable validation
active-platform integration validation
deferred inactive-platform verification
platform catch-up
```

Candidate general doctrine:

> Cross-platform support does not require every ordinary milestone to execute on every supported platform. A repository may use one active development platform for a sequence of milestones. Portable validation and active-platform integration gate each milestone. Inactive-platform-specific verification may be deferred explicitly and validated cumulatively against the current repository when development switches platform. Completed milestone human reviews are not reopened; a catch-up task owns any platform-compatibility review.

## Candidate guide areas

- engineering command/platform model;
- milestone validation planning;
- human-review ownership;
- readiness/completion semantics;
- guide-profile distinction between supported/tested targets and current verification freshness.

## Completion criteria

Resolve only during a dedicated guide synchronization/migration workflow against the then-current `carlrabbit/agentic-project-guides`.

Do not copy guide documents into this repository.

The platform-epoch repository behavior does not depend on resolving this hint.
