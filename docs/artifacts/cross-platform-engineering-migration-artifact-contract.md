# Cross-Platform Engineering Migration Artifact Contract

## Root

```text
artifacts/engineering/M036/
```

## Required artifacts

```text
guide-profile-migration-report.json
launcher-inventory.json
launcher-cleanup-report.json
platform-policy.json
git-normalization-report.json
path-portability-report.json
asset-home-platform-report.json
receipt-environment-report.json

platform/
  linux/
    platform-verification.json
    command-results.json
    graphics-development.json
  windows/
    platform-verification.json
    command-results.json
    graphics-development.json

platform-comparison.json
m036-completion-audit.json
diagnostics.json
```

## Launcher inventory

Each launcher entry contains:

```text
path
stable command identity
classification
active references
suite references
test references
platform constraint
decision
decision reason
replacement command when deleted/superseded
```

`launcher-cleanup-report.json` must prove no `historical-delete` file remains and no active reference targets a deleted launcher.

## Platform verification

Each platform report contains:

```text
schema
platform
os version family
architecture
launcher family/version
dotnet sdk
source revision
repository/input fingerprint
required Class A command results
semantic result hashes
graphics development status
generated-at
```

A graphics skip is not a passing platform report for M036.

## Platform comparison

Requires both reports to target the same relevant source revision/fingerprint.

It distinguishes:

```text
semantic equality
allowed host metadata differences
declared platform-specific capability differences
unexpected difference
```

Unexpected semantic differences fail comparison.

## Completion audit

`m036-completion-audit.json` enumerates every M036 acceptance criterion and records:

```text
satisfied
not-applicable with reason
unsatisfied-agent-resolvable
unsatisfied-external
```

The final terminal outcome is one of:

```text
COMPLETE
AWAITING HUMAN REVIEW
BLOCKED
```

M036 has no required human review, so `AWAITING HUMAN REVIEW` is not expected unless the milestone contract is formally amended.

## Boundedness

Do not store full build/test logs in the durable M036 artifact root. Store summaries and paths to transient logs where useful.

Do not record usernames, home-directory absolute paths, or raw environment secrets.
