# Cross-Platform Engineering Contract

## Authority

Authoritative for the M036 Linux/Windows engineering surface.

## Supported development platforms

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

Support is evidence-based. A platform is supported only after its required platform verification passes.

## Canonical engineering interface

`eng/` remains the stable human/agent interface.

Substantive semantics live in tested .NET engineering code. Native launchers:

- locate the repository/engineering host;
- forward arguments;
- invoke the same semantic command;
- return the same exit classification.

Launchers must not duplicate suite definitions, receipt/fingerprint logic, review semantics, artifact schemas, or platform-comparison rules.

## Platform-neutral paths

Durable repository-relative paths serialize with `/`.

Host absolute paths may be used operationally but must not enter durable project truth or cross-platform semantic fingerprints unless explicitly declared.

## Temporary data and atomic files

Use host-native temporary directories.

When atomic replacement is required, stage on the same filesystem/volume as the destination.

Interrupted operations cannot leave a valid success receipt.

## Environment metadata

Validation evidence records:

```text
os
architecture
launcher
dotnet sdk/runtime
graphics capability when relevant
```

Environment identity is provenance metadata unless a contract explicitly makes it a semantic input.

## Git boundary

Git synchronizes:

- source;
- tests;
- authored configuration;
- approved/promoted project assets;
- documentation;
- durable review/configuration truth.

Git does not synchronize:

- bin/obj;
- normal generated artifacts;
- IDE state;
- raw shared asset homes;
- temporary preview files.

## Product/platform distinction

Windows development support does not imply Windows distribution support.

Linux-only export proofs remain valid platform-specific commands.

## Parallel development

Each host uses its own clone/worktree and machine-local generated state. Cross-OS shared working directories are not part of the support contract.
