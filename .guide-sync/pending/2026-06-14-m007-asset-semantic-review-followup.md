# Guide Sync Hint — M007 Asset Semantic Review Follow-up

## Status

Pending.

## Created by

Milestone 007 planning package.

## Purpose

Preserve human-review follow-up for the first asset metadata and tile atlas curation slice.

## Scope

After implementation and human review, inspect the generated asset metadata and asset inspection artifacts for semantic boundary clarity.

Review questions:

- Are structural metadata, proposed visual labels, and approved physical/gameplay behaviors clearly separated?
- Are visual labels visibly proposals rather than approved source truth?
- Do approved physical/gameplay semantics require explicit review evidence?
- Do `result.json`, `asset-summary.json`, `tiles.json`, and `diagnostics.json` provide enough evidence for later agents?
- Is the sample fixture obviously a structural validation fixture rather than production art?

## Potential documentation updates

Update only if needed:

```text
docs/CONTENT.md
docs/specs/asset-metadata-contract.md
docs/artifacts/asset-inspection-artifact-contract.md
docs/HUMAN-REVIEW.md
```

Do not create public docs, TBPs, issue templates, workflow docs, or release docs unless a later task explicitly activates those layers.

## Completion criteria

Delete this hint only when the semantic boundary has been reviewed and either no documentation change is needed or the needed project-truth updates are applied.
