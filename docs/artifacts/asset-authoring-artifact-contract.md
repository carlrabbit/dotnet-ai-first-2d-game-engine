# Asset Authoring Artifact Contract

## Authority

Authoritative for Milestone 011 asset-review application and asset-perception artifacts.

## Asset review apply

Required files:

```text
<output>/result.json
<output>/diagnostics.json
<output>/mutation-plan.json
<output>/validation-result.json
```

Dry-run additionally requires `<output>/proposed-metadata.json`.

`result.json` schema: `agentic2d.asset-review-apply.result.v1`.

Required fields include command, decision source, asset ID/path, dry-run, expected/actual fingerprints, status/exit code, decision counts, mutation/no-op counts, validation status, and artifacts. Mutation plan identifies target, previous/proposed values, decision ID, and action. Stale input records both fingerprints and no mutation.

## Asset perception

Required files:

```text
<output>/result.json
<output>/diagnostics.json
<output>/tile-features.json
<output>/semantic-proposals.json
```

Schemas:

```text
agentic2d.asset-perception.result.v1
agentic2d.asset-perception.tile-features.v1
agentic2d.asset-perception.semantic-proposals.v1
```

Tile features contain ordered observations and duplicate groups. Semantic proposals contain proposals only, never approvals.

## Policy

Use repository statuses and exit codes. Paths are repository/output relative. Absolute paths and wall-clock values are not semantic comparison fields.
