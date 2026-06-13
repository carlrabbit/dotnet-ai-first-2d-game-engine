# Engineering

## Authority

This document indexes build, validation, command contracts, and engineering policy for this repository.

## Current status

The repository has the base engineering substrate established by Milestone 001:

- shared .NET/editor configuration at the repository root;
- executable canonical `eng/` scripts;
- a `.slnx` solution with contracts, engine, tools, and unit test projects.

Milestones 002 and 003 added:

- a minimal deterministic runtime;
- a development product CLI host under `src/Agentic2D.Tools`;
- product CLI validation wrappers for the current command surface.

## Indexed documents

| Document | Purpose |
|---|---|
| `docs/engineering/command-contract.md` | Canonical repository engineering command behavior and validation tier mapping. |
| `docs/engineering/validation-tiers.md` | Validation tier names and expected usage. |
| `docs/engineering/future-dotnet-solution.md` | Current and candidate .NET solution/project layout. |
| `docs/engineering/product-cli.md` | Repository-local product CLI invocation and validation contract. |

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

## Product CLI validation wrappers

The current product CLI validation wrappers are:

```text
./eng/cli-smoke.sh
./eng/product-validate.sh
```

These wrappers validate product CLI behavior through `src/Agentic2D.Tools`. They are repository engineering commands, not the product API.

The product/runtime API is documented separately in:

```text
docs/specs/product-cli-contract.md
docs/engineering/product-cli.md
```

## Future artifact-first commands

The following commands are planned candidates only:

```text
./eng/scenario.sh <scenario-id>
./eng/scenario-smoke.sh
./eng/scenario-packaged.sh <scenario-id>
./eng/content-validate.sh <scope>
./eng/artifacts-validate.sh <artifact-path>
./eng/review-pack.sh <run-id-or-artifact-path>
```

Do not document or rely on future commands as supported until a milestone implements them and updates the command contract.

## Command rule

Commands must either validate meaningful state or fail clearly. Do not create success-only placeholder scripts.
