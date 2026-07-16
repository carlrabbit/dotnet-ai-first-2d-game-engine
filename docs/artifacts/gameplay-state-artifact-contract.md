# Gameplay State Artifact Contract

## Authority

Authoritative for resource, damage, lifecycle, item, inventory, collection, world-item, diagnostic, and replay evidence.

Required files:

```text
gameplay-result.json
resource-transitions.jsonl
damage-intents.jsonl
damage-resolutions.jsonl
lifecycle-transitions.jsonl
collection-intents.jsonl
collection-resolutions.jsonl
inventory-transitions.jsonl
world-item-transitions.jsonl
gameplay-diagnostics.json
```

Damage evidence reconstructs intent → resolution → resource transition → damaged event → optional defeat.

Collection evidence reconstructs intent → resolution → atomic inventory/world transaction → item-collected event.

Records include before/after state, command/transaction IDs, correlations, identities, ticks, rejection reasons, provenance, and fingerprints.

Do not emit empty placeholder files for unexercised families.
