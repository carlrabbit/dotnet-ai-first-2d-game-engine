# Cross-Platform Development and Launcher Policy

## Authority

Authoritative for M036 launcher classification, deletion criteria, native shell support, and parallel host workflow.

## Native launcher policy

```text
Linux: Bash
Windows: PowerShell 7 (`pwsh`)
```

The two launcher families expose current engineering capabilities over shared .NET semantics.

## Launcher inventory classification

Every tracked `eng/*.sh` file receives exactly one M036 classification.

### `active-cross-platform`

Keep the engineering capability and provide native access on both supported platforms.

Typical examples:

- restore;
- build;
- test;
- format;
- check;
- review operations;
- current suite dispatch;
- current product/headless capability smoke used by ongoing development.

### `active-platform-specific`

Keep when the capability itself is intentionally platform-specific.

Example:

- Linux export proof.

Do not create a meaningless Windows counterpart.

### `thin-compatibility-wrapper`

Keep only when a stable current name is still referenced by current project truth or active regression workflows and the wrapper is thin.

Compatibility with completed milestone prose alone is insufficient.

### `historical-delete`

Delete when all are true:

1. the wrapper is not referenced by active engineering documentation as a current command;
2. it is not registered by a current suite;
3. no current test uses it as a supported entry point;
4. it does not provide a current product/capability/regression proof;
5. it is not a currently supported platform-specific command;
6. its only remaining purpose is completed milestone history, superseded workflow, or duplicate legacy orchestration.

## Deletion behavior

For every deleted wrapper:

- record classification and reason;
- remove active indexes/references;
- remove stale suite registration;
- remove wrapper-only compatibility tests that no longer express current behavior;
- do not rewrite completed milestone documents solely to erase historical command text.

## PowerShell surface

Prefer generic launchers and shared host commands over one `.ps1` per historical suite.

PowerShell scripts:

- require PowerShell 7;
- use ordinary `pwsh` semantics;
- do not contain Bash emulation;
- propagate host exit codes;
- use robust argument forwarding.

## Git and line endings

`.gitattributes` is project authority for normalization.

Repository text must not churn between Linux and Windows due solely to local Git line-ending configuration.

Binary assets must be explicitly classified.

## Parallel machines

Supported workflow:

1. clone normally on each machine;
2. create branches/commits normally;
3. push/pull through Git;
4. rebuild locally;
5. regenerate ignored artifacts locally;
6. keep raw shared asset homes machine-local.

Do not use a shared Windows/Linux network working tree as the canonical repository.

## Platform evidence transfer

M036 validation artifacts are generated and normally ignored.

When one host needs the other host's small M036 platform report for final comparison, transfer that report explicitly through a user-controlled file transfer or equivalent local mechanism.

Do not permanently track transient validation artifacts merely to move them between machines.
