# Milestone 043 — Canonical Runtime Persistence Unification

## Execution Profile

| Field | Value |
|---|---|
| Lifecycle state | ready |
| Mode | ai-executed-broad |
| Baseline implementation model | GPT-5.6 Luna |
| Repository role | capability-provider |
| Profiles | artifact-first-agentic-authoring; runtime-tool; game-simulation |
| Maturity | implementation-ready; artifact-first |
| Validation | resumable-sharded, active Windows epoch |
| Human review | none |
| Execution prerequisites | M039–M042 are complete in current `main`; no historical milestone is reopened |

M043 is the first corrective milestone closing the persistence fragmentation exposed by the M020 audit.

Historical M020 remains historical evidence. M043 does not rewrite M020. It replaces the current architectural role of the M020-specific persistence runtime and the M035 test envelope with one canonical persistence path over the actual authoritative simulation world.

## Goal

Make the engine have one current durable gameplay persistence architecture:

```text
resolved game/project semantic identity
        +
canonical game-save envelope
        +
actual SimulationWorld v2 authoritative payload
        +
atomic disk/recovery mechanics
        +
player save metadata/catalog as a separate metadata layer
```

The milestone is complete when a save captures and reconstructs the real `SimulationWorld` / typed `EntityComponentWorld` authority rather than a parallel persistence-only runtime.

## Primary Acceptance Question

> Does one canonical persistence service save, validate, reconstruct, inspect, compare, atomically replace and recover the actual authoritative simulation world without a parallel gameplay model or synthetic compatibility identity?

## Problem Being Corrected

Current repository history contains several overlapping persistence shapes:

```text
M020
  agentic2d.canonical-save.v1
  PersistentWorldRuntime / PersistentEntity
  synthetic project/world/content identity

M035
  agentic2d.m035.save-envelope.v1
  test-oriented checksum / atomic write / recovery

M039+
  agentic2d.simulation-world-save.v2
  actual SimulationWorld / typed component authority

M037
  SaveCatalog / SaveRecord / autosave / Continue metadata
```

The durable architecture must converge around the actual world rather than preserving these as competing authorities.

## Target Architecture

### Canonical durable save envelope

Introduce one current outer durable save schema:

```text
agentic2d.game-save.v1
```

Version `1`.

It wraps one authoritative world payload:

```text
agentic2d.simulation-world-save.v2
```

The outer envelope owns durable file identity, compatibility identity, payload integrity and file-level recovery metadata.

`SimulationWorld` remains authoritative for world semantics and world serialization. M043 does not create a second gameplay-state DTO graph.

### Required canonical envelope fields

The canonical envelope contains at least:

```text
schema
version
saveId
projectId
worldId
worldConfigurationId
worldConfigurationFingerprint
semanticContentFingerprint
worldPayloadSchema
worldPayloadVersion
componentRegistrationFingerprint
payloadFingerprint
payloadChecksum
canonicalSaveFingerprint
```

World-configuration fields are required when the active product/world has such a configuration.

Build/game version may be retained as diagnostic metadata, but it is not a semantic compatibility substitute.

Wall-clock save timestamps, player-visible title, manual/autosave classification and catalog ordering metadata belong to the M037 catalog layer and do not alter the canonical semantic save fingerprint.

Machine absolute paths never contribute to save identity.

### Semantic content compatibility identity

`semanticContentFingerprint` MUST be derived from actual resolved semantic content used to reconstruct/continue the world.

It is the canonical fingerprint of a stable ordinal sequence of semantic identity entries:

```text
kind
stableId
schema/version
semanticFingerprint
```

The producer obtains entries from the real project/content/world-configuration resolution path.

Include semantic authored inputs that can alter authoritative reconstruction or future gameplay behavior for the saved world, including applicable:

```text
game/project manifest semantic identity
world configuration identity/fingerprint
referenced entity/item/flag or other gameplay definitions
semantic map/scenario configuration when runtime-relevant
component registration identity/fingerprint
other authored gameplay policy consumed by the saved world
```

Exclude presentation-only assets/definitions, machine paths, generated evidence, diagnostics and wall-clock metadata.

Do not implement `semanticContentFingerprint` as a literal milestone marker, repository revision string, or caller-provided unchecked constant.

The concrete collector/API is implementation-owned; the semantic rule above is fixed.

### Real world payload only

The canonical persistence service captures the actual current `SimulationWorld.Capture()` payload.

Typed component authority remains in `EntityComponentWorld` and is serialized only through `SimulationWorld` registrations/classifications.

