# Guide Sync Hint — Index Guide-System v0.3.0 Migration

## Status

pending

## Origin

Milestone 009 guide-system v0.3.0 migration package.

## Purpose

After the v0.3.0 migration is applied, a documentation synchronization pass should update normal project documentation indexes so the migration is discoverable without requiring ordinary implementation agents to read `.guide-profile.json`, `.guide-sync/`, or the external guide repository.

## Suggested documentation-sync scope

Review and update, where appropriate:

```text
docs/MILESTONES.md
docs/DECISIONS.md
docs/RESEARCH.md
README.md
AGENTS.md
```

Expected checks:

- Milestone 009 is indexed as implemented or planned after application.
- If an ADR is later created for adopting guide system v0.3.0, it is indexed after acceptance.
- Active docs still state that copied guide material under `docs/research/` is non-authoritative traceability material.
- Active docs do not require ordinary implementation agents to read `.guide-profile.json`, `.guide-sync/`, external guide internals, or prompt templates.
- Active docs do not reference external guide documents as operational repository authority.

## Completion criteria

This hint can be deleted when:

- Milestone 009 is indexed or intentionally excluded with a documented reason;
- any related accepted decision record is indexed or intentionally excluded with a documented reason;
- ordinary implementation routing still uses localized project docs and milestone-listed authority only;
- no guide documents or prompt templates have been copied into the repository.

## Notes

This file is deferred documentation synchronization metadata. Ordinary implementation agents must ignore `.guide-sync/` unless explicitly assigned documentation synchronization, guide migration, planning, or release-readiness work.
