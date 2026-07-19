# Human Review Workflow

## Authority

This document is authoritative for repository-local review requests, durable review records, evidence references, status, staleness, and the Tier 5 review gate.

## Applicability

Use repository-local review state when project truth marks review as `required` or `blocking`, or when a migration explicitly requires a durable human decision.

Review levels:

```text
none
recommended
required
blocking
```

Review classes include:

```text
semantic
visual
UX
creative
security
public-api
release
migration
artifact-quality
```

## Repository state

```text
.review/
  pending/
  records/
  closed/
```

Use `artifacts/review/` for generated or large evidence that should not be committed.

`.review/pending/`, `.review/records/`, and `.review/closed/` contain small durable Markdown or JSON records. Generated evidence remains under `artifacts/review/`.

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

A review request or record contains:

- review ID;
- owning milestone ID and path;
- subject;
- review class and applicability level;
- source milestone/task;
- reviewer role;
- status;
- evidence references;
- decision and conditions;
- reviewed revision or fingerprint;
- decision history, provenance revision/fingerprint, and completion time.

## Staleness

Before completion, active review evidence must be current for its owning milestone. Completed records are immutable historical evidence and do not stale when later commits change the repository. Future milestones create new review requests.

## Canonical commands

Repositories with active required/blocking review expose:

```text
./eng/review-list.sh [--milestone <id>] [--state <active|historical>] [--status <status>]
./eng/review-show.sh <review-id-or-alias>
./eng/review-request.sh --milestone <id>
./eng/review-record.sh <review-id-or-alias> <decision>
./eng/review-reopen.sh <review-id-or-alias> --reason <reason> [--correct-record]
./eng/review-check.sh --milestone <id>
```

Launchers are thin. Review parsing, schema validation, fingerprinting, and status transitions live in tested .NET engineering code.

`review-list` deterministically displays active and historical reviews and atomically writes its current alias context to ignored `artifacts/review/session/aliases.json`. Every list context assigns numeric aliases. `show`, `record`, and `reopen` accept a canonical ID or an alias only from that latest successful list; a changed review context makes an alias fail safely and requires another list. Aliases never enter review files, milestones, receipts, automation, or cross-references.

`review-show` is read-only and displays the request/record paths, ownership, state, subject, classes, level, reviewer role, evidence, criteria, waiver policy, current decision, history, and provenance. It never compares a historical record to current repository state. `review-record` resolves the owning milestone from the active canonical request, records the decision, reviewer, notes/conditions, evidence, provenance, timestamp, and history, and only final decisions move an active request to an immutable record. `changes-requested` remains active.

`review-reopen` may return a review to pending only while its owning milestone is active. It never reopens a historical record merely because HEAD, source, documentation, or fingerprints changed; later milestones create their own review. An erroneous historical record may be corrected only with `--correct-record --reason <reason>`; the original record remains immutable and the correction is a separately identified active request. `review-check` is the Tier 5 gate for the named milestone only. It ignores aliases and historical milestones as current gates, and fails unresolved, malformed, superseded, missing, or insufficiently evidenced required/blocking reviews.

## M022 migration review

The guide-system v0.5.0 migration requires one migration-class review covering:

- shard boundaries and practical runtime limits;
- receipt/fingerprint trustworthiness;
- review-state usability;
- declared platform support;
- removal of guide leakage;
- confirmation that ordinary implementation agents remain isolated from guide metadata.