Persist only `AuthoritativePersistent` components.

`DerivedRebuildable` state is omitted and rebuilt through current reconstruction behavior.

`ActiveModeTransient`, `PresentationOnly` and `ExternalHandle` state are not gameplay-save authority unless a later explicit contract adds a separate continuation representation.

Activities, reservations, regions, tombstones, semantic clock, sequence and other current `SimulationWorld` authority remain part of the v2 world payload.

### No second contributor/runtime model

M043 MUST NOT preserve a second authoritative persistence runtime such as:

```text
PersistentWorldRuntime
PersistentEntity
PersistentWorldSnapshot
```

as current production persistence.

Do not replace it with another generic property-bag runtime.

Do not add a generic reflection/plugin persistence framework.

The current world owns its canonical world snapshot. The outer persistence service owns durable envelope/file concerns.

### Load pipeline

Canonical load is:

```text
read bytes
→ parse outer envelope
→ validate outer schema/version
→ validate checksum/fingerprints
→ validate actual project/world/config/content compatibility
→ validate embedded SimulationWorld schema/version
→ validate component registration fingerprint and references
→ construct/load a fresh SimulationWorld transactionally
→ rebuild derived state
→ run reconstructed-world invariants
→ publish the new world only on complete success
```

Any failure leaves the caller's existing authoritative world/session untouched.

A failed load does not publish a partially reconstructed world.

### Event and sequence continuation

The canonical payload must preserve every current sequence/epoch value required for deterministic continuation.

After load, future semantic event/command sequence identity must continue from the saved value rather than restart.

A same-process fresh-world continuation test must fail on duplicate/reset event IDs or sequence rollback.

Full separate-process proof belongs to M044.

### Atomic file persistence and recovery

Promote the useful M035 mechanics into the canonical persistence service:

```text
write temporary
→ flush/close
→ read/validate temporary canonical save
→ preserve previous-good according to policy
→ atomic replace
```

Recovery:

```text
inspect damaged current
→ identify previous-good candidate
→ validate previous-good completely
→ restore through temporary output
→ validate restored canonical save
→ atomically replace
```

Never overwrite the only previous-good evidence before replacement validation succeeds.

Use stable diagnostics for malformed/truncated save, checksum mismatch, semantic fingerprint mismatch, unknown required semantic component/reference, unsupported version and recovery failure.

### Compatibility boundary

Current supported durable format after M043:

```text
outer: agentic2d.game-save.v1
world: agentic2d.simulation-world-save.v2
```

Explicitly unsupported as current durable player/runtime formats:

```text
agentic2d.canonical-save.v1
agentic2d.m035.save-envelope.v1
agentic2d.simulation-world-save.v1
```

No migration shim is required for these unreleased historical formats.

Reject them with stable diagnostics. Do not silently deserialize them through compatibility adapters.

### M037 player save layer

`SaveCatalog` remains metadata/catalog authority, not world serialization authority.

M043 establishes a concrete integration boundary:

```text
SaveRecord / catalog metadata
        references
canonical durable save file / SaveId
```

A catalog entry is loadable only if the referenced canonical save validates through the M043 service.

Manual/autosave UI behavior and full Continue/resume end-to-end proof remain M044 scope, but there must no longer be ambiguity about which persistence service they use.

### Product/CLI persistence commands

Current save-oriented product commands must route to the canonical service where supported:

```text
save create
save inspect
save validate
save compare
save recover
```

`save migrate` may report a stable no-supported-migration-path result while only v1 outer saves are supported.

Do not leave product-facing commands routed to `PersistentWorldRuntime`.

`project resume` full continuation proof is M044 scope.

## Legacy Retirement

Direct current consumers of the historical M020 persistence runtime must be migrated, demoted to non-authoritative test fixtures, or removed.

Known audit targets include:

```text
src/Agentic2D.Persistence/Persistence.cs
src/Agentic2D.Persistence/PersistentWorldLoadTransaction.cs
src/Agentic2D.Tools/M020Commands.cs
src/Agentic2D.Tools/M020RuntimeState.cs
src/Agentic2D.Tools/M021AuthoritativeSource.cs
src/Agentic2D.Engineering/PerformanceHost.cs
tests/unit/Agentic2D.Tests.Unit/M020PersistenceTests.cs
```

Search the live tree; this list is not permission to ignore additional direct usages.

M021 presentation evidence may keep a bounded deterministic gameplay fixture, but it must not depend on a fake current persistence authority. Prefer adapting it to current world/event projections or moving purely historical fixture mechanics out of `Agentic2D.Persistence`.

