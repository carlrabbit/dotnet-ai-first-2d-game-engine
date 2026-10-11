# Milestone 051 — Adopt Guide System v0.9.1 and Preserve Agent-Oriented UI Direction

## Execution profile

| Field | Resolved value |
|---|---|
| Lifecycle | **ready** (planning-seeded overlay) |
| Workflow classification | Guide-system migration; ordinary coding/documentation milestone execution |
| Execution mode | `ai-executed-broad` (preserve the project's configured mode) |
| Baseline implementation model | `GPT-5.6 Luna` (existing project metadata; do not change just to match a new default) |
| Baseline executor readiness | Confirmed: material migration and project-direction choices are specified below |
| Decision preservation | Confirmed: all implementation-affecting decisions are in this milestone or referenced project authority |
| Tractability | One coherent migration; executor decomposes tasks using pre-seeded obligations |
| Execution ledger | `.execution/M051.md`, seeded by planning before `ready` |
| Repository role | `capability-provider` with bounded game/workbench dogfood |
| Maturity | `implementation-ready`, with artifact-first specialization |
| Migration | Recorded `0.7.2` → resolved `0.9.1` |
| Active development host | Windows / PowerShell 7; Linux / Bash supported but inactive, with deferred platform proof |
| Human review | **None for M051**: machine-verifiable migration; no subjective review gate |
| Runtime product changes | None |

## Goal

Update `carlrabbit/dotnet-ai-first-2d-game-engine` from the presently recorded guide-system version `0.7.2` to `0.9.1`, accounting for **every intermediate migration**, without importing the external guide's text into the game repository or changing its runtime behavior. Make new AI-executed milestones use the correct planning-seeded, criterion-specific execution-evidence model. Preserve the agreed typed-DSL UI direction for future work without starting its implementation.

The external guide repository is `carlrabbit/agentic-project-guides`; v0.9.1 is already merged there. This ready milestone contains the applicable migration contract and decisions; **ordinary implementation must not need to read that external repository or the planning conversation**.

## Target state and ownership

- `.guide-profile.json` truthfully identifies guide version `0.9.1`, the repository-wide base, the relevant scoped profiles, and project-specific execution/engineering metadata.
- The repo's *active* engineering/specification authority reflects the versioned changes affecting guide interpretation, validation depth and target/locus, execution ledgers, completion evidence and CLI consumer contracts. Old historical milestone records remain intact.
- No new guide methodology, generic prompt templates, copied guide text, duplicate command buses, or unrelated product behavior is added.
- The authoritative new project direction at `docs/architecture/agentic-ui-direction.md` is available to plan M052. M051 does not modify GUI source, workbench workflows, GUI grammar or rendering adapters.

## Scope and exclusions

**In scope:** current-to-target migration and metadata reconciliation, scope/profile selection, active planning/engineering policy updates, stale authoritative references that would mislead upcoming milestones, selective research classification, any necessary deferred documentation-synchronization hints, targeted verification and obligation-specific completion evidence.

**Out of scope:** implementing the typed DSL, writing UI validators/renderers, migrating workbenches, changing command semantics, packaging a new product distribution, historic milestone rewrites, external guide edits, general repository cleanup, full historical research reconstruction, and M048/M050 implementation already in flight on PR #23.

## Resolved profile selection for guide 0.9.1

Use guide-profile schema version **2** and the **additive, unordered** `appliedProfiles` list. Replace legacy selection fields rather than leaving conflicting parallel profile declarations. The resulting profile assignments are:

| Central profile | Scope | Rationale |
|---|---|---|
| `base` | repository | Common project structure and execution method |
| `artifact-first-runtime` | repository | Structured scenarios, projections, evidence, validation and artifact-first consumption are integral to the engine's product thesis |
| `dotnet-library` | component `engine-libraries` | Supported reusable .NET engine runtime/contract/validation/rendering library APIs; **not** a claim that a NuGet release already exists |
| `cli-tool` | surface `product-cli` | Supported `agentic2d` CLI commands under `src/Agentic2D.Tools`, consumed by games/agents; not the internal `eng/` wrappers |

`engine-libraries` is the semantic group of supported reusable engine library APIs, independent of individual `src/` paths. `product-cli` denotes the supported `agentic2d` product command/process interface defined in `docs/specs/product-cli-contract.md` and `docs/engineering/product-cli.md`. Record these definitions in active project authority, not only the metadata.

Legacy labels `artifact-first-agentic-authoring`, `runtime-tool`, and `game-simulation` describe concrete project specialization, **not** central 0.9.1 profile identifiers. Preserve their substantive intent in project-local truth (`README.md`, `docs/specs/project-thesis.md`, etc.) without treating them as stale selectable guide profiles. Do not activate a vendor-specific/game-engine central profile; do not add `source-generator` merely because the initial architecture mentioned a candidate; do not require CLI packaged-install proof until a distributable CLI artifact is actually supported.

Preserve `repositoryRole = capability-provider`, `maturityStage = implementation-ready`, the existing baseline model and `ai-executed-broad` execution-mode choice, Windows/Linux supported engineering launchers, milestone-scoped human review, and separate documentation synchronization/release readiness. The current active Windows development epoch and deferred inactive Linux checks remain explicit in normal engineering authority; don't relabel supported platforms as freshly verified.

## Migration chain: required and conditional transformations

### 0.7.2 → 0.7.3: resumable execution ledger

Retain implementation-time work-package decomposition, durable `.execution/<id>.md` resume state and fresh milestone/ledger/repository reconciliation. The old implementation-created obligation registry is **superseded** by 0.9.0 planning-seeded coverage; use the newer rule for new ready milestones. Do **not** create per-milestone duplicated execution-methodology documents or generic new engineering ledger commands.

### 0.7.3 → 0.7.4: consumer-surface verification

For a milestone changing a supported distributable artifact, require a bounded verification path exercising the **current packaged/published artifact through the consumer's actual entrypoint**. Internal `dotnet run`, builds and unit tests alone are insufficient for consumer-boundary claims. Linux executable export remains an actual supported surface with an existing specific validation policy; Windows export is not implied. A distributable `dotnet tool` install is not currently established merely by naming `agentic2d`; packaged-tool checks become conditional if/when such a supported distribution exists. M051 changes no distributable runtime product, so no new Tier 4 packaged run is needed for M051 itself.

### 0.7.4 → 0.7.5: decision preservation

For every future ready milestone require **both** decision completeness and decision preservation: no material project-level design choice may survive only in conversation, external guide material or non-authoritative research. Capture project-wide outcomes in referenced specs/architecture/decisions; milestone-specific ones in the ready milestone. Do not hand implementation unresolved architecture, semantics, compatibility, acceptance or validation decisions.

### 0.7.5 → 0.8.0: profiles, research and integration-first validation

Use **Tier 0 edit sanity**, **Tier 1 focused**, **Tier 2 standard repository**, **Tier 3 integration**, **Tier 4 consumer/release**, **Tier 5 human review** as validation *depths*, independent of execution location. Record actual target, execution locus, platform/capabilities and evidence where material. CI is not definitionally Tier 3, and integration need not run in CI. Preserve the repository's canonical PowerShell 7 / Bash launchers and current Windows-local graphics/integration reality.

Treat profile composition as additive, scope-aware and semantic, not positional override or feature-tag stacking; update schema-v2 metadata to canonical `appliedProfiles`. Do not preserve old central project-type selections as external authority. Preserve concrete engine/runtime/tool/game specialization in project-local docs.

`docs/research/` is non-authoritative evidence/planning knowledge; not executable project policy. Classify copied historical guides as legacy guide leakage, distinguish real evidence from old methodology, and promote any *currently operative* project requirements found only there into active specs/architecture/engineering if relevant. **Historical research adoption decision: `none` for reconstruction during M051.** Existing valuable research is retained as such; there is no obligation to reconstruct older chats/files, and this choice is not serialized in `.guide-profile.json`. The typed-DSL decision is directly preserved as fresh project authority, not archival research. If a substantial legacy-research reconstruction is later useful, it is a distinct planning/research task, not a hidden migration gate.

### 0.8.0 → 0.9.0: planning-seeded lossless obligations and evidence cases

For each newly planned AI-executed milestone, planning seeds `.execution/<id>.md` **before marking the milestone ready**, with a separate stable `AC-*`/other obligation row for every individually verifiable criterion, optional selective `EC-*` sub-cases for materially different proof paths, and validation gates mapped to precise IDs. The implementer **does not** merge, delete, renumber, or weaken seeded rows; it owns only work-package decomposition, concrete evidence, status/progress and resume metadata. The ledger may compress implementation work, not obligations.

Before `COMPLETE`: establish exact coverage sets, prove each applicable obligation/evidence case with exercised behavior (an aggregate green result is not generic evidence), and retain compact permanent criterion→proof links in milestone completion authority before any ledger cleanup. Do not create the Cartesian product of all possible evidence dimensions.

This M051 ready milestone **includes** `.execution/M051.md` with planning-seeded obligations and validation-gate mappings. Do not rewrite completed historical milestones. Audit draft, ready and active AI-executed milestones currently on the implementation branch and adopt the newer contract where applicable. The concurrently open M048/M050 PR #23 must be reconciled on its owning branch when work resumes; don't force-merge its ledgers or overwrite its work in unrelated M051 changes. A concrete mandatory follow-up on that branch is acceptable, but do not falsely assert that already-active work was retroactively reconciled.

### 0.9.0 → 0.9.1: supported CLI profile

Apply `cli-tool` only to the actual supported **product CLI** surface, not build/engineering scripts. The normal project-authoritative `docs/engineering/product-cli.md` and `docs/specs/product-cli-contract.md` already cover substantial command identity, arguments, structured artifacts and exit codes. Reconcile material uncovered CLI/process rules, where applicable: stdout/stderr, non-interactive behavior, configuration precedence, path anchoring, cancellation/error semantics, help/version, diagnostics, and compatibility. Do not invent a new CLI implementation, force `dotnet tool` packaging or introduce artificial requirements unrelated to currently supported product behavior.

Update applied guide metadata to 0.9.1 only **after** the migration obligations are implemented/verified. Do not claim full upgrade merely because the numeric version string changed.

## Planned UI direction — preservation, not implementation

`docs/architecture/agentic-ui-direction.md` is provided in the overlay. It establishes **a small strongly typed compositional C# DSL** as the sole planned agent-facing UI authoring surface; no manually authored ASCII grammar, WinForms/XAML, or bespoke command system. Targets remain headless semantic rendering, Raylib and a future renderer adapter. Existing simulation/application commands and headless operation controllers own business outcomes; input micro-state and animation are presentation-only. Mechanically correct software is engineered and verified; actual agentic development advantage must later be evaluated empirically on realistic agent tasks. M052–M054 are not implemented here.

The current `docs/specs/ui-toolkit-contract.md` continues to describe implemented M037 behavior until a future implementation milestone changes the UI contract. Do not rewrite it now to falsely claim the typed DSL already exists.

## Required repository-local authority for execution

Start with `AGENTS.md` and this ready milestone. Relevant current project truth:

- `.guide-profile.json` — **migration/planning task only**, existing version, role, maturity, platform and profile metadata;
- `README.md` — repository role, product CLI and public engine identity;
- `docs/ENGINEERING.md` and `docs/engineering/command-contract.md` — supported engineering commands, constrained suite and platform policy;
- `docs/engineering/platform-verification.md` and `eng/platform-verification.json` — active Windows and inactive Linux verification;
- `docs/engineering/product-cli.md` and `docs/specs/product-cli-contract.md` — supported `product-cli` consumer surface;
- `docs/specs/project-thesis.md` — agentic engine product intent;
- `docs/specs/standalone-linux-game-export-contract.md` — supported export/consumer boundary;
- `docs/architecture/player-and-diagnostics-application-architecture.md` — existing UI and application dependency semantics;
- `docs/architecture/agentic-ui-direction.md` — newly established future UI direction;
- `docs/HUMAN-REVIEW.md` and `docs/engineering/human-review-workflow.md` — milestone-scoped human review semantics;
- `docs/MILESTONES.md`, `docs/SPECS.md`, `docs/DECISIONS.md` — indexes, with historical inconsistencies not automatically authoritative;
- `.execution/M051.md` — planning-seeded coverage and validation gates; **operational, not project authority**;
- `.guide-sync/pending/2026-10-11-m051-guide-and-gui-followups.md` — deferred synchronization only, not active implementation authority.

Migration semantics are fully captured above. External guide files, prompts, project planning chat and copied old research guides are **not** required reading for M051 execution.

## Required transformations and direct documentation impact

- Replace conflicting old `.guide-profile.json` fields with canonical scoped 0.9.1 metadata; do not remove legitimate project specialization merely because its old label is no longer a central profile.
- Update active engineering authority as necessary for the new validation depth-vs-locus, planning-seeded ledger, criterion-specific evidence and consumer-surface obligations. Preserve existing `eng/` launcher semantics; migration should not require building a new generic command dispatcher.
- Clarify `engine-libraries` and `product-cli` semantic scope definitions in normal project authority, without coupling them to hardcoded source file globs.
- Reconcile existing `AGENTS.md` or other active routing instructions if they incorrectly imply that implementation initializes or owns the obligation registry; preserve ordinary implementer exclusion from guide/profile/sync metadata.
- Retain project-thesis and future UI direction as coherent architectural authority, not informal notes or an implementation claim.
- Inventory active planned/in-progress migration-sensitive milestones and avoid collision with PR #23 / current preview-review source. Completed milestone records and reviews are immutable history.
- Record any genuinely deferred broad indexing/research/historical cleanup in `.guide-sync/pending/` instead of expanding M051 into a generic documentation sweep.

## Change classification

| Class | Decision |
|---|---|
| Required | Metadata update; canonical scope definitions; version 0.7.3–0.9.1 applicable planning/execution/evidence guidance; truthful active engineering authority; guide/implementation boundary |
| Conditional | Packaged consumer validation when a supported artifact is affected; active milestone ledger reconciliation on owning branch; CLI rules only where supported product behavior requires them |
| Deprecated | Obsolete central project-type/profile labels, schema-v2 legacy parallel selection fields, per-milestone duplicated execution-methodology handoffs, CI-as-Tier-3 equation |
| Manual review | Human judgment about historical research recovery or subjective UI behavior **not required** to complete this migration; M051 is mechanically verifiable |
| No-op | No new product renderer, UI toolkit, command bus, repository-wide human review, new published .NET tool, copied guide docs or historic milestone retrofits |

## Acceptance criteria — each is a separate ledger obligation

- **AC-01** — `.guide-profile.json` is valid JSON with `schemaVersion: 2`, target guide version `0.9.1`, and no conflicting legacy guide/version/profile fields.
- **AC-02** — The canonical selected profiles are exactly base [repository], artifact-first-runtime [repository], dotnet-library [component `engine-libraries`], and cli-tool [surface `product-cli`]; profile order is semantically irrelevant.
- **AC-03** — The project-local engineering/architecture contract defines both semantic scope identifiers, excludes `eng/` commands from the product CLI scope, and preserves provider role, maturity, execution mode and baseline model.
- **AC-04** — Current active engineering guidance defines validation Tiers 0–5 by depth, independent of local/CI/remote locus, including active Windows and inactive Linux proof policy.
- **AC-05** — New/active relevant milestone planning guidance requires both resolved-decision completeness/preservation and baseline-executor tractability; material project-level decisions do not depend on planning conversations or non-authoritative research.
- **AC-06** — New AI-executed ready milestones require a *planning-seeded*, lossless per-obligation ledger, with selective evidence cases and implementation-owned work packages/status; no duplicate bespoke execution-methodology document is required.
- **AC-07** — Completion guidance requires criterion/evidence-case set reconciliation, exercised evidence and a durable completion trace; aggregate green validation or unverified ledger claims cannot satisfy unrelated criteria.
- **AC-08** — Consumer-surface validation guidance requires representative real-artifact invocation when packaging/export surfaces are changed and does not falsely require a packaged dotnet tool or Windows export today.
- **AC-09** — Product CLI guidance satisfies the applicable cli-tool process/interface decision surface (automation, output/errors, paths/configuration, cancellation, help/version, diagnostics and compatibility), using existing command contracts rather than a new CLI.
- **AC-10** — Research and legacy-guide classification is explicit: research is non-authoritative; copied old guides are not promoted; historical research reconstruction is **not** required for M051 and no fictitious recovered evidence is claimed.
- **AC-11** — The pre-existing milestone-scoped human-review workflow and existing review records are preserved; this migration creates no new subjective/historical reapproval gate.
- **AC-12** — Active M048/M050 concurrency is handled safely: no overwriting open PR #23 or silently modifying its execution ledgers; any conditional 0.9 active-ledger follow-up is recorded with owning-branch responsibility.
- **AC-13** — `docs/architecture/agentic-ui-direction.md` remains authoritative for typed-C#-DSL UI direction; neither manual ASCII authoring nor UI implementation is introduced during M051.
- **AC-14** — No engine/product runtime source or supported CLI/domain behavior is changed by the migration; no guide copies, new TBPs, issue templates, or speculative scaffolding is introduced.
- **AC-15** — Documentation/sync change scope is bounded: directly necessary project authority is current, while unrelated stale indexes and future UI-contract changes are deferred in a precise `.guide-sync/pending/` hint.
- **AC-16** — Required current validation and completion reconciliation are executed with concrete receipts/evidence for the applicable obligations before marking M051 done; historical evidence survives ledger cleanup.

## Verification gates and execution locus

These are **required gate IDs**. Planning's obligation-to-gate mapping is seeded in `.execution/M051.md`. A gate proves only the behavior it actually checks.

| ID | Depth; target and locus | Invocation / required evidence |
|---|---|---|
| `VAL-01` | Tier 1; repository metadata and docs; active Windows-local | Parse `.guide-profile.json` using PowerShell `Get-Content -Raw .guide-profile.json \| ConvertFrom-Json`; inspect `appliedProfiles` and scope IDs, prove old fields are absent; record exact resulting shape and relevant project-authority lines. |
| `VAL-02` | Tier 1; repository authority/migration evidence; active Windows-local | `git diff --check`; focused inspection of changed `AGENTS.md`, `docs/ENGINEERING.md`, CLI contracts, scope definitions, research classification and M051 direction; record criterion-specific documented evidence, not only text search. |
| `VAL-03` | Tier 2; current .NET solution; active Windows-local | `pwsh ./eng/build.ps1`, `pwsh ./eng/test.ps1`, and `pwsh ./eng/format.ps1 --verify` where applicable; record command, environment and outputs. No assertion of Linux execution. |
| `VAL-04` | Tier 2; current repository engineering aggregate; active Windows-local | `pwsh ./eng/check.ps1` when tractable; if the canonical check uses a resumable suite, use `pwsh ./eng/suite.ps1 <suite-id> --plan-json`, separately run each required `--shard <id>`, and finish with `--verify`. A partial log does not establish success. |
| `VAL-05` | Tier 1; M051 completion topology; active Windows-local | Independently compare AC-01…AC-16 and every applicable EC ID with ledger rows; link concrete evidence/gates per criterion; persist compact criterion-to-proof trace in a permanent completion record or this milestone before removing mutable operational state. |

Because M051 changes only project guidance and metadata, Tier 3 graphical integration, Tier 4 distribution/consumer installation, and Tier 5 human visual review are **not** blocking M051 gates. Those depths are mandated for future milestones only when their target surface changes. If an existing command cannot execute due to environment, report it accurately and apply current repository validation policy; never invent a passing receipt.

## Constrained execution and completion

- Use `pwsh` for active Windows launcher invocation. Keep Bash policy intact for the inactive Linux host. Do not introduce Linux evidence by assumption.
- For long resumable suites, record required shard IDs and execute them separately. `--verify` alone establishes aggregate success. Never run unbounded detached/background test processes to evade the harness.
- The execution ledger must start with the planning-seeded obligations and gate assignments. Implementation derives bounded work packages, maps them to stable IDs, fills evidence, and performs a fresh completion audit.
- End only as the repository's normal terminal result: `COMPLETE`, `AWAITING HUMAN REVIEW` (not expected for M051), or `BLOCKED`, and record actual blockers where applicable.

## Recovery and rollback

The migration is documentation/metadata only. If an intermediate change produces conflicting project authority, restore the previous file revision and reapply the 0.7.2→0.9.1 decisions coherently rather than just restoring a version number. Preserve prior `0.7.2` metadata in Git history. Never reset or rewrite completed review/milestone history for migration convenience. Current product workflows, engine commands and rendering are not changed by this milestone.

## Deferred synchronization

The overlay seeds `.guide-sync/pending/2026-10-11-m051-guide-and-gui-followups.md`. Narrow/complete individual items only after inspecting the live branch and relevant completed changes. This hint is not authority for M051 or any later implementation milestone.
## M051 completion record

**Final status: COMPLETE** (2026-10-11). This record is the durable criterion-to-proof trace; `.execution/M051.md` remains the mutable operational ledger.

Validation ran on Windows with PowerShell 7 and .NET SDK 10.0.401, the active platform in `eng/platform-verification.json`. No Linux verification is claimed. `pwsh ./eng/restore.ps1`, `pwsh ./eng/build.ps1` (0 warnings, 0 errors), `pwsh ./eng/test.ps1` (191/191), `pwsh ./eng/format.ps1 --verify`, and the complete `pwsh ./eng/check.ps1` all passed. `git diff --check` passed. The canonical check first exposed absent generated Signal Passage WAV fixture inputs; after running the existing documented workspace/project/sound validation and synthesis steps, the aggregate check passed. Those generated validation outputs are not committed. M051 requires no Tier 3 integration, Tier 4 consumer-install, Tier 5 human review, or Linux catch-up gate.

| Criterion | Durable evidence | Gate/result |
|---|---|---|
| AC-01 | `.guide-profile.json` uses `schemaVersion: 2` and `guideSystem.version: 0.9.1`; legacy guide/version/profile selector fields are absent. | VAL-01 passed: PowerShell JSON parse and legacy-key check. |
| AC-02 | `.guide-profile.json` contains exactly `base` [repository], `artifact-first-runtime` [repository], `dotnet-library` [component `engine-libraries`], and `cli-tool` [surface `product-cli`]. | VAL-01 passed: exact tuple-set comparison; order ignored. |
| AC-03 | `docs/ENGINEERING.md` defines both semantic scopes and excludes `eng/`; README/project thesis retain project specialization; metadata retains provider role, implementation-ready maturity, GPT-5.6 Luna, ai-executed-broad, and separate review/sync/release settings. | VAL-01 and VAL-02 passed. |
| AC-04 | `docs/engineering/validation-tiers.md` defines depth independently from locus; `eng/platform-verification.json` and `docs/engineering/platform-verification.md` retain Windows active and Linux deferred policy. | VAL-02 passed. |
| AC-05 | `AGENTS.md` requires ready milestones to preserve material decisions in project authority or the milestone and be executable by the baseline model. | VAL-02 passed. |
| AC-06 | `AGENTS.md` assigns pre-ready per-obligation/evidence-case seeding to planning and bounded work-package/status/evidence updates to implementation; the M051 registry retains its seeded AC/EC rows and VAL mappings. | VAL-02 and VAL-05 passed. |
| AC-07 | `AGENTS.md` requires exact coverage reconciliation, exercised per-criterion proof, and a durable completion trace; this table records that trace. | VAL-05 passed; exact AC-01…AC-16 and applicable EC sets reconciled. |
| AC-08 | `docs/engineering/validation-tiers.md` requires representative real-consumer invocation for changed supported distributions and names Linux export; it explicitly excludes an unpublished dotnet tool and Windows export. | VAL-02 passed. |
| AC-09 | `docs/specs/product-cli-contract.md` and `docs/engineering/product-cli.md` define supported process invocation, automation, stdout/stderr, outputs, exit codes, paths, configuration inputs, interruption limits, help/version, diagnostics, and contract-based compatibility. | VAL-02 passed. |
| AC-10 | `AGENTS.md` classifies research and copied guides as non-authoritative legacy traceability and explicitly does not require historical reconstruction. No research file was promoted. | VAL-02 passed. |
| AC-11 | Existing `docs/HUMAN-REVIEW.md`, `docs/engineering/human-review-workflow.md`, and review records remain unchanged; M051 declares no subjective review gate. | VAL-02 passed; review tree unchanged. |
| AC-12 | `.guide-sync/pending/2026-10-11-m051-guide-and-gui-followups.md` assigned any M048/M050 follow-up to PR #23's owning branch while it was open; no M048/M050 ledger or source was edited by M051. PR #23 later merged as `429285480a2e4d6cd61400301720859e04186f99`; its ledgers remain in progress. | VAL-02 passed; active branch collision avoided. The merged ledgers and their outstanding reconciliation/review status are preserved. |
| AC-13 | `docs/architecture/agentic-ui-direction.md` preserves the typed compositional C# DSL and renderer/operation boundaries; no UI implementation was added. | VAL-02 passed. |
| AC-14 | The change set is limited to metadata, project guidance, the M051 overlay, operational ledger, and deferred sync hint; product/runtime source and supported CLI implementation are unchanged. | VAL-02, VAL-03, and VAL-04 passed. |
| AC-15 | The seeded sync hint now distinguishes M051-complete authority from deferred indexes, future M052/M053 indexing, owning-branch M048/M050 reconciliation, and non-authoritative historical research. | VAL-02 passed. |
| AC-16 | VAL-01…VAL-05 were executed and recorded in `.execution/M051.md`; this record preserves their outcomes and criterion mapping. | VAL-01 through VAL-05 passed; VAL-04 is the complete canonical aggregate. |

Selective evidence cases:

| Evidence case | Proof |
|---|---|
| EC-03a | `docs/ENGINEERING.md` defines `engine-libraries` as supported reusable .NET API components without implying NuGet publication; VAL-01/02 passed. |
| EC-03b | `docs/ENGINEERING.md` defines `product-cli` as the `agentic2d` product surface and expressly excludes `eng/`; VAL-01/02 passed. |
| EC-08a | `docs/engineering/validation-tiers.md` retains representative real-artifact Linux-export consumer validation; VAL-02 passed. |
| EC-08b | `docs/engineering/validation-tiers.md` says no packaged dotnet tool or Windows export is currently required/present; VAL-02 passed. |
| EC-09a | Product CLI contracts cover invocation, help/version, unattended use, stdout/stderr, structured results, and exit behavior; VAL-02 passed. |
| EC-09b | Product CLI contracts cover command-specific configuration/environment inputs, path resolution, cancellation limits, diagnostics, and compatibility boundaries; VAL-02 passed. |

No completed milestone, review record, product runtime behavior, or GUI implementation was changed. PR #23 later merged M048/M050 into `main`; those ledgers still record reconciliation/review work in progress. Their owners should assess any needed guide-era follow-up before completion, without rewriting their seeded rows or review records.
