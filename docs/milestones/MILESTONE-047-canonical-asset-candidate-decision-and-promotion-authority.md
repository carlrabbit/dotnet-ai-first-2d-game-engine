# Milestone 047 — Canonical Asset Candidate, Decision, and Promotion Authority

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
| Execution prerequisite | M046 COMPLETE with current `m046-smoke --verify` |

M047 closes the authority and materialization gaps found in the historical M029 asset workbench. It does not redesign workbench UX or preview presentation. It makes one mechanically provable chain authoritative:

```text
current raw source bytes
+ current discovery/campaign candidate descriptor
→ canonical candidate fingerprint
→ exact v2 decision subject
→ deterministic versioned processing recipe
→ immutable validated promoted generation
→ atomic current-generation publication
```

## Goal

Make the exact candidate and variant that a decision approves deterministically produce the exact promoted derivative and provenance attributed to that decision.

No candidate may be resolved by ID-name heuristics, file enumeration order, or unrelated sample content. No correction may be recorded as authoritative while being ignored by promotion. No promotion or rebuild command may report success from producer-authored booleans or file existence alone.

## Primary Acceptance Question

> Given a current campaign candidate, can the engine prove that the exact reviewed candidate/variant and structured corrections resolve to the exact source bytes, deterministic recipe, promoted derivative bytes, stable approved identity, provenance, and current promoted generation — and reject every stale, mismatched, unsupported, or partially published alternative?

## Preconditions

Implementation begins only after:

```powershell
pwsh ./eng/suite.ps1 m046-smoke --verify
```

passes and M046 is COMPLETE.

Historical M028/M029 milestone documents and completed review records are evidence, not current implementation instructions. Do not rewrite them.

## Problems Being Corrected

1. M029 workbench sessions currently reduce candidates to opaque IDs without a canonical source-selection descriptor.
2. Promotion can select the first PNG/WAV fixture rather than the candidate's actual source bytes.
3. Media kind and consequence semantics can be inferred from candidate-ID substrings.
4. `choose-alternative` and `approve-with-corrections` can be recorded while promotion ignores the selected alternative/correction.
5. decision staleness is not bound tightly enough to the exact candidate/proposal being promoted.
6. current v1 decision records cannot prove the exact candidate/variant/recipe approved and therefore cannot be trusted promotion authority.
7. staging validation is superficial and can publish internally inconsistent output.
8. generated provenance does not independently prove source, decision, recipe, and derivative linkage.
9. stable approved IDs lack explicit collision semantics.
10. affected-rebuild currently emits success-looking claims without performing a dependency rebuild.
11. evidence can repeat producer-authored `true` values instead of deriving predicates from observed bytes and references.

## Target Authority Chain

```text
Shared asset source registry / immutable raw bytes
        │
        ▼
Discovery profile source-file + region/animation/audio observations
        │
        ▼
Game-local campaign candidate descriptor
  - stable candidate ID
  - exact source-relative file + SHA-256
  - typed media kind
  - normalized selection
  - promotion-relevant proposal/roles
  - typed alternatives
        │
        ▼
Canonical candidate fingerprint
        │
        ▼
V2 decision
  - exact candidate fingerprint
  - exact selected-variant fingerprint
  - typed corrections
  - consequence response
        │
        ▼
Versioned deterministic processing recipe
        │
        ▼
Immutable staged generation
  - derivative bytes
  - approved definitions
  - provenance
  - manifest + hashes
        │ independent validation
        ▼
Atomic current-generation authority switch
```

The shared asset home remains authoring infrastructure. Promoted project content remains project-local presentation authority. Gameplay binding remains separate.

## Canonical Candidate Resolution

Authority: `docs/specs/canonical-asset-candidate-contract.md`.

A promotable candidate is a structured descriptor, not merely a candidate ID.

The resolver must establish at least:

```text
campaignId
candidateId
sourceId
source-relative file path
exact source-file SHA-256
media kind
normalized source selection
promotion-relevant presentation role/proposal
available typed variants
candidate fingerprint
```

Canonical candidate identity never depends on:

- absolute asset-home path;
- source enumeration order;
- filename/display-name heuristics beyond structured discovery facts;
- candidate-ID substring parsing;
- current machine path;
- timestamps;
- workbench aliases or input state.

The media kind and selection come from structured source/profile/campaign authority.

