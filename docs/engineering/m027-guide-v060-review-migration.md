# M027 Guide-System v0.6.0 Review Migration

## Authority

Authoritative for repository-specific migration from recorded guide-system 0.5.1 to 0.6.0.

```text
repository-wide review check
→ explicit milestone review check

completed approval with future staleness
→ immutable historical record

pending request without owner
→ milestone owner or manual-review classification

canonical ID
→ durable identity

numeric alias
→ ephemeral interactive convenience
```

Inspect all requests and records and emit:

```text
artifacts/review-migration/M027/review-migration-report.json
artifacts/review-migration/M027/review-migration-report.md
```

Classification:

```text
active-owned
historical-completed
unfinished-focused-work
ambiguous-manual-review
invalid
```

Rules:

- do not create approval;
- do not reapprove completed milestones solely because later commits occurred;
- preserve decision, reviewer, evidence, and reviewed revision for provenance;
- remove generic future-change triggers from completed records;
- active M027 evidence must remain current until M027 completion;
- automated shard receipts remain fingerprinted and may stale.

The full v0.6.0 review command family is defined in `docs/engineering/human-review-workflow.md`. M027 uses its canonical request ID for show/record/reopen targets and `./eng/review-check.sh --milestone M027` for its gate.

Migration completes only when command tests pass, every pending request is owned or manually classified, historical approvals no longer stale from later commits, M027 gate behaves correctly, `.guide-profile.json` records 0.6.0, and M027 verifier passes.