Historical milestone documents and completed review records remain immutable.

## Scope

- one canonical outer game-save envelope;
- actual `SimulationWorld v2` as the only authoritative world payload;
- actual semantic content compatibility fingerprinting;
- canonical inspect/validate/compare;
- atomic write and previous-good recovery;
- strict compatibility diagnostics;
- event/sequence continuation preservation;
- integration boundary to M037 `SaveCatalog`;
- removal/demotion of the parallel M020 persistence runtime;
- removal/demotion of M035 test envelope as current production authority;
- focused current documentation updates;
- machine-derived evidence.

## Non-goals

Do not:

- redesign `EntityComponentWorld`;
- redesign `SimulationWorld` gameplay semantics;
- change M040 work/logistics/needs semantics;
- change M041 fidelity ownership semantics;
- change M042 tolerance/equivalence policy;
- add historical save migrations;
- add cloud saves, compression, encryption or streaming saves;
- add background saving/concurrency;
- introduce arbitrary pluggable serializers or reflection-based persistence;
- persist presentation/native handles;
- redesign M037 UI;
- implement the full fresh-process resume matrix reserved for M044;
- add human review.

## Resolved Decisions

1. `agentic2d.game-save.v1` is the current outer durable save schema.
2. `agentic2d.simulation-world-save.v2` is the only current world payload schema.
3. The actual `SimulationWorld` is saved; no parallel `PersistentWorldRuntime` remains current production authority.
4. Semantic content compatibility comes from actual resolved semantic content, not constants or revision labels.
5. `SimulationWorld` persistence classifications remain authoritative.
6. The outer persistence service owns durable file identity, integrity, compatibility and recovery.
7. M035 atomic-write/recovery concepts are promoted; the M035 test envelope itself is retired as current architecture.
8. M037 `SaveCatalog` remains metadata and references the canonical durable save.
9. M020 and M035 historical formats are unsupported; no migration shim is required.
10. Sequence/event continuation is mechanically verified at M043.
11. Full process-separated continuation is deferred to M044.
12. Human review is none.

## Required Authority

Read after `AGENTS.md` and this milestone:

1. `docs/specs/entity-component-runtime-contract.md`
2. `docs/specs/simulation-world-and-semantic-foundation-contract.md`
3. `docs/specs/save-compatibility-and-recovery-contract.md`
4. `docs/specs/save-catalog-and-autosave-contract.md`
5. `docs/specs/canonical-runtime-persistence-contract.md`
6. `docs/specs/region-fidelity-and-reconciliation-contract.md`
7. `docs/specs/abstract-activity-and-travel-contract.md`
8. `docs/decisions/ADR-0051-close-m031-with-typed-components-and-atomic-semantic-transactions.md`
9. `docs/decisions/ADR-0055-one-canonical-game-save-wraps-simulation-world-v2.md`
10. `docs/architecture/canonical-persistence-architecture.md`
11. `docs/engineering/command-contract.md`
12. `docs/engineering/validation-tiers.md`

Historical M020 documents may be inspected as migration evidence but are not authority for current architecture.

## Files / Areas Likely Affected

```text
src/Agentic2D.Persistence/
src/Agentic2D.Simulation/
src/Agentic2D.Tools/
src/Agentic2D.UI/
src/Agentic2D.GameHost/
src/Agentic2D.Engineering/
tests/unit/Agentic2D.Tests.Unit/
eng/
docs/specs/
docs/architecture/
docs/engineering/
docs/ARTIFACTS.md
docs/ENGINEERING.md
docs/SPECS.md
docs/DECISIONS.md
```

Do not use this list for broad unrelated cleanup.

## Validation

M043 uses a resumable machine-only suite.

```powershell
pwsh ./eng/suite.ps1 m043-smoke --plan-json

pwsh ./eng/suite.ps1 m043-smoke --shard canonical-authority-and-envelope
pwsh ./eng/suite.ps1 m043-smoke --shard real-content-compatibility
pwsh ./eng/suite.ps1 m043-smoke --shard simulation-world-roundtrip
pwsh ./eng/suite.ps1 m043-smoke --shard persistence-classification-and-rebuild
pwsh ./eng/suite.ps1 m043-smoke --shard atomic-write-and-recovery
pwsh ./eng/suite.ps1 m043-smoke --shard sequence-and-identity-continuation
pwsh ./eng/suite.ps1 m043-smoke --shard legacy-runtime-retirement
pwsh ./eng/suite.ps1 m043-smoke --shard product-save-boundary
pwsh ./eng/suite.ps1 m043-smoke --shard evidence-integrity
pwsh ./eng/suite.ps1 m043-smoke --shard current-simulation-regression

pwsh ./eng/suite.ps1 m043-smoke --verify
```

