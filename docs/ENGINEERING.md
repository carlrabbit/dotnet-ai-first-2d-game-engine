# Engineering

## Authority

This document indexes build, validation, commands, and project-layout policy.

## Current status

The repository includes deterministic runtime, product CLI, scenarios, content validation, asset workflows, maps, runtime inspection, behavior/spatial systems, entity/component runtime, definitions and interactions, backend-neutral rendering, and an isolated raylib-cs debug client.

## Indexed documents

| Document | Purpose |
|---|---|
| `docs/engineering/command-contract.md` | Canonical engineering commands. |
| `docs/engineering/validation-tiers.md` | Validation tiers and graphics-capable distinction. |
| `docs/engineering/future-dotnet-solution.md` | Current and candidate solution shape. |
| `docs/engineering/product-cli.md` | Product CLI invocation. |

## Canonical commands

```bash
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/format.sh --verify
./eng/check.sh
```

## Current capability wrappers

```text
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/scenario-smoke.sh
./eng/content-validate.sh scenarios
./eng/content-validate.sh assets
./eng/content-validate.sh maps
./eng/content-validate.sh entities
./eng/content-validate.sh visuals
./eng/asset-inspect-smoke.sh
./eng/review-pack-smoke.sh
./eng/asset-curation-smoke.sh
./eng/asset-review-smoke.sh
./eng/asset-perception-smoke.sh
./eng/map-smoke.sh
./eng/runtime-inspect-smoke.sh
./eng/m011-smoke.sh
./eng/behavior-smoke.sh
./eng/grid-spatial-smoke.sh
./eng/m012-smoke.sh
./eng/entity-runtime-smoke.sh
./eng/continuous-spatial-smoke.sh
./eng/m013-smoke.sh
./eng/entity-definition-smoke.sh
./eng/spatial-query-trigger-smoke.sh
./eng/interaction-smoke.sh
./eng/m014-smoke.sh
./eng/visual-content-smoke.sh
./eng/render-projection-smoke.sh
./eng/raylib-debug-client-smoke.sh
./eng/m015-smoke.sh
./eng/input-content-smoke.sh
./eng/input-mapping-smoke.sh
./eng/input-runtime-smoke.sh
./eng/input-replay-smoke.sh
./eng/m016-smoke.sh
```

## Validation boundary

- Headless structural validation is mandatory in ordinary environments.
- `raylib-debug-client-smoke.sh` requires a documented graphics-capable environment.
- M015 must report graphics smoke as passed, failed, or explicitly skipped; it must not silently claim execution.
- Screenshot capture is explicit and not part of every ordinary run.

## Command rule

Commands must validate meaningful state or fail clearly. Success-only placeholders are prohibited.
