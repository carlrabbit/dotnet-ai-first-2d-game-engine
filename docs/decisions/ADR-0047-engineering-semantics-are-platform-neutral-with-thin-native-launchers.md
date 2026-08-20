# ADR-0047 — Engineering Semantics Are Platform-Neutral with Thin Native Launchers

## Status

Accepted for M036.

## Context

The repository currently exposes a large Bash launcher surface and declares Linux/Bash as the only tested engineering platform. `Agentic2D.Engineering` already owns resumable suites, fingerprints, receipts, and review logic.

Native Windows development is now required. Mirroring every Bash file and its logic into PowerShell would create two engineering implementations and preserve obsolete historical wrappers indefinitely.

## Decision

Use `Agentic2D.Engineering` as the platform-neutral semantic authority.

Expose native thin launchers for:

```text
Linux -> Bash
Windows -> PowerShell 7
```

Port the active engineering surface, not the historical file set.

Inventory existing shell wrappers and delete completed-milestone-only or superseded wrappers that have no current capability, regression, engineering, documentation, or supported platform purpose.

Keep current platform-specific commands when the underlying capability remains supported, including Linux-only export proof.

## Consequences

### Positive

- one tested semantic implementation;
- lower launcher duplication;
- native Windows development;
- simpler long-term addition/removal of engineering commands;
- explicit platform evidence;
- working tree reflects current operational truth.

### Negative

- migration touches a broad engineering surface;
- stale active references must be discovered and updated;
- some historical milestone command names disappear from the working tree;
- cross-platform completion requires two hosts.

## Rejected alternatives

### Mirror every `.sh` as `.ps1`

Rejected because it duplicates semantics and preserves dead wrappers.

### Use Bash/WSL as the Windows development contract

Rejected because the selected target is native Windows/PowerShell development.

### Keep every historical wrapper forever

Rejected because Git already preserves history and dead launchers create false current authority.

### Move engineering commands into the product CLI

Rejected because repository engineering remains separate from product runtime/API.
