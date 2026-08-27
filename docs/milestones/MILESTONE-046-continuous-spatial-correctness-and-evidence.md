# Milestone 046 — Continuous Spatial Correctness and Evidence

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
| Execution prerequisite | M045 COMPLETE with current `m045-smoke --verify` |

M046 closes the remaining M013 **continuous-spatial** gaps. M045 owns runtime snapshots and mutation authority. M046 uses that boundary and fixes only continuous kinematic spatial semantics, diagnostics and evidence.

## Goal

Make `spatial.continuous-kinematic-2d` a truthful deterministic reference module:

```text
immutable runtime snapshot
+ finite movement intent
+ map/static AABB geometry
→ deterministic X-then-Y resolution
→ truthful accepted/clipped/slid/blocked/no-op classification
→ actual limiting-source evidence
→ proposed runtime mutation
→ M045 commit
```

No resolution may claim movement success when the runtime mutation would reject, and no valid non-zero safe displacement may be discarded merely because both axes were constrained.

## Primary Acceptance Question

> For every bounded AABB movement case, does the continuous resolver produce the correct deterministic safe displacement, outcome class and causal collision evidence, with runtime state changing exactly when the accepted proposal commits?

## Preconditions

Implementation begins only after:

```powershell
pwsh ./eng/suite.ps1 m045-smoke --verify
```

passes and M045 is COMPLETE. Do not reintroduce live `EntityComponentWorld` mutation into the resolver.

## Problems Being Corrected

1. non-finite direction inputs can produce success-looking resolution followed by rejected component mutation;
2. partially valid movement on both axes can be classified `blocked` and discarded;
3. `ConstraintSourceId` can identify the first static obstacle rather than the actual limiter;
4. `KinematicMotion2` contains velocity fields that ordinary resolution neither uses nor updates;
5. historical scenario assertion dispatch can default unsupported assertions to passing;
6. historical wrappers prove a narrow subset of the claimed behavior.

## Corrected Contract

### Input validity

Movement direction X/Y must be finite. Stored transform, movement policy and collision shape remain finite through component validation.

Non-finite intent yields:

```text
rejected domain result
stable diagnostic
applied displacement 0,0
no mutation proposal
no factual transform-change event
```

Do not rely on later runtime validation to contradict an already reported success resolution.

### Movement policy

Retain the bounded reference model:

```text
desired direction
→ normalize when magnitude > epsilon
→ multiply by MaxSpeed for one fixed tick
→ resolve X
→ resolve Y
```

No wall-clock time. X then Y is stable.

### Kinematic component semantics

For the current reference module, `KinematicMotion2` is movement-policy state and should contain the authoritative `MaxSpeed` semantics actually used by resolution.

Remove legacy `VelocityX`/`VelocityY` from the current component shape unless implementation discovers a genuinely current consumer that uses persistent velocity semantics and can make those fields authoritative end-to-end within M046. Do not retain dead authoritative-looking fields only for source compatibility.

No released M013-era continuous-component save compatibility is promised. Current M043 semantic compatibility naturally rejects incompatible old definitions.

### Axis result and actual limiter

Each axis result records at least:

```text
requested
applied
constrained
constraintSourceId
```

The axis algorithm returns the geometry/bounds source that actually produced the final limiting displacement. It is not guessed afterward.

If multiple candidates create the exact same limiting boundary, choose the ordinal-lowest stable source ID for `constraintSourceId`; evidence still lists all candidates.

### Outcome classification

Use requested/applied displacement and constraints.

`accepted`:

```text
applied == requested on both axes
```

within the declared epsilon.

`blocked`:

```text
requested displacement is non-zero
AND applied X == 0
AND applied Y == 0
```

A non-zero safe displacement is never blocked.

`slid`: a diagonal request where constraint changes one axis relative to the other while meaningful movement continues along the other requested axis. Canonical examples:

```text
(1,1) -> (0,1)
(1,1) -> (.2,1)
(1,1) -> (1,.4)
```

`clipped`: non-zero safe movement shorter/constrained without the slide semantics. Canonical examples:

