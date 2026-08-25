# Save Compatibility and Recovery Contract

## Authority

Authoritative for M035 save-version support, migrations, corruption handling, atomic recovery, and reference saves.

## Compatibility policy

For the current unreleased simulation foundation, `agentic2d.simulation-world-save.v2` is the minimum supported SimulationWorld schema. `agentic2d.simulation-world-save.v1` is explicitly unsupported and must fail validation without mutating the destination; no v1 migration shim is provided.

Declare:

- current schema;
- minimum supported prior schema or explicit boundary;
- migration sequence;
- forward-incompatible behavior;
- required/optional unknown type handling;
- checksum/fingerprint policy.

## Load transaction

Validate and migrate completely before replacing authoritative world state.

Failure leaves the destination world unchanged.

## Atomic save

Write temporary output, validate, atomically replace, and preserve previous good save according to policy.

## Recovery

Required operations:

```text
save inspect
save validate
save migrate
save recover
save compare
```

Recovery never overwrites the only previous-good evidence without explicit command authority.

## Corruption cases

- truncation;
- checksum mismatch;
- malformed envelope;
- unknown required component/trigger;
- missing reference;
- invalid transition/fidelity ownership;
- incompatible version.

Each fails with stable diagnostics.

## Reference saves

Retain manifests and repository-approved fixtures for stable settlement, construction, carrying, triggers, transition, shortage, failure, and prior supported schema.

Binary fixtures may be generated/retained by implementation policy but are not part of this planning package.

## Canonical authority

Migration must preserve declared semantic equivalence and produce before/after fingerprints and reports.
