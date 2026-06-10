# Milestone 001 — Base Engineering Substrate

## Goal

Create the actual engineering substrate for the repository.

## Required authority

- `docs/INITIALIZATION.md`
- `docs/ENGINEERING.md`
- `docs/engineering/command-contract.md`
- `docs/engineering/validation-tiers.md`

## Scope

- Add `.editorconfig`.
- Add `global.json`.
- Add `Directory.Build.props`.
- Add `Directory.Packages.props`.
- Add `.config/dotnet-tools.json` if tools are required.
- Add canonical `eng/` scripts.
- Ensure scripts fail clearly if no solution exists yet, or create the initial solution in the same milestone if explicitly scoped.

## Non-goals

- Do not implement engine runtime behavior.
- Do not add runtime graphics packages.
- Do not add asset pipeline behavior.

## Validation tier

Tier 1 if scripts validate meaningful state. Tier 0 if the task only creates pre-solution files.

## Focus Area A — Engineering scripts

### Goal

Create canonical shell scripts that become the repository engineering API.

### Likely files

```text
eng/common.sh
eng/restore.sh
eng/build.sh
eng/test.sh
eng/format.sh
eng/check.sh
```

### Direct documentation impact

Update `docs/ENGINEERING.md` and `docs/engineering/command-contract.md` with actual command behavior.

## Focus Area B — Shared .NET configuration

### Goal

Add baseline .NET repository configuration.

### Likely files

```text
.editorconfig
global.json
Directory.Build.props
Directory.Packages.props
.config/dotnet-tools.json
```
