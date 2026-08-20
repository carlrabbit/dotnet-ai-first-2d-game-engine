# Cross-Platform Engineering Contract

## Authority

Authoritative for Linux/Windows development support, native launchers, platform-neutral engineering semantics, platform epochs, and cross-platform portability boundaries.

## Supported development targets

```text
Linux:
  native .NET development
  Bash launchers
  headless engineering
  Raylib graphical development where graphics-capable
  Linux export where separately supported

Windows:
  native .NET development
  PowerShell 7 launchers through pwsh
  headless engineering
  Raylib graphical development where graphics-capable
  Windows export not implied
```

Both are supported targets.

Fresh platform verification is governed by `docs/engineering/platform-verification.md` and `eng/platform-verification.json`.

A supported target does not need to be executed by every milestone.

## Platform epochs

Exactly one supported development platform is active for normal milestone execution.

Portable validation and active-platform integration evidence gate the milestone.

Inactive-platform-specific validation may be deferred as explicit verification debt.

Deferred evidence must never be labeled as passing platform evidence.

## Canonical engineering interface

`eng/` remains the stable human/agent engineering interface.

Substantive semantics live in tested .NET engineering code. Native launchers:

- locate the repository/engineering host;
- forward arguments;
- invoke the same semantic command;
- return the same exit classification.

Launchers must not duplicate suite definitions, receipt/fingerprint logic, review semantics, artifact schemas, platform-epoch policy, or platform-comparison rules.

## Platform-neutral paths

Durable repository-relative paths serialize with `/`.

Host absolute paths may be used operationally but must not enter durable project truth or cross-platform semantic fingerprints unless explicitly declared.

## Temporary data and atomic files

Use host-native temporary directories.

When atomic replacement is required, stage on the same filesystem/volume as the destination.

Interrupted operations cannot leave valid success evidence.

## Environment metadata

Validation evidence records platform provenance such as:

```text
os
architecture
launcher
dotnet sdk/runtime
graphics capability when relevant
active/deferred platform state when relevant
```

Environment identity is provenance unless a contract explicitly makes it a semantic input.

## Fingerprints

Platform-neutral semantic fingerprints do not change merely because validation ran on Windows or Linux.

Platform-sensitive receipts include the active-platform state as an input so an epoch switch invalidates stale native-integration evidence.

## Git boundary

Git synchronizes source, tests, authored configuration, approved project assets, documentation, and durable review/engineering state.

Git does not synchronize bin/obj, ordinary generated artifacts, IDE state, raw shared asset homes, or temporary preview data.

## Product/platform distinction

Windows development support does not imply Windows distribution support.

Linux-only export proofs remain valid platform-specific commands.

## Parallel development

Each host uses its own clone/worktree and machine-local generated state.

Git is the synchronization boundary.

Cross-OS shared working directories are not part of the support contract.