Supported source-selection baseline:

```text
image-file
image-region
animation-sequence over explicit image regions
audio-file
```

Audio trimming is a processing correction, not a source-identity heuristic.

A newer source/profile/campaign container may still satisfy an existing decision only when the exact candidate and selected variant re-resolve to the same canonical fingerprints and the exact referenced source-file hash is current. An unrelated container change alone does not stale a decision.

Source unavailable, source file missing, source-file hash mismatch, candidate missing, candidate fingerprint mismatch, variant mismatch, or invalid selection makes the decision stale/non-promotable.

## Decision Authority

Authority: `docs/specs/asset-workbench-decision-and-consequence-contract.md`.

Current promotion authority uses `agentic2d.asset-review-decision.v2`.

A v2 decision binds:

```text
canonical candidate ID + fingerprint
selected variant ID + fingerprint, when any
typed corrections
consequence presentation/response when applicable
decision sequence and supersession
```

Free text remains reason/note authority only. It must not encode an unparsed crop, frame order, pivot, audio trim, or other processing operation.

`choose-alternative` must select a current typed variant. `approve-with-corrections` must contain supported typed corrections. Unknown or unsupported correction types fail decision validation or make the decision non-promotable; they are never silently ignored.

Group decisions bind the exact candidate fingerprints in the group at decision time. Effective promotion resolves group decisions to deterministic candidate-level effective decisions; a later individual decision supersedes the group result only for that candidate.

Presentation-only approval remains presentation-only and creates no gameplay binding.

### V1 compatibility decision

Historical `agentic2d.asset-review-decision.v1` records may remain readable for history/inspection, but they are not current promotion authority because they do not prove the exact candidate/variant/recipe binding required by M047.

There is no automatic semantic migration from v1 to v2.

A candidate previously approved only by v1 requires explicit current re-review to produce a v2 decision before M047 promotion succeeds.

Existing M029 workbench session/input schemas may remain operationally compatible. M047 changes decision/promotion authority, not the RDP-safe input model.

## Processing Recipe Contract

Authority: `docs/specs/approved-asset-and-deterministic-promotion-contract.md`.

Every promoted derivative is produced by an explicit versioned recipe. The recipe fingerprint includes operation IDs, versions, ordered parameters, exact candidate/variant fingerprint, and exact input hashes.

M047 must support the deterministic subset needed to honor current M029 image/animation/audio approval semantics:

```text
exact source copy
exact image-region crop
transparent-padding preserve or deterministic alpha-bounds trim
positive integer nearest-neighbor scale
pivot/anchor metadata
explicit animation frame ordering
audio exact copy
audio trim on PCM sample-frame boundaries
```

Gain adjustment, channel conversion, resampling, opaque enhancement, and generative alteration are not M047 requirements. If encountered as a correction, they are unsupported and block promotion rather than being ignored.

For image operations, pixel selection and nearest-neighbor integer scaling are exact. For audio trim, boundaries are integer PCM sample-frame indexes. Metadata-only corrections do not mutate derivative bytes but remain in the approved definition and recipe fingerprint.

## Stable Approved Identity

Approved identity is stable across derivative revisions for the same logical campaign candidate and presentation role.

The canonical approved identity tuple is:

```text
campaignId
candidateId
approved asset kind
presentation role
```

The current approved ID is deterministically derived from that tuple using full SHA-256 identity material or an equivalently collision-resistant full canonical digest representation. Truncated IDs may be exposed only when the full identity is still retained and collision is mechanically detected.

Two different identity tuples must never silently resolve to one approved ID. Collision/mismatch is a hard validation failure.

Candidate/variant/recipe fingerprints are revisions/provenance, not the stable approved identity.

## Promotion and Publication

Promotion builds an immutable generation and validates it before publication.

A current promoted workspace has one authoritative current-generation record that identifies one immutable validated generation.

Required semantics:

```text
plan
→ stage immutable generation
→ materialize all derivatives/definitions/provenance
→ independently validate staged generation
→ derive generation fingerprint from canonical staged content
→ atomically replace the small current-generation authority record
→ read back and validate the newly current generation
→ report success
```

Do not publish by mutating live approved files in place.

Do not make a whole-directory remove/move window the only source of atomicity. A process failure before the authority-record switch leaves the prior generation current. A process failure after a successful authority-record switch leaves the new complete validated generation current.

