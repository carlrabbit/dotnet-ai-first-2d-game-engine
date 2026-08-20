# Cross-Platform Development and Launcher Policy

## Authority

Authoritative for native launcher policy, active launcher classification, platform epochs, parallel-host workflow, and inactive-platform verification.

## Native launchers

```text
Linux: Bash
Windows: PowerShell 7 (`pwsh`)
```

The launcher families expose current engineering capabilities over shared .NET semantics.

## Active platform

Current platform state is authored in:

```text
eng/platform-verification.json
```

Current epoch begins at M036 on Windows.

Normal milestone validation uses the native launcher of the active platform.

An inactive supported platform is not required merely for ordinary milestone completion.

## Launcher inventory classification

Tracked `eng/*.sh` / `eng/*.ps1` commands remain classified by current purpose:

### `active-cross-platform`

Current engineering/capability command whose semantics are supported on both targets.

### `active-platform-specific`

Current command intentionally limited to one platform, such as Linux export proof.

### `thin-compatibility-wrapper`

Retained because current project truth or active regression workflow still relies on its stable name.

### `historical-delete`

Delete when it has no current capability, regression, engineering, documentation, or supported platform purpose.

Compatibility with completed milestone prose alone is insufficient.

## Thin-launcher rule

Prefer generic launchers and shared host commands over mirrored script forests.

Launchers:

- forward arguments;
- propagate exit codes;
- use native shell conventions;
- do not reimplement suite, fingerprint, receipt, review, or platform-epoch semantics.

## Platform-sensitive validation

Portable tests run on the active platform.

Native/platform-sensitive checks run on the active platform.

For an inactive supported platform:

- record required future checks in `eng/platform-verification.json`;
- do not fabricate passing reports;
- do not require the inactive host to finish each milestone.

## Platform catch-up

On an epoch switch, validate the current repository against accumulated obligations for the newly active platform.

Do not replay historical commits or reopen completed milestone reviews.

A catch-up task may create its own platform-compatibility review where subjective native behavior requires it.

## Git and line endings

`.gitattributes` is project authority for normalization.

Repository text must not churn between Linux and Windows due solely to local Git line-ending configuration.

Binary assets are explicitly classified.

## Parallel machines

Supported workflow:

1. clone normally on each machine;
2. create branches/commits normally;
3. push/pull through Git;
4. rebuild locally;
5. regenerate ignored artifacts locally;
6. keep raw shared asset homes machine-local.

Do not use a shared Windows/Linux network working tree as the canonical repository.

## Evidence transfer

Generated validation artifacts are normally ignored.

When a platform catch-up or comparison requires small reports from another host, transfer them explicitly through a user-controlled mechanism.

Do not permanently track transient validation artifacts merely to move them between machines.
