# Research — Milestone receipts versus enduring regression validation

**Recorded:** 2026-10-12  
**Status:** Observed engineering friction; proposed direction.  
**Authority:** Non-authoritative research. This file does not change active milestone requirements, engineering commands, review policy, or guide-system contracts.

## Question

Why does an implementer repeatedly regenerate M048 evidence while implementing M050 or later changes, rather than running ordinary regression tests? The underlying concern is coupling between lasting behavioral checks, historical acceptance evidence, and the receipt cache's invalidation rules.

## Findings in the current repository

Baseline inspected: `main`, 2026-10-12, commit `e8ed3fd10032a989709a882684f07b49cc7b850a`.

1. `EngineeringHost.Suites` defines **12 M048 machine shards**: subject/bundle integrity, image/animation/audio, decision binding and staleness, review/workbench compatibility, native Raylib capture, evidence integrity, and predecessor regressions. Some exercise real behavior; others are tightly coupled to a milestone's acceptance narrative and outputs.
2. `M050ModalitySpecificPreviewSuite.M048Regression` calls `host.Verify(host.GetSuite("m048-smoke"), ...)` and fails when the **entire** M048 suite is not current, in addition to checking exact M048 review IDs and review-set behavior. The M050 ready specification expressly requires the M048 aggregate prerequisite.
3. `EngineeringHost.RunShardAsync` records `Fingerprints.Repository(root)` on each passing receipt. `Verify` requires exact equality to the current repository fingerprint and separately checks command, suite, input, and artifact fingerprints. `Fingerprints.Repository` covers practically every non-generated repository file; it is *not* an affected-input dependency fingerprint per test.
4. An unrelated code, test, documentation, or execution-ledger edit can therefore invalidate all twelve M048 receipts **without any behavioral test having failed**. M050's prerequisite can require expensive old native-graphics executions again.
5. M048 and M050 currently have a legitimate active correction relationship: M050 improves M048's subjective review experience, and M048 retains its own human-review decisions. Current relevant compatibility verification is necessary. Perpetually requiring the whole earlier milestone's completion evidence is a different claim.

Implementation references: `src/Agentic2D.Engineering/EngineeringHost.cs` (`RunShardAsync`, `Verify`, `Fingerprints.Repository`, `Fingerprints.Input`, `Fingerprints.Result` and suite definitions); `src/Agentic2D.Engineering/M050ModalitySpecificPreviewSuite.cs` (`M048Regression`); `docs/milestones/MILESTONE-050-modality-specific-asset-preview-review-experience.md`; `docs/engineering/validation-tiers.md`.

## Separate the three lifetimes

| Purpose | Expected lifetime | Requirement |
|---|---|---|
| Maintained unit/behavioral regressions | Indefinite, while the capability is supported | Ordinary repeatable tests against current functionality |
| Current integration/consumer checks | When changes or release conditions affect that surface | Real target/platform, explicit input/output, bounded native journeys, reliable negative assertions |
| Milestone acceptance/completion proof | Produced for completion and retained afterward | Criterion/evidence-case-to-exercised-proof trace and historical human decision; not perpetual re-certification |

Guide system **v0.9.1 did not remove evidence**. It requires planning-seeded acceptance/evidence cases, per-criterion validation, a completion audit, and a durable historical proof trace. It explicitly does **not** require reopening completed human reviews or replaying all completed milestones whenever repository state changes. Existing `AGENTS.md` and active platform-epoch policy preserve this distinction.

A stale receipt is not proof of a current regression. A historic green receipt is also not proof that modified current product behavior remains correct.

## Proposed direction — for future planning, not implementation authority

1. **Promote lasting behaviors to maintained tests.** Give shared assertions capability/contract identities independent of milestone numbers (candidate materialization identity, exact preview subject, IPC recovery, review registry, modality-specific controls, input and persistence safety). Run fast coverage through the normal `eng/check` path; keep native graphical checks as explicit affected integration tests.
2. **Use behavioral prerequisites.** Replace M050-style whole-predecessor-verifier requirements with exactly the current behaviors a change could break. Keep transitional M048/M050 acceptance obligations intact while those milestones are still being closed; do not silently rewrite ready contracts.
3. **Use scoped, fail-closed freshness.** A reusable test result depends on its relevant implementation/build outputs, fixture/media, configuration, tools, environment/capabilities and evidence. Unrelated Markdown or execution-ledger changes should not force native recapture. Unknown dependencies must conservatively invalidate, rather than silently accept stale graphical evidence.
4. **Separate archival proof from current test outcomes.** Immutable milestone completion/review evidence remains queryable. Future changes produce current test/consumer evidence, not reconstructed acceptance for unrelated completed milestones.
5. **Audit success-only reports.** Where shards merely assert `true` or predecessor-status prose, require observed behaviors and independent negative checks instead.

## Candidate acceptance experiments

- A typical unrelated `.md` or `.execution/*.md` edit does not force full M048 native recapture for an unchanged M050 behavior.
- Changes to preview identity, subject selection, review dispatch or native presentation select and fail the relevant current regression/integration test.
- M050 or a later dependent consumer can validate its prerequisite contracts without demanding every M048 historical shard receipt.
- `pwsh ./eng/check.ps1` covers maintained fast regressions; separate native tests record the actual Windows graphical execution locus.
- Receipt reuse verifies relevant inputs, artifact fingerprints, commands and environment; unknown affected-input selection uses conservative fallback.
- Historical M048/M050 records, unresolved subjective reviews, original acceptance obligations and active milestone authority are not weakened or rewritten.
- Compare correct regression detection, false cache reuse, irrelevant invalidations, total shard reruns, native launches and wall time using deliberately affected/unaffected changes.

## Planning questions

Which original M048/M050 shards should become unit tests, native integration tests or historical-only proof? What dependency selection safely covers non-.NET fixtures, graphics drivers, external tools and generated files? How should pending M048/M050 closure transition without violating their ready contracts? What minimal current graphical proof should authorize a *new* human review versus historical approval?

**Boundary:** This research identifies an engineering-infrastructure milestone candidate. It does not authorize changing M048/M050 validation during their active implementation or migrating M053 UI workbenches. Resolve material policy choices in a future ready milestone and active project authority.
