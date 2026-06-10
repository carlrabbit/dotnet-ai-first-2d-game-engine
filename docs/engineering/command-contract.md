# Command Contract

## Authority

This document is authoritative for engineering command expectations.

## Current state

No executable `eng/` scripts exist yet. This is intentional.

## Future base commands

```text
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/format.sh
./eng/check.sh
```

## Future focused commands

```text
./eng/test-project.sh <project-or-path>
./eng/test-filter.sh <filter>
./eng/check-affected.sh
./eng/schema-validate.sh <path-or-scope>
```

## Future artifact-first commands

```text
./eng/product-validate.sh
./eng/cli-smoke.sh
./eng/scenario.sh <scenario-id>
./eng/scenario-smoke.sh
./eng/scenario-packaged.sh <scenario-id>
./eng/content-validate.sh <scope>
./eng/artifacts-validate.sh <artifact-path>
./eng/review-pack.sh <run-id-or-artifact-path>
```

## Rule for creating commands

A command must either validate meaningful state or fail clearly with an explanation that the required substrate has not been initialized.

Do not create success-only placeholder scripts.
