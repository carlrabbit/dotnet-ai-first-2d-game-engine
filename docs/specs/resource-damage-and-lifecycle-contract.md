# Resource, Damage, and Lifecycle Contract

## Authority

Authoritative for bounded resources, health, damage intents/resolutions, resource transitions, defeat, and lifecycle behavior.

M019 implements `resource.health` with integer values:

```text
minimum <= current <= maximum
maximum > minimum
```

Damage is a positive integer request. Healing is outside M019.

```text
DamageIntent
→ validation
→ DamageResolution
→ resource transition
→ entity.damaged
→ optional entity.defeated
```

Applied damage is capped at remaining health. Duplicate correlation IDs cannot reapply damage.

Defeat emits exactly once when health first reaches minimum and remains distinct from entity removal.

Initial lifecycle states:

```text
active
defeated
inactive
```

Defeated entities remain queryable/renderable but do not execute normal gameplay behaviors. Removal remains an explicit registry transaction.

Presentation may consume damage/defeat events but never controls gameplay outcomes.