```text
(1,0) -> (.4,0)
(0,1) -> (0,.3)
(1,1) -> (.4,.4)
```

`no-op`: finite zero direction/request. It emits no transform mutation proposal and is not a collision block.

The concrete boolean implementation is implementation-owned; the semantic table above is fixed.

### No penetration

Final dynamic AABB must remain inside map bounds and non-overlapping with blocked/static AABBs under the documented touching/epsilon policy.

If the starting AABB already penetrates static collision geometry, reject with a stable diagnostic and no mutation proposal. M046 does not implement depenetration physics.

### Candidate determinism

Candidate enumeration/result must be deterministic and independent of source container insertion order.

Canonical evidence ordering:

```text
map bounds
blocked cells by Y/X or other documented stable coordinate order
static objects by stable object ID
```

The closest valid limiting boundary wins per axis, not the first enumerated obstacle.

### Required geometry cases

Prove at least:

```text
single obstacle
several obstacles on one axis
different obstacles constraining X and Y
corner approach
narrow corridor
map bound + object
blocked cell + object
equal limiting candidates
initial penetration
```

### Mutation/event truthfulness

M046 follows M045:

```text
resolver result
→ optional runtime mutation proposal
→ coordinator commits through runtime transaction
```

Domain resolution may report its spatial outcome. Factual `entity.continuous-transform-changed` occurs only after successful runtime commit.

Blocked/no-op/rejected results have no transform mutation proposal and no factual transform-change event.

### Resolution evidence

Record actual:

```text
intent ID / entity ID
initial transform / collision shape / movement policy
requested direction and displacement
candidate geometry
X axis result + actual limiter
Y axis result + actual limiter
applied displacement
result transform
outcome
mutation proposal ID if any
runtime commit result if submitted
diagnostics
```

Do not conflate predicted domain outcome events with factual runtime mutation events.

### Strict scenario assertions

Unknown or unsupported scenario assertion types fail validation/execution. No fallback may create `Passed = true` for an assertion that was not evaluated.

At minimum support current M046 proof needs for event occurrence, continuous transform, outcome, applied displacement, constraint source, no penetration, diagnostic occurrence and final tick.

### Numeric policy

Use finite `double`, one documented geometric epsilon, negative-zero normalization in serialized evidence, and no premature rounding in collision calculations.

## Scope

- finite intent validation;
- coherent motion-policy component;
- correct X/Y limiter tracking;
- accepted/clipped/slid/blocked/no-op classification;
- multi-obstacle/corner correctness;
- initial/final penetration checks;
- truthful M045 mutation/event linkage;
- strict scenario assertion dispatch;
- deterministic evidence;
- migration of current continuous smoke fixtures as needed;
- machine-derived M046 validation.

## Non-goals

Do not add dynamic entity/entity collision, rigid-body physics, force/mass/impulse, gravity/jumping, non-AABB shapes, rotation, swept high-speed CCD, acceleration/friction, pathfinding, variable timestep, rendering/animation, a third-party physics package, or human review.

## Resolved Decisions

1. M046 depends on completed M045.
2. Resolver consumes immutable snapshot and has no live mutation authority.
3. Non-finite input rejects before success can be reported.
4. `KinematicMotion2` becomes coherent MaxSpeed policy unless genuinely used velocity semantics are found and fully implemented.
5. X then Y remains fixed.
6. Axis resolution returns the actual limiting source.
7. `blocked` means zero valid displacement for a non-zero request.
8. Every non-zero safe displacement is accepted, slid or clipped.
9. `(1,1)->(.4,.4)` is clipped, not blocked.
10. zero direction is no-op.
11. starting penetration rejects; no depenetration.
12. factual transform-change occurs only after runtime commit.
13. unsupported assertions fail.
14. Human review is none.

## Required Authority

Read after `AGENTS.md` and this milestone:

