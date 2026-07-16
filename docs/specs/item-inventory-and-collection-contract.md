# Item, Inventory, and Collection Contract

## Authority

Authoritative for authored item definitions, world-item components, inventory state, capacity/stack rules, collection intents/resolutions, atomic collection, and world ownership.

Initial item:

```text
item.collectible-crystal
```

Item definitions contain stable ID, stackability, maximum stack, tags, visual reference, collection cue, and provenance.

A world-item entity references an item definition and positive quantity.

Inventory is entry-based:

```text
inventory ID
maximum distinct entries
entries(item definition ID, quantity)
revision
```

Entries sort by item-definition ID. No grid slots or equipment.

Collection is all-or-nothing:

```text
CollectItemIntent
→ validation
→ CollectionResolution
→ atomic inventory update + world-item removal
→ item.collected
```

Failure leaves inventory and world unchanged. Events emit only after commit. A removed item cannot be collected again.
