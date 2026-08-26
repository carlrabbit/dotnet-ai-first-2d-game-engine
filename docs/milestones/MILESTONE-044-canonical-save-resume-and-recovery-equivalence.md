# Milestone 044 — Canonical Save Resume and Recovery Equivalence

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
| Execution prerequisite | M043 COMPLETE with a current `m043-smoke --verify` |

M044 is the second persistence corrective milestone.

M043 decides and implements the canonical persistence architecture. M044 MUST NOT redesign that architecture. It proves that the canonical durable save behaves correctly when the producer process is gone, the save is loaded by a separate process, current simulation continuation resumes, product save selection uses real save files, and corruption recovery returns to a valid resumable state.

## Goal

Prove the original M020 promise at the current engine level:

```text
uninterrupted execution
==
execution → canonical durable save → producer exits
          → separate consumer loads → continues
```

for representative current typed-world, detailed, abstract, need, fidelity, tombstone and recovery states.

## Primary Acceptance Question

> Can the M043 canonical save survive real process termination, reload and continuation without state, identity, event, scheduler, executor, fidelity, reservation, content-compatibility or recovery divergence?

## Preconditions

Before M044 implementation:

```powershell
pwsh ./eng/suite.ps1 m043-smoke --verify
```

must pass against the current repository fingerprint and M043's completion audit must be `COMPLETE`.

M044 uses the actual M043 canonical persistence service. It does not retain a test-only alternate serializer.

## Process-Separated Proof Model

Every fresh-process equivalence case uses three roles.

### Control process A

```text
create canonical initial state
→ execute uninterrupted to target
→ emit raw final canonical observations
→ exit
```

### Producer process B

```text
create the exact same canonical initial state
→ execute to declared stable checkpoint
→ write canonical durable save through M043
→ flush/close
→ emit checkpoint provenance
→ exit
```

### Consumer process C

Only after B has exited:

```text
read canonical save from disk
→ reconstruct fresh process/world
→ validate external continuation controls
→ continue beyond checkpoint to same target
→ emit raw final canonical observations
→ exit
```

The engineering runner owns process provenance and verifies:

- A, B and C are distinct OS processes;
- B exited before C started;
- C loaded bytes produced by B;
- C advanced beyond the checkpoint;
- result comparison is performed outside producer-authored pass/fail claims.

A same-process object recreation is not M044 proof.

## Exact Same-Schedule Equivalence

For each case using the same authored execution schedule/policy, the final canonical `SimulationWorld` fingerprint must match exactly.

Also compare exact current authoritative continuation identity where applicable:

```text
semantic clock
command/event sequence
event IDs after checkpoint
correlation/causation
regions and fidelity ownership
execution epochs
scheduler queue/continuation
detailed continuation
abstract continuation
activities/stages/revisions
reservations/status/revisions
resources/inventory/storage/needs
tombstones
component authority
transition revision
```

Derived/presentation-only state is not compared as save authority; required derived state is rebuilt and separately validated.

## Event Identity Continuation

M044 explicitly closes the historical M020 defect where event ordinal state reset after load.

For every process-separated continuation:

- collect the set of semantic event/command IDs allocated before the checkpoint;
- collect post-load IDs;
- require disjoint sets;
- require strictly valid current sequence progression;
- compare the resumed post-checkpoint ordered semantic facts/IDs with the uninterrupted control for the same schedule.

Dropping IDs from the comparison is forbidden.

## Required Canonical Checkpoint Matrix

Use current M040–M042 semantics and M039 world authority.

At minimum prove these stable checkpoint classes.

### `typed-world-active-reservation`

A typed world with an active legal activity/reservation and authoritative resources/components.

Consumer continues the activity to semantic completion.

### `destroyed-entity-tombstone`

A world after deterministic entity destruction/tombstoning.

Consumer continues far enough that the destroyed entity would have reappeared if tombstone semantics were broken.

### `abstract-travel`

An M040 abstract continuation mid-travel at a stable save boundary.

### `abstract-carrying`

An M040 abstract continuation carrying authoritative resources.

### `mandatory-need-interruption`

A stable M040 need/interruption continuation.

