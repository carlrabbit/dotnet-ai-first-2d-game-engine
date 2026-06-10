# Engineering

## Authority

This document indexes build, validation, command contracts, and engineering policy for this repository.

## Current status

The repository has the base engineering substrate established by Milestone 001:

- shared .NET/editor configuration at the repository root;
- executable canonical `eng/` scripts;
- a minimal `.slnx` solution with contracts, engine, and unit test projects.

## Indexed documents

- `docs/engineering/command-contract.md`
- `docs/engineering/validation-tiers.md`
- `docs/engineering/future-dotnet-solution.md`
- `docs/engineering/product-cli.md`

## Canonical engineering commands

The project uses these canonical `eng/` commands:

```text
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/format.sh
./eng/check.sh
```

Command details and validation tier mapping are defined in `docs/engineering/command-contract.md`.

Additional artifact-first commands are expected later:

```text
./eng/product-validate.sh
./eng/cli-smoke.sh
./eng/scenario.sh <scenario-id>
./eng/scenario-smoke.sh
./eng/content-validate.sh <scope>
./eng/artifacts-validate.sh <artifact-path>
./eng/review-pack.sh <run-id-or-artifact-path>
```

Do not create commands that pass without validating meaningful state. Before the .NET solution exists, command creation should either be deferred or fail clearly with an initialization message.
