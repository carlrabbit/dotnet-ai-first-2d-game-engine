# Guide Sync Hint — Index Guide-System Migration Documents

## Status

Pending.

## Source

Created by the guide-system v0.2.0 migration planning package.

## Purpose

After the migration package is applied, a documentation synchronization pass should update repository indexes so the new migration milestone and decision record are discoverable from the normal project-documentation entry points.

## Deferred work

Review and update, where appropriate:

```text
docs/MILESTONES.md
docs/DECISIONS.md
```

Expected updates:

- add the guide-system migration milestone under the current milestone list or completed milestone list after implementation;
- add `docs/decisions/ADR-0009-adopt-external-guide-system-v0.2.0.md` to the decision index after the ADR is accepted;
- preserve the rule that repository docs contain project truth and do not make external guide docs ordinary implementation authority.

## Completion criteria

This hint can be deleted when:

- the migration milestone is indexed or intentionally excluded with a documented reason;
- ADR-0009 is indexed or intentionally excluded with a documented reason;
- no index points ordinary implementation agents to `.guide-profile.json`, `.guide-sync/`, copied guide documents, or external guide internals as implementation authority.