Abandoned staging or non-current generations are cleanup concerns and are never current authority.

Promotion success is emitted only after readback proves that the current authority record resolves to the exact generation just validated.

## Approved Validation

`asset approved validate` validates observed current content, not producer claims.

It must derive at least:

- current-generation authority record is valid and references an existing immutable generation;
- every approved definition schema is current and valid;
- approved IDs are unique and identity-consistent;
- every derivative exists at a safe project-relative path;
- every derivative SHA-256 matches provenance/manifest;
- every source provenance entry includes exact source-relative path and source-file hash;
- every decision reference is a current v2 effective decision for the exact candidate/variant fingerprint;
- every recipe is supported, versioned, and fingerprint-consistent;
- every internal reference resolves;
- no absolute asset-home/local machine path leaks into promoted semantic content;
- no workbench alias, partial input, session path, or operational input state leaks into promoted semantic content;
- no duplicate/conflicting approved identity exists;
- generation fingerprint is independently reproduced from canonical content.

Validation fails for legacy v1 promoted workspaces as current M047 authority. A diagnostic may identify them as legacy/untrusted; promotion may replace them with a v2 generation.

## Deterministic Serialization and Reproducibility

Semantic JSON written as current promoted authority uses deterministic canonical ordering, UTF-8 without BOM, and LF line endings. Object/property mechanics may follow repository conventions, but byte output must be stable for equivalent semantic input.

Promoting the same canonical inputs from different absolute asset-home locations must produce byte-identical derivative bytes and semantically/byte-identical canonical promoted authority, excluding explicitly operational command-output wrappers outside the promoted generation.

No timestamp, process ID, absolute path, random staging name, or enumeration order participates in generation identity.

## Provenance

Each promoted derivative/definition records or can resolve:

```text
sourceId
source-relative file path
exact source-file SHA-256
canonical candidate ID + fingerprint
selected variant ID + fingerprint when applicable
v2 decision ID + decision fingerprint
processing recipe ID/version/fingerprint
output derivative path
output derivative SHA-256
approved stable identity
current generation fingerprint
```

Provenance is independently cross-validated. It is not established by producer-authored `containsX=false`, `atomic=true`, or similar booleans.

## Command Compatibility

Preserve the existing product command family unless a current product-CLI contract requires a compatible alias:

```text
asset batch apply-review
asset batch promotion-plan
asset batch promote
asset approved validate
asset approved inspect/list/show
```

Their current authority/output schemas may advance to v2 as specified by the permanent contracts.

`asset rebuild --affected` is not a successful capability in M047. Until M049 implements real dependency-aware rebuild, invoking it must return a stable non-success/unsupported diagnostic or the command may be removed if current product-CLI compatibility permits. It must not emit success-looking `changedDependenciesOnly=true` evidence without performing a rebuild.

## Scope

- canonical structured candidate descriptors over M028 source/profile/campaign truth;
- exact source-file and selection resolution;
- candidate/variant fingerprints;
- v2 decision subject binding and candidate-scoped staleness;
- deterministic effective group/individual decision resolution;
- structured corrections for the M047 supported processing subset;
- versioned deterministic recipes;
- exact source/output hashes and provenance;
- stable approved identities with collision detection;
- immutable promoted generations;
- atomic current-generation publication and crash/fault recovery semantics;
- independent full approved-generation validation;
- byte/path-independent deterministic repeatability;
- retirement of fake affected-rebuild success;
- current M028/M029 operational regression coverage where not contradicted by corrected authority;
- machine-derived evidence only.

## Non-goals

Do not implement:

- actual candidate audiovisual preview correctness or workbench visual/audio redesign — M048;
- human judgment of crop, animation, sound, or UX — M048;
- runtime/renderer/audio consumption of promoted content — M049;
- real dependency-graph affected rebuild — M049;
- game bindings or gameplay semantics;
- image/audio generation or semantic enhancement;
- audio resampling, gain, channel conversion, codecs beyond the current deterministic PCM/WAV baseline;
- new remote/multi-user asset service or database;
- package/release/export integration;
- broad documentation synchronization;
- guide migration;
- TBPs or issue templates.

## Resolved Decisions