Bash launchers provide equivalent commands.

Receipts:

```text
artifacts/validation/m043-smoke/<shard>.json
```

Domain evidence:

```text
artifacts/persistence/M043/
```

Only `m043-smoke --verify` over current fingerprinted receipts establishes aggregate success.

Then:

```powershell
pwsh ./eng/build.ps1
pwsh ./eng/test.ps1
pwsh ./eng/format.ps1 --verify
pwsh ./eng/check.ps1
```

No M043 human-review gate exists.

## Shard Acceptance Boundaries

### `canonical-authority-and-envelope`

Fail unless the canonical outer schema is `agentic2d.game-save.v1`, the embedded world schema is current SimulationWorld v2, envelope fields are canonical/stable, catalog-only metadata does not affect semantic fingerprint, and no second authoritative gameplay DTO graph is introduced.

### `real-content-compatibility`

Use a real resolved game/project fixture. Prove semantic input changes alter the semantic content fingerprint; presentation-only changes do not; wrong project/world/config/content identity rejects before publication; and the fingerprint is not a hard-coded marker.

### `simulation-world-roundtrip`

Use a real typed `SimulationWorld` with regions, entities, authoritative typed components, activities/reservations and tombstones:

```text
capture A
→ load fresh world
→ capture B without advance
→ canonical A payload == canonical B payload
```

### `persistence-classification-and-rebuild`

Prove executable behavior for all five classifications. No metadata-only proof.

### `atomic-write-and-recovery`

Inject deterministic file failures/corruption. Prove temporary validation, atomic replacement, previous-good preservation, stable diagnostics and safe recovery.

### `sequence-and-identity-continuation`

After same-process fresh-world load and continued semantic commands, event/command sequence does not reset, new IDs do not duplicate pre-save IDs, ordering remains deterministic, and same seeded continuation reruns identically.

### `legacy-runtime-retirement`

Fail if current production persistence still depends on `PersistentWorldRuntime`, `PersistentEntity`, `PersistentWorldSnapshot`, `agentic2d.canonical-save.v1`, or `agentic2d.m035.save-envelope.v1`. Historical docs/artifacts may still contain those strings.

### `product-save-boundary`

Prove M037 catalog metadata references a canonical SaveId/file and validates through the M043 service. Do not claim full Continue equivalence; that is M044.

### `evidence-integrity`

Independent checks derive every pass/fail claim from observed files/runtime values. Artifact existence alone is not acceptance. Producer-authored booleans are not proof.

### `current-simulation-regression`

Exercise representative M039–M042 current world capture/load and continuation structures through the unified persistence boundary without changing gameplay semantics.

## Completion Audit

Before `COMPLETE`, explicitly confirm:

- one canonical outer durable schema exists;
- actual `SimulationWorld v2` is the payload;
- no parallel current `PersistentWorldRuntime` remains;
- actual semantic content drives compatibility;
- typed component classifications are executable;
- event/command sequence continues rather than resets;
- load validates fully before publication;
- atomic write and recovery preserve previous-good state;
- unsupported M020/M035/SimulationWorld-v1 formats fail explicitly;
- M037 catalog has one unambiguous canonical persistence backend;
- current save product commands use the canonical service;
- evidence is independently derived;
- all M043 shards and aggregate verifier pass;
- build/test/format/check pass;
- historical milestone/review records were not rewritten.

## Escalation

Return to planning only if correct completion requires:

- changing `SimulationWorld v2` durable semantic shape incompatibly;
- preserving M020/M035 historical save compatibility;
- replacing `EntityComponentWorld`;
- adding a generic persistence plugin/reflection framework;
- changing persistence classification meanings;
- changing M040–M042 gameplay/fidelity semantics;
- making presentation state authoritative;
- introducing human review.

Concrete APIs, file layout, serializer implementation, checksum algorithm from standard .NET primitives, test fixture mechanics, diagnostics and internal refactors are implementation-owned.

## Terminal Outcome

Terminate with exactly one:

```text
Milestone status: COMPLETE
```

or:

```text
Milestone status: BLOCKED
```

`AWAITING HUMAN REVIEW` does not apply.