### `detailed-carrying`

An M040 detailed executor carrying authoritative resources.

### `immediately-after-materialization`

Stable state immediately after a completed M041 abstract→detailed transition.

### `immediately-after-abstraction`

Stable state immediately after a completed M041 detailed→abstract transition.

### `equal-time-trigger-and-switch-boundary`

Stable state immediately after the M042-defined same-instant switch/trigger boundary has committed.

Never save a deliberately half-committed fidelity transition.

The implementation may reuse current M042 fixture builders and schedule definitions. It must route persistence through M043's actual durable file service.

## External Schedule / Control Validation

M042 execution schedule remains validation/control input rather than gameplay save authority.

For schedule-dependent checkpoints:

- producer records schedule ID/fingerprint as process evidence;
- consumer is explicitly re-supplied the schedule;
- consumer rejects a mismatched schedule fingerprint before continuation;
- the schedule is not smuggled into the gameplay payload solely to make the test pass.

## Product Save / Autosave / Continue Proof

M044 connects the M037 metadata layer to real canonical saves.

Required bounded flow:

```text
new/current world
→ manual save through canonical service
→ catalog record
→ advance world
→ autosave through canonical service
→ catalog record
→ process exits
→ new process resolves Continue
→ validates selected canonical save
→ loads it
→ continues world
```

Prove:

- catalog `SaveId` refers to an actual canonical save;
- Continue orders valid candidates by current M037 policy;
- invalid/incompatible candidates are skipped with diagnostics;
- manual/autosave metadata does not alter semantic save fingerprint;
- retention never deletes manual saves;
- loaded world provenance/configuration matches the selected record;
- resumed continuation is real world continuation, not metadata-only session creation.

This is a headless product/service proof. M044 does not require graphical UI review.

## Corruption and Previous-Good Recovery Proof

Use the M043 durable file path and deterministic fault/corruption injection.

Required cases include:

```text
truncated current save
payload checksum mismatch
malformed outer envelope
semantic-content mismatch
unsupported outer/world schema
unknown required semantic component/reference
```

For corruption where a valid previous-good save exists:

```text
detect invalid current
→ validate previous-good
→ recover atomically
→ start separate consumer process
→ load recovered save
→ continue to declared target
```

Compare against an uninterrupted control started from the same previous-good semantic state and schedule.

Recovery must not manufacture a success artifact without actually loading and continuing the recovered bytes.

For semantic incompatibility, recovery must not silently substitute a semantically incompatible save.

## Save/Load/Save Stability Across Processes

At least one canonical case performs:

```text
producer writes A
→ consumer loads A
→ consumer writes B without semantic advance
```

and requires canonical semantic equality of A and B after excluding explicitly non-semantic catalog/file-write metadata.

## Deterministic Reruns

For every canonical checkpoint family, at least one exact rerun of the same seed/schedule/process pattern must produce identical raw comparison inputs and final canonical fingerprint.

The validation harness may choose a representative subset for byte-for-byte artifact rerun if all checkpoint semantic fingerprints are already exact, but it must include:

```text
one detailed case
one abstract case
one fidelity-transition case
one recovery case
```

## Independent Comparison and Evidence Integrity

Run producers emit raw structured observations.

An independent comparer computes:

```text
process distinctness/order
checkpoint reached
consumer advanced beyond checkpoint
canonical final equality
event/command identity continuity
reservation/activity equality
resource/conservation equality
scheduler/fidelity continuation equality
save A/B semantic equality
catalog/save linkage
recovery provenance
content/schedule compatibility result
```

Producer-authored fields such as `equivalent=true`, `freshProcess=true`, `recovered=true`, or `continued=true` are not acceptance authority.

Artifact existence alone is not acceptance.

## Scope

- process-separated canonical save/resume harness;
- exact event/sequence continuation proof;
- current M040 detailed/abstract continuation checkpoints;
- current M041 post-transition checkpoints;
- current M042 equal-time schedule boundary checkpoint;
- tombstone and active-reservation continuation;
- external schedule fingerprint validation;
- real M037 manual/autosave/Continue integration;
- corruption + previous-good recovery followed by actual continuation;
- cross-process save/load/save stability;
- deterministic reruns;
- independently derived evidence;
- focused documentation updates.