1. M047 uses `ai-executed-broad`, lifecycle `ready`, baseline `GPT-5.6 Luna`.
2. M046 COMPLETE/current verifier is the execution prerequisite.
3. One canonical structured candidate descriptor binds campaign intent to exact source bytes and selection.
4. Candidate IDs are opaque stable identity; no semantic/media behavior is inferred from their text.
5. Exact candidate fingerprint and selected-variant fingerprint are promotion guards.
6. Staleness is candidate-scoped: unrelated container changes do not invalidate a decision when exact candidate/variant/source-file identity re-resolves unchanged.
7. Missing/unavailable/mismatched source, candidate, variant, or selection blocks promotion.
8. Current promotion decisions are v2; v1 decisions remain historical/readable but non-promotable and require explicit re-review.
9. Session/input v1 compatibility is preserved unless implementation discovers a direct contradiction.
10. Authoritative corrections are typed; unsupported operations fail rather than being ignored.
11. M047 supports exact copy, image crop/padding/integer scale/pivot metadata, animation frame order, audio copy, and sample-frame trim.
12. Approved stable identity excludes mutable recipe/fingerprint revisions and uses a collision-detectable canonical digest.
13. Promotion publishes immutable validated generations through an atomic current-generation authority record.
14. Independent validation derives all success predicates from observed content/hashes/references.
15. Legacy v1 promoted workspaces are not current trusted M047 authority.
16. Promotion is path-independent and canonical promoted semantic output is byte deterministic.
17. No fake affected-rebuild success remains; M049 owns real affected rebuild.
18. M048 owns subjective/preview correctness; M049 owns consumer/runtime integration.
19. Human review is none.

## Required Authority

Read after `AGENTS.md` and this milestone:

1. `docs/specs/canonical-asset-candidate-contract.md`
2. `docs/specs/asset-workbench-decision-and-consequence-contract.md`
3. `docs/specs/approved-asset-and-deterministic-promotion-contract.md`
4. `docs/specs/shared-asset-home-and-source-registry-contract.md`
5. `docs/specs/reusable-asset-discovery-profile-contract.md`
6. `docs/specs/asset-campaign-and-batch-contract.md`
7. `docs/specs/asset-workbench-session-and-alias-contract.md`
8. `docs/decisions/ADR-0059-promotion-authority-is-exact-candidate-bound-and-generation-published.md`
9. `docs/engineering/command-contract.md`
10. `docs/engineering/validation-tiers.md`
11. `eng/platform-verification.json`

Inspect the historical M029 milestone, source, tests, and completed review only as needed to preserve non-contradicted operational behavior. Historical M029 records remain immutable.

Ordinary implementation must not read `.guide-profile.json`, `.guide-sync/`, external guide internals, or prompt templates.

## Acceptance Criteria

M047 is complete only when all are true:

1. two distinct candidates backed by distinct source files resolve to distinct exact source hashes; approving one can never materialize the other's bytes;
2. media kind and source selection are obtained from structured authority, not candidate-ID substrings or first-file enumeration;
3. candidate fingerprint changes when any promotion-relevant source/selection/proposal/variant input changes and remains stable across absolute-path relocation;
4. an unrelated profile/campaign change does not stale an otherwise identical canonical candidate;
5. unavailable/missing/changed referenced source bytes stale the decision and block promotion;
6. v2 decisions bind exact candidate and selected-variant fingerprints;
7. v1 decisions cannot authorize current promotion and require explicit v2 re-review;
8. later individual decisions deterministically supersede earlier individual/group results for that candidate without rewriting history;
9. selected alternatives and every supported structured correction change the plan/materialization exactly as specified;
10. unsupported corrections fail; none are silently ignored;
11. stable approved IDs remain stable across recipe/derivative revisions of the same logical identity and collisions/conflicting identity are rejected;
12. every derivative is reproducibly derived from exact source bytes through a versioned recipe and has independently verified input/output hashes;
13. staging validation detects missing/corrupt derivatives, bad hashes, bad schemas, unresolved references, ID collisions, unsafe paths, stale decisions, unsupported recipes, and leaked operational/absolute-path state;
14. no generation becomes current before complete independent validation;
15. deterministic fault/process-failure injection before publication leaves the previous valid generation current;
16. after the current-generation authority switch, readback resolves one complete validated generation and promotion reports success only then;
17. identical semantic inputs in two different asset-home absolute locations produce equivalent/byte-identical canonical promoted generations and derivative bytes;
18. `asset approved validate` fails a manipulated/corrupt current generation and independently reproduces the generation fingerprint for valid output;
19. legacy v1 promoted content is diagnosed rather than accepted as current M047 authority;
20. `asset rebuild --affected` no longer reports fabricated success while real rebuild remains M049 scope;
21. M029 session/input/navigation behavior needed by current authoring remains functional unless directly contradicted by v2 authority;
22. evidence assertions are observation-derived and fail when test fixtures are deliberately corrupted;
23. current M046 and focused M028/M029 regressions remain passing;
24. direct authority docs and indexes are current, with historical M029 milestone/review untouched;
25. all required validation, standard build/test/format/check, and completion audit pass.

