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
  evidence/
```

Use `artifacts/review/` for generated or large evidence that should not be committed.

`.review/pending/` and `.review/records/` contain small durable Markdown or JSON records. `.review/evidence/` contains only intentionally committed, bounded evidence references or manifests.

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
- subject;
- review class and applicability level;
- source milestone/task;
- reviewer role;
- status;
- evidence references;
- decision and conditions;
- reviewed revision or fingerprint;
- re-review triggers;
- created/updated metadata that does not replace fingerprints.

## Staleness

An approval becomes stale when relevant source, generated output, acceptance criteria, evidence, or review fingerprints change.

## Canonical commands

Repositories with active required/blocking review expose:

```text
./eng/review-list.sh
./eng/review-request.sh
./eng/review-record.sh
./eng/review-check.sh
```

Launchers are thin. Review parsing, schema validation, fingerprinting, status transitions, and stale-record checks live in tested .NET engineering code.

`review-check` is the Tier 5 gate. It fails for unresolved, stale, malformed, superseded, missing, or insufficiently evidenced required/blocking reviews.

## M022 migration review

The guide-system v0.5.0 migration requires one migration-class review covering:

- shard boundaries and practical runtime limits;
- receipt/fingerprint trustworthiness;
- review-state usability;
- declared platform support;
- removal of guide leakage;
- confirmation that ordinary implementation agents remain isolated from guide metadata.
