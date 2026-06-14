# ADR-0009 — Adopt the External Guide System v0.2.0

## Status

Proposed for the guide-system migration milestone.

## Context

This repository was initialized from project-specific planning packages that retained copied setup and engineering guide research under `docs/research/` for traceability.

The repository now has localized project-truth documentation for engineering commands, runtime principles, the minimal deterministic runtime, and the product CLI. It should no longer depend on copied setup or engineering guides as repository-local operational authority.

The guide-system model separates external methodology from product repositories:

```text
External guide repository:
  planning, migration, documentation synchronization, release readiness

Product repository:
  project truth, implementation authority, engineering commands, specs, decisions, milestones
```

Version `0.2.0` additionally introduces a deferred documentation synchronization queue under `.guide-sync/pending/`.

## Decision

Adopt the external guide-system model for this repository at guide system version `0.2.0`.

This repository will:

- keep project truth in `README.md`, `AGENTS.md`, `docs/`, `eng/`, source, tests, and authored project data;
- use `.guide-profile.json` only as guide-selection and traceability metadata;
- use `.guide-sync/pending/` only for deferred documentation synchronization hints;
- retain copied guide documents under `docs/research/` only as legacy, non-authoritative research inputs unless a later migration explicitly removes them;
- avoid repository-local TBPs and issue templates by default;
- avoid copying guide-system documents or prompt templates into this repository.

Ordinary implementation agents must not be required to read:

```text
.guide-profile.json
.guide-sync/
docs/research/project-setup-guide-*.md
docs/research/engineering-guide-*.md
external guide repository internals
prompt templates
```

Guide migration, planning, documentation synchronization, and release-readiness tasks may use the external guide repository as input.

## Consequences

The repository gains traceability to the selected guide-system version without making the guide repository part of ordinary implementation authority.

Deferred documentation work can survive handoff through `.guide-sync/pending/` without becoming permanent project documentation.

Implementation milestones remain focused on project behavior and validation rather than broad guide-system methodology.

Documentation synchronization becomes an explicit task mode that may consume `.guide-sync/pending/` and then delete or narrow resolved hints.

## Alternatives considered

### Keep copied guides under `docs/research/` as ordinary authority

Rejected. This creates duplicate methodology in product repositories and risks stale local copies becoming stronger than localized project truth.

### Delete all legacy research guide copies immediately

Deferred. The copied guide documents are already marked as non-authoritative research. Removing them is not required for migration correctness and should be handled as a manual-review or documentation-sync cleanup if desired.

### Copy guide-system prompt templates into this repository

Rejected. Prompt templates belong in the external guide repository. Product repositories should not carry copied prompt libraries by default.

### Make `.guide-sync/` part of implementation authority

Rejected. `.guide-sync/` is coordination metadata for documentation synchronization. It must not contain implementation instructions, durable requirements, or architecture authority.

## Validation

The migration is valid when:

- `.guide-profile.json` exists and declares guide system version `0.2.0`;
- `AGENTS.md` tells ordinary implementation agents to ignore `.guide-profile.json` and `.guide-sync/`;
- `.guide-sync/pending/` exists with any deferred synchronization hints needed for handoff;
- active project docs do not treat copied guide documents or prompt templates as operational authority;
- no copied guide documents or prompt templates are introduced by the migration.
