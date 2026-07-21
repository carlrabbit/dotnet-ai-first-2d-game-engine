# Asset Campaign and Batch Contract

## Authority

Authoritative for game-local interpretation of shared profiles and bounded proposal batches.

## Campaign

Schemas:

```text
agentic2d.asset-campaign.v1
agentic2d.asset-campaign-status.v1
agentic2d.asset-campaign-proposal.v1
agentic2d.asset-unresolved-decision.v1
```

A campaign contains canonical ID, target game/workspace, source/profile fingerprints, requested presentation roles, taxonomy/choices, batches, review policy, future integration targets, fallback policy, and completion criteria. Campaign files are game-local truth or bounded provider fixtures.

## Batch

Schemas:

```text
agentic2d.asset-batch.v1
agentic2d.asset-candidate-group.v1
```

A batch is independently inventoryable, proposable, validatable, review-packable, and retryable.

## Authority separation

```text
observed fact: shared profile
reusable correction: shared annotation
game relevance/proposal: campaign
future approved presentation identity: approved asset definition
gameplay behavior: explicit game binding
```

M028 stops at proposals and unresolved decisions. A “chest-like” proposal does not create collision, interaction, container behavior, or progression meaning.

## Reuse proof

Two campaign fixtures reference one profile fingerprint, rank/select/group at least one candidate differently, retain independent unresolved decisions, and leave the shared profile unchanged.
