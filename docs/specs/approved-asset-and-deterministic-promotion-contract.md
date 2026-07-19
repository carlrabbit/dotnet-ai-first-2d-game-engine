# Approved Asset and Deterministic Promotion Contract

## Authority

Authoritative for approved presentation assets, deterministic processing, promotion plans/manifests, atomic materialization, provenance, and affected rebuild.

## Schemas

```text
agentic2d.approved-asset.v1
agentic2d.approved-image-region.v1
agentic2d.approved-sprite.v1
agentic2d.approved-animation.v1
agentic2d.approved-audio.v1
agentic2d.approved-presentation-role.v1
agentic2d.asset-processing-recipe.v1
agentic2d.asset-promotion-plan.v1
agentic2d.asset-promotion-manifest.v1
agentic2d.asset-provenance.v1
agentic2d.asset-future-binding-suggestion.v1
```

Approved assets are project-local presentation authority. Future-binding suggestions are not gameplay authority.

Promotion validates current fingerprints and effective canonical decisions, generates stable IDs, stages outputs, validates staging, and atomically replaces the promoted set. Failure preserves the prior valid set.

Allowed processing is bounded and deterministic. Every derivative records recipe and input/output hashes. Promoted output contains no absolute asset-home path, ephemeral alias, or operational input state.
