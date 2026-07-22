# Settlement Production and Needs Infrastructure Contract

## Authority

Authoritative for M034 farmland, crop production, food storage/consumption, and shelter/comfort capacity.

Farmland state includes designation/plot identity, region/cells, preparation, planted crop, growth progress, water requirement, maturity, harvestable quantity, and revision.

Crop loop:

```text
prepare
→ plant
→ supply water
→ grow by semantic time
→ mature
→ harvest
→ food stack
→ storage
→ consume
```

Growth works in both fidelities. No seasons, disease, soil chemistry, recipes, cooking, or spoilage.

Food uses integer quantity, finite storage, reserve policy, hauling, and fixed need consumption.

Comfort structures define capacity, interaction or abstract location, comfort amount/rate, condition modifier, enabled state, and reservations.

Finite capacity conflicts are explicit and alertable.

Persist crop, water, food, storage, comfort, activities, and reservations.

Diagnostics:

```text
FARM-PREPARE
FARM-PLANT
FARM-WATER
FARM-GROW
FARM-HARVEST
FOOD-STORAGE
FOOD-SHORTAGE
COMFORT-CAPACITY
COMFORT-UNAVAILABLE
```
