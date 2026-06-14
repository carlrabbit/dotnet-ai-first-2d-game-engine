# Guide Sync Hint — Review Legacy Guide Leakage

## Status

Pending.

## Source

Created by the guide-system v0.2.0 migration planning package.

## Purpose

The repository contains or may contain legacy copied guide research such as setup and engineering guides under `docs/research/`. These files may be retained for traceability, but active project docs must not use them as ordinary implementation authority.

## Deferred work

During a documentation synchronization pass, review active project docs for guide leakage.

Check at least:

```text
README.md
AGENTS.md
docs/RESEARCH.md
docs/ENGINEERING.md
docs/MILESTONES.md
docs/SPECS.md
docs/DECISIONS.md
docs/milestones/*.md
docs/decisions/*.md
```

Look for:

- copied setup or engineering guide documents referenced as required implementation reading;
- external guide-system documents referenced as operational project authority;
- prompt templates copied or referenced as required implementation reading;
- TBP or issue-template dependencies introduced by accident;
- broad documentation synchronization instructions embedded in ordinary implementation milestones.

## Completion criteria

This hint can be deleted when active project docs consistently state that:

- copied guide material under `docs/research/` is non-authoritative traceability material;
- external guides are for planning, migration, documentation synchronization, and release readiness only;
- ordinary implementation agents use localized project docs and milestone-listed authority only;
- `.guide-profile.json` and `.guide-sync/` are not ordinary implementation authority.
