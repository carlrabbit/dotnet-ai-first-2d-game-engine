# Engineering

## Authority

This document indexes build, validation, command contracts, and engineering policy for this repository.

## Current status

The repository is an initialization skeleton. It intentionally has no executable `eng/` scripts and no .NET projects yet.

## Indexed documents

- `docs/engineering/command-contract.md`
- `docs/engineering/validation-tiers.md`
- `docs/engineering/future-dotnet-solution.md`
- `docs/engineering/product-cli.md`

## Intended engineering model

The project will use canonical `eng/` commands once the engineering substrate is created:

```text
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/format.sh
./eng/check.sh
```

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
