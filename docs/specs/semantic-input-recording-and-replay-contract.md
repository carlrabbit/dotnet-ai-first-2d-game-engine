# Semantic Input Recording and Replay Contract

## Authority

Authoritative for recording consumed semantic frames, replaying them from normal scenario initial state, compatibility, and equivalence.

## Replay authority

Consumed semantic `InputFrame` records are replay authority. Raw physical/adapter samples are diagnostic provenance only.

## Recording

Record every frame consumed by a tick, linked to scenario, source, input-map revision, actions, pointer state, behavior intents, and runtime outcomes.

## Replay

1. Validate compatibility.
2. Initialize the scenario normally.
3. Supply recorded frames in tick order.
4. Execute normal behavior/runtime resolution.
5. Compare evidence.

Replay does not restore arbitrary runtime snapshots and requires no raylib or physical devices.

## Compatibility

Compare scenario ID/fingerprint, input map ID/revision, relevant content/runtime contract identity, seed, and starting tick assumptions.

Incompatibility is a structured rejection.

## Equivalence

Compare consumed frames, intents, movement and interaction resolutions, commands, events, final components, assertions, and final render-projection fingerprint.

Exclude volatile environment data.

## Non-goals

Not full save/load, snapshot restore, network rollback, prediction, time travel, or cross-version migration.