## Non-goals

Do not:

- redesign the M043 persistence envelope;
- add another serializer or save format;
- add historical save migrations;
- change M040 shared semantics;
- change M041 fidelity ownership rules;
- change M042 equivalence tolerances;
- save half-committed fidelity transitions;
- add cloud save or network synchronization;
- add background async save architecture;
- add compression/encryption;
- add graphical save UI review;
- reopen historical M020/M035/M037 reviews;
- repeat the M042 365-day stress campaign unless needed for a concrete regression.

## Resolved Decisions

1. M044 begins only after M043 is COMPLETE.
2. All canonical resume evidence uses the M043 durable save service.
3. Fresh-process means distinct OS processes and producer exit before consumer start.
4. Event/command IDs and sequence continuity are exact and included in comparison.
5. Required checkpoint matrix covers typed/reserved, tombstone, abstract, detailed, need and fidelity-transition states.
6. Stable post-transition states are saved; half transitions are never canonical save points.
7. M042 schedule remains external control input and is validated on resume.
8. M037 catalog/Continue must resolve and load actual canonical save bytes.
9. Previous-good recovery is not complete until a separate process loads and continues the recovered save.
10. Producer success booleans and file existence are not proof.
11. Human review is none.

## Required Authority

Read after `AGENTS.md` and this milestone:

1. `docs/milestones/MILESTONE-043-canonical-runtime-persistence-unification.md`
2. `docs/specs/canonical-runtime-persistence-contract.md`
3. `docs/specs/canonical-save-resume-equivalence-contract.md`
4. `docs/specs/save-catalog-and-autosave-contract.md`
5. `docs/specs/save-compatibility-and-recovery-contract.md`
6. `docs/specs/simulation-world-and-semantic-foundation-contract.md`
7. `docs/specs/shared-work-logistics-and-needs-semantics-contract.md`
8. `docs/specs/abstract-activity-and-travel-contract.md`
9. `docs/specs/region-fidelity-and-reconciliation-contract.md`
10. `docs/specs/multi-fidelity-equivalence-contract.md`
11. `docs/decisions/ADR-0055-one-canonical-game-save-wraps-simulation-world-v2.md`
12. `docs/decisions/ADR-0056-resume-equivalence-requires-process-separated-continuation.md`
13. `docs/scenarios/m044-canonical-save-resume-and-recovery.md`
14. `docs/engineering/command-contract.md`
15. `docs/engineering/validation-tiers.md`

## Files / Areas Likely Affected

```text
src/Agentic2D.Persistence/
src/Agentic2D.Simulation/
src/Agentic2D.UI/
src/Agentic2D.GameHost/
src/Agentic2D.Tools/
src/Agentic2D.Engineering/
tests/unit/Agentic2D.Tests.Unit/
eng/
docs/engineering/
docs/specs/
docs/scenarios/
docs/ARTIFACTS.md
docs/ENGINEERING.md
```

Do not perform unrelated product-shell or gameplay work.

## Validation

M044 uses a resumable machine-only suite.

Precondition:

```powershell
pwsh ./eng/suite.ps1 m043-smoke --verify
```

Plan and shards:

```powershell
pwsh ./eng/suite.ps1 m044-smoke --plan-json

pwsh ./eng/suite.ps1 m044-smoke --shard process-provenance-and-event-identity
pwsh ./eng/suite.ps1 m044-smoke --shard typed-reservation-and-tombstone-resume
pwsh ./eng/suite.ps1 m044-smoke --shard abstract-and-needs-resume
pwsh ./eng/suite.ps1 m044-smoke --shard detailed-resume
pwsh ./eng/suite.ps1 m044-smoke --shard fidelity-boundary-resume
pwsh ./eng/suite.ps1 m044-smoke --shard product-save-autosave-continue
pwsh ./eng/suite.ps1 m044-smoke --shard corruption-and-recovery-continuation
pwsh ./eng/suite.ps1 m044-smoke --shard cross-process-roundtrip-and-reruns
pwsh ./eng/suite.ps1 m044-smoke --shard evidence-integrity
pwsh ./eng/suite.ps1 m044-smoke --shard predecessor-regression

pwsh ./eng/suite.ps1 m044-smoke --verify
```

