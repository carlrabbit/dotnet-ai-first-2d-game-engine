# Human Review Workflow

## Authority

This document is authoritative for repository-local review requests, durable review records, evidence references, status, staleness, Tier 5 review gates, and review behavior across platform epochs.

## Applicability

Use repository-local review state when project truth marks review as `required` or `blocking`, or when a migration explicitly requires a durable human decision.

Review levels:

```text
none
recommended
required
blocking
```

Review classes include semantic, visual, UX, creative, security, public-api, release, migration, accessibility-baseline, artifact-quality, and platform-compatibility where declared.

## Repository state

```text
.review/
  pending/
  records/
  closed/
```

Use `artifacts/review/` for generated or large evidence.

Durable review requests/records remain small project truth.

## Review states

```text
pending
approved
changes-requested
rejected
waived
superseded
```

## Required fields

A request or record contains:

- review ID;
- owning milestone ID/path;
- subject;
- review class/applicability;
- source milestone/task;
- reviewer role;
- status;
- evidence;
- decision/conditions;
- reviewed revision/fingerprint;
- decision history/provenance/completion time.

## Staleness

Before completion, active review evidence must be current for its owning milestone.

Completed records are immutable historical evidence and do not stale when later commits change the repository.

Future milestones/tasks create new review requests.

## Platform epochs

Unless a milestone explicitly requires subjective comparison across multiple platforms, its human review is performed on the **active development platform**.

Approval gates that milestone and becomes historical evidence after completion.

Inactive-platform subjective verification may be deferred to a future platform catch-up task.

That catch-up task owns any new `platform-compatibility` review it requires.

Do not reopen a completed milestone review merely because development later switches platform.

Release gates or explicitly cross-platform milestones may require separate multi-platform review by their own authority.

## Canonical commands

Repositories with active required/blocking review expose the canonical Bash family:

```text
./eng/review-list.sh [--milestone <id>] [--state <active|historical>] [--status <status>]
./eng/review-show.sh <review-id-or-alias>
./eng/review-request.sh --milestone <id>
./eng/review-record.sh <review-id-or-alias> <decision>
./eng/review-reopen.sh <review-id-or-alias> --reason <reason> [--correct-record]
./eng/review-check.sh --milestone <id>
```

Windows uses PowerShell 7 adapters with the same host semantics.

Launchers are thin. Review parsing, schema validation, fingerprinting, and transitions live in tested .NET engineering code.

`review-list` deterministically displays active/historical reviews and maintains ephemeral aliases under ignored review-session artifacts.

`review-show` is read-only.

`review-record` records the durable human decision.

`review-reopen` may return a review to pending only while its owning milestone is active. It never reopens a historical record merely because HEAD, documentation, platform, or fingerprints changed.

`review-check` is the Tier 5 gate for the named milestone only and ignores historical milestones as current gates.

## Platform-switch review

A future platform catch-up may create a new review such as:

```text
review.platform.linux.catch-up.<epoch-or-task>
```

when accumulated deferred native/UX behavior needs human confirmation.

That review belongs to the catch-up task, not to M036/M037 or other completed milestones.
