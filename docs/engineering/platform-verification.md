# Platform Verification

## Authority

This document is authoritative for development-platform epochs, active-platform milestone validation, deferred inactive-platform verification, and platform catch-up.

Machine-readable current state is:

```text
eng/platform-verification.json
```

## Product target versus milestone execution

The engine targets supported development platforms independently of which platform is used for a particular milestone.

```text
supported development targets
!=
platforms executed by every milestone
```

At any point, one supported platform is the **active development platform**.

A sequence of milestones developed primarily on that platform is a **platform epoch**.

## Current epoch

```text
active platform: Windows
native launcher: PowerShell 7
epoch starts: M036
inactive supported platform: Linux
```

Deferred Linux obligations from M036 and M037 are recorded in `eng/platform-verification.json`.

## Milestone validation rule

For an ordinary milestone:

### Portable validation

Required on the active platform.

Examples:

- unit tests;
- deterministic/headless simulation;
- serialization and save semantics;
- structural UI projection;
- semantic input;
- content validation;
- platform-neutral engineering-host behavior.

Portable code must remain portable by architecture and tests even though the suite is executed on one active platform.

### Active-platform integration

Required on the active platform when applicable.

Examples:

- window creation;
- native graphics loading;
- display mode changes;
- native keyboard/mouse/text input;
- audio device behavior;
- filesystem/process integration;
- safe-mode startup.

### Inactive-platform verification

Platform-sensitive checks for an inactive supported platform may be deferred.

Deferred verification:

- is explicit;
- is not reported as `passed`;
- does not keep an otherwise accepted normal milestone open;
- does not turn absence of the inactive platform into `BLOCKED`;
- accumulates in `eng/platform-verification.json`.

## Human review

Milestone human review runs on the active development platform unless the milestone explicitly makes cross-platform subjective comparison part of its product acceptance.

An approved milestone review completes that milestone.

Inactive-platform subjective checks, when necessary, are owned by a future platform catch-up task and do not reopen a completed milestone review.

## Platform switch and catch-up

When development switches to another supported platform:

1. update the active epoch only as part of the transition task;
2. run the current portable baseline on the newly active platform;
3. collect all deferred obligations for that platform;
4. execute a cumulative catch-up against the **current repository**;
5. fix current incompatibilities;
6. perform a platform-compatibility human review when deferred items require subjective verification;
7. resolve/remove satisfied deferred items;
8. continue normal milestones on the new active platform.

Do not replay every historical milestone against its old commit.

Do not reopen completed milestone reviews.

## Verification states

Use these concepts distinctly:

```text
supported-target
active-verified
deferred-inactive-platform
unsupported
```

`deferred-inactive-platform` is not failure and is not success.

## Guide metadata

`.guide-profile.json` may identify broadly supported/tested launcher platforms for guide selection and traceability.

It is not the authority for the current platform epoch or per-milestone verification freshness.

Ordinary implementation uses `eng/platform-verification.json` and this document for that purpose.