## Validation

Execution mode: `resumable-sharded`.

Receipt root:

```text
artifacts/validation/m047-smoke/
```

Expected semantic evidence root:

```text
artifacts/assets/M047/
```

The suite uses current repository/suite/command/input fingerprints through the existing engineering host. A child command's partial output is never aggregate success. Only the current verifier over all required receipts establishes M047 machine acceptance.

### Commands

```powershell
pwsh ./eng/suite.ps1 m046-smoke --verify
pwsh ./eng/suite.ps1 m047-smoke --plan-json

pwsh ./eng/suite.ps1 m047-smoke --shard candidate-identity-resolution
pwsh ./eng/suite.ps1 m047-smoke --shard decision-binding-and-staleness
pwsh ./eng/suite.ps1 m047-smoke --shard alternatives-and-corrections
pwsh ./eng/suite.ps1 m047-smoke --shard recipe-materialization
pwsh ./eng/suite.ps1 m047-smoke --shard promoted-generation-validation
pwsh ./eng/suite.ps1 m047-smoke --shard atomic-publication-recovery
pwsh ./eng/suite.ps1 m047-smoke --shard repeatability-and-path-independence
pwsh ./eng/suite.ps1 m047-smoke --shard stable-id-collision
pwsh ./eng/suite.ps1 m047-smoke --shard legacy-and-fake-capability-cleanup
pwsh ./eng/suite.ps1 m047-smoke --shard m028-m029-operational-regression
pwsh ./eng/suite.ps1 m047-smoke --shard evidence-integrity
pwsh ./eng/suite.ps1 m047-smoke --shard predecessor-regression

pwsh ./eng/suite.ps1 m047-smoke --verify
pwsh ./eng/build.ps1
pwsh ./eng/test.ps1
pwsh ./eng/format.ps1 --verify
pwsh ./eng/check.ps1
```

### Shard Contract

`candidate-identity-resolution`: distinct real source files/regions/animation/audio candidates resolve exact typed descriptors and hashes; IDs are opaque; reordered inventories and relocated asset homes do not retarget candidates.

`decision-binding-and-staleness`: v2 binding, source/candidate/variant changes, missing/unavailable source, unrelated-container change, v1 non-promotability, supersession, and group candidate-fingerprint binding.

`alternatives-and-corrections`: typed alternatives and the supported image/animation/audio correction subset affect effective plans; unsupported/free-text processing attempts cannot silently materialize.

`recipe-materialization`: exact source-to-output byte linkage, recipe version/fingerprint, crop/padding/scale/pivot/frame-order/audio-copy/sample-frame-trim semantics, and provenance hashes.

`promoted-generation-validation`: full independent schema/path/reference/hash/identity/decision/recipe/generation validation plus deliberate corruption cases.

`atomic-publication-recovery`: deterministic faults before and around publication prove the old or new complete generation is authoritative, never a partial generation; success requires readback after current-authority switch.

`repeatability-and-path-independence`: two clean roots and repeated runs produce byte-equivalent promoted authority/derivatives and identical generation fingerprint with no absolute-path/timestamp/random-state influence.

`stable-id-collision`: stable ID persists across derivative revisions and intentionally forced/truncated collision scenarios reject conflicting identity rather than aliasing.

`legacy-and-fake-capability-cleanup`: v1 workspace/decision diagnosis is explicit and non-promotable; fake affected rebuild cannot return success; old constant success flags cannot satisfy current validators.

`m028-m029-operational-regression`: current source registry/discovery/campaign and non-contradicted workbench session/input/alias/history behavior remains available.

`evidence-integrity`: manipulate source bytes, derivative bytes, manifests, hashes, references and claimed booleans; current evidence/verifier must derive failure from observed state rather than trust claims.