Bash launchers provide equivalent commands.

Receipts:

```text
artifacts/validation/m044-smoke/<shard>.json
```

Domain evidence:

```text
artifacts/persistence/M044/
```

Only `m044-smoke --verify` over current fingerprinted receipts establishes aggregate success.

Then run:

```powershell
pwsh ./eng/build.ps1
pwsh ./eng/test.ps1
pwsh ./eng/format.ps1 --verify
pwsh ./eng/check.ps1
```

No M044 human-review gate exists.

## Shard Acceptance Boundaries

### `process-provenance-and-event-identity`

Mechanically prove distinct A/B/C process identities and B-exit-before-C-start. Exercise at least one canonical continuation and fail on event/command ID reset, duplicate allocation or sequence rollback.

### `typed-reservation-and-tombstone-resume`

Run the `typed-world-active-reservation` and `destroyed-entity-tombstone` checkpoints through real disk persistence and separate consumer processes. Require exact canonical target state and legal activity/reservation/tombstone semantics.

### `abstract-and-needs-resume`

Run `abstract-travel`, `abstract-carrying`, and `mandatory-need-interruption` through separate-process continuation. Require exact same-schedule target equality.

### `detailed-resume`

Run `detailed-carrying` through separate-process continuation and prove exact authoritative target equality.

### `fidelity-boundary-resume`

Run `immediately-after-materialization`, `immediately-after-abstraction`, and `equal-time-trigger-and-switch-boundary` with actual M041/M042 ownership/epoch/scheduler semantics. Require same schedule fingerprint and exact target equality.

### `product-save-autosave-continue`

Prove real canonical files back catalog records, autosave retention is correct, invalid latest candidates are skipped, Continue loads a real world and advances it.

### `corruption-and-recovery-continuation`

Exercise required corruption classes. Where recovery applies, require recovered bytes to be loaded in a separate process and continued to a target equivalent to the corresponding control.

### `cross-process-roundtrip-and-reruns`

Require cross-process save A → load → save B semantic equality without advance. Run exact representative deterministic reruns for detailed, abstract, fidelity and recovery paths.

### `evidence-integrity`

Independent comparer owns pass/fail. Fail if process freshness, continuation, equality or recovery is established only by producer claims or file existence.

### `predecessor-regression`

Exercise current canonical M043 persistence plus representative M040–M042 semantics to ensure M044 added proof rather than a second runtime path.

## Completion Audit

Before `COMPLETE`, explicitly confirm:

- M043 verifier is current and passing before M044 execution;
- every required checkpoint is run through the M043 canonical disk save;
- control, producer and consumer are distinct processes;
- producer exits before consumer launch;
- consumer advances beyond each checkpoint;
- event/command sequence and IDs do not reset or duplicate;
- exact same-schedule final canonical equality holds;
- typed activity/reservation and tombstone cases continue correctly;
- abstract/detailed/need continuation cases continue correctly;
- M041/M042 fidelity boundary cases continue correctly;
- schedule mismatch rejects before continuation;
- manual/autosave catalog records reference actual canonical saves;
- Continue loads and advances actual canonical world state;
- corruption diagnostics are stable;
- previous-good recovery is followed by actual separate-process continuation;
- cross-process save/load/save equality passes;
- deterministic reruns pass;
- independent comparison derives acceptance;
- all M044 shards and aggregate verifier pass;
- build/test/format/check pass;
- no historical milestone/review was reopened.

## Escalation

Return to planning only if correct completion requires:

- changing the M043 canonical envelope materially;
- changing `SimulationWorld v2` compatibility;
- adding a historical migration promise;
- changing M040–M042 gameplay/fidelity semantics;
- persisting half-transition state;
- changing M037 catalog semantics materially;
- weakening process-separation or exact-equivalence requirements;
- adding human review.

Process launcher mechanics, fixture composition, raw evidence schema, diagnostic codes, local APIs and performance-local implementation are agent-owned.

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