1. `docs/milestones/MILESTONE-045-runtime-snapshot-and-mutation-authority.md`
2. `docs/specs/runtime-snapshot-and-mutation-authority-contract.md`
3. `docs/specs/continuous-kinematic-spatial-correctness-contract.md`
4. `docs/decisions/ADR-0057-evaluation-reads-immutable-runtime-snapshots-and-mutation-commits-transactionally.md`
5. `docs/decisions/ADR-0058-continuous-kinematic-resolution-classifies-safe-displacement-truthfully.md`
6. `docs/specs/entity-component-runtime-contract.md`
7. `docs/scenarios/m046-continuous-spatial-correctness.md`
8. `docs/engineering/command-contract.md`
9. `docs/engineering/validation-tiers.md`

Inspect historical M013 source/tests only as needed. Historical M013 records remain immutable.

## Validation

Execution mode: `resumable-sharded`.

```text
artifacts/spatial/M046/
artifacts/validation/m046-smoke/
```

Precondition and shards:

```powershell
pwsh ./eng/suite.ps1 m045-smoke --verify
pwsh ./eng/suite.ps1 m046-smoke --plan-json
pwsh ./eng/suite.ps1 m046-smoke --shard finite-intent-validation
pwsh ./eng/suite.ps1 m046-smoke --shard outcome-matrix
pwsh ./eng/suite.ps1 m046-smoke --shard constraint-source-attribution
pwsh ./eng/suite.ps1 m046-smoke --shard multi-obstacle-and-corners
pwsh ./eng/suite.ps1 m046-smoke --shard motion-state-semantics
pwsh ./eng/suite.ps1 m046-smoke --shard strict-scenario-assertions
pwsh ./eng/suite.ps1 m046-smoke --shard grid-and-runtime-regression
pwsh ./eng/suite.ps1 m046-smoke --shard deterministic-evidence
pwsh ./eng/suite.ps1 m046-smoke --shard evidence-integrity
pwsh ./eng/suite.ps1 m046-smoke --shard predecessor-regression
pwsh ./eng/suite.ps1 m046-smoke --verify
```

Then:

```powershell
pwsh ./eng/build.ps1
pwsh ./eng/test.ps1
pwsh ./eng/format.ps1 --verify
pwsh ./eng/check.ps1
```

Only current aggregate verification establishes success.

### Shard boundaries

`finite-intent-validation`: NaN/±Infinity and invalid starting state reject with zero displacement, no proposal, no factual transform event.

`outcome-matrix`: table-driven accepted/blocked/slid/clipped/no-op including both-axes-partially-clipped diagonal movement; every nonzero safe result commits.

`constraint-source-attribution`: multiple candidates prove each axis names its actual limiter; equal-limit ties use ordinal source ID.

`multi-obstacle-and-corners`: several obstacles, distinct X/Y limiters, corner, corridor, bounds+object, blocked-cell+object and penetration-free final states.

`motion-state-semantics`: no dead authoritative-looking velocity state; MaxSpeed deterministically drives requested displacement.

`strict-scenario-assertions`: implemented assertions derive real pass/fail; unknown assertion fails rather than defaulting true.

`grid-and-runtime-regression`: M045 runtime boundary and existing grid semantics remain intact.

`deterministic-evidence`: reordered source containers yield equivalent resolution/evidence fingerprint.

`evidence-integrity`: success, commit, constraint-source and no-penetration claims derive from observed values, not existence/constant booleans.

`predecessor-regression`: current M045 verifier and focused M039–M044/runtime regressions remain passing after continuous component changes.

## Completion Audit

Before COMPLETE, confirm M045 prerequisite; finite rejection; no-op behavior; full outcome matrix; no discarded nonzero safe movement; actual limiter attribution; insertion-order independence; penetration-free corner/multi-obstacle cases; coherent motion component; factual event only after commit; unsupported assertions fail; modern evidence supersedes historical wrapper-only proof; all shards/verifier; build/test/format/check; untouched historical M013 records.

## Escalation

Return to planning only if implementation requires changing M045 authority, dynamic-body physics, a different axis order, swept CCD, materially expanding persistent velocity semantics, changing M040–M044 semantics, or adding human review.

Concrete geometry helpers, result records, epsilon mechanics, assertion schema and test organization are implementation-owned.

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