`predecessor-regression`: current M046 verifier plus focused current platform/core regressions remain passing.

## Completion Audit

Before COMPLETE, verify:

- M046 prerequisite current;
- canonical candidate resolution uses actual structured source identity;
- no first-file or ID-substring routing remains in promotion authority;
- v1 decisions cannot promote;
- exact variant/correction semantics reach materialized output;
- all supported recipes deterministic and versioned;
- source/output hashes independently verified;
- stable identity and collision behavior correct;
- immutable generation publication and recovery correct;
- current-generation readback required for success;
- negative validation catches deliberate corruption;
- path-independent repeatability proven;
- fake affected rebuild removed/disabled;
- no accidental M048 preview/human-review or M049 consumer scope;
- M028/M029 operational regressions preserved where compatible;
- all m047 shards and verifier current;
- build/test/format/check pass;
- direct project truth updated;
- historical M029 milestone/review records untouched.

Passing tests alone is not completion if any contract/documentation/cleanup obligation above remains unresolved.

## Direct Documentation Impact

Planning authority created/replaced by the M047 overlay:

- this milestone;
- `docs/decisions/ADR-0059-promotion-authority-is-exact-candidate-bound-and-generation-published.md`;
- `docs/specs/canonical-asset-candidate-contract.md`;
- `docs/specs/asset-workbench-decision-and-consequence-contract.md`;
- `docs/specs/approved-asset-and-deterministic-promotion-contract.md`.

Apply the merge-safe index/terminology edits in `M047-APPLICATION-INSTRUCTIONS.md` before disconnected implementation.

Implementation updates directly contradicted product/engineering command documentation and artifact descriptions as required by the finished implementation. Do not perform broad documentation synchronization.

## Deferred Documentation Synchronization

`.guide-sync/pending/2026-08-27-m047-asset-authority-sync.md` records broader post-implementation documentation reconciliation. It is not implementation authority and ordinary implementation agents must not read it.

## Human Review

Applicability: `none`.

M047 acceptance is candidate identity, hashes, deterministic recipes, staleness, validation, atomic publication, provenance, compatibility, and evidence integrity. These are mechanically decidable.

M048 owns subjective visual/audio/UX review using the actual candidate preview experience.

No `.review/` request is created. `AWAITING HUMAN REVIEW` does not apply.

## Constrained Execution

M047 is intentionally resumable-sharded. Each shard is a bounded independent invocation with a fingerprinted receipt under `artifacts/validation/m047-smoke/`.

Run `--plan-json`, run each required shard independently, then run `--verify`. Do not background validation, inflate timeouts, infer aggregate success from logs, or accept a subset of passing shards.

Capability-provider validation is authoritative for M047. Consumer/runtime dogfood is explicitly deferred to M049; do not invent a consumer acceptance gate here.

## Baseline-Executability Audit

Before marking this package ready, planning resolved:

- architecture: canonical candidate → v2 decision → recipe → immutable generation → atomic current authority;
- semantics: exact candidate/variant binding, supported corrections, stable identity, staleness, provenance, validator behavior;
- compatibility: v1 decisions/workspaces non-promotable, v1 session/input operational compatibility, no automatic migration;
- scope: preview/human review M048; runtime consumption/affected rebuild M049;
- acceptance: exact positive/negative outcomes and corruption/fault cases;
- validation: resumable m047 suite, current verifier only;
- human review: none;
- platform: current Windows epoch with portable semantics.

Remaining choices — concrete record/type names beyond schema IDs, storage helpers, serializer implementation, transaction helper, test organization, internal refactoring sequence, and exact code-file layout — are local implementation mechanics.

No stronger executor is required to resolve project-level uncertainty.

## Escalation Boundary

Return to planning only if implementation requires changing one of these material decisions:

- trusting/migrating v1 decisions as current promotion authority;
- broad container-level staleness instead of exact candidate/variant re-resolution;
- adding a processing operation whose numeric/byte semantics are not already fixed here;
- changing stable approved identity semantics;
- abandoning immutable-generation + atomic-current-authority publication;
- making promoted v1 content current trusted authority;
- implementing candidate preview/human-review scope in M047;
- implementing consumer/runtime integration or real affected rebuild in M047;
- adding remote/database/multi-user authority;
- adding human review;
- changing external guide or release/public scope.

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
