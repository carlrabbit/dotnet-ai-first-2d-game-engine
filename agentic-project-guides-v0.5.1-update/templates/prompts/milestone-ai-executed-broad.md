# Prompt — Plan a Broad AI-Executed Milestone

Our planning task is to create an implementation-ready broad AI-executed milestone package for:

`{milestone}`

The active guide authority is external:

- Guide repository: `carlrabbit/agentic-project-guides`

Do not copy guide documents into the target repository.
Do not make target repository documentation reference guide documents as operational authority.
Use the guide system only for planning, migration, documentation synchronization, review, and release readiness.

Target repository documentation must contain project truth only.

If the target repository contains old copied setup or engineering guides, treat them as legacy/non-authoritative unless the repository explicitly marks them as active project documentation.

If the target repository contains `.guide-profile.json`, use it as guide-selection metadata. Ordinary implementation agents must not be required to read `.guide-profile.json`.

If the target repository contains `.guide-sync/`, treat it as deferred documentation synchronization metadata. Ordinary implementation agents must not be required to read `.guide-sync/`.

If the target repository contains `.review/`, treat it as repository-local human-review state. Read it only when the milestone requires human review, engineering migration, documentation synchronization, or release readiness.

## Planning/implementation separation

There is no direct synchronization between the planning AI and the implementation AI.

Create an implementation-ready package. The later implementation agent must be able to:

1. unpack the ZIP into the target repository;
2. open the primary milestone document;
3. read only the authority documents explicitly listed in that milestone;
4. implement the focus areas;
5. run the specified validation tiers;
6. handle constrained execution using resumable validation when required;
7. complete required human review or report its blocking status;
8. finish without reconstructing planning context or reading the external guide repository.

Also generate a concise execution prompt in chat for the later implementation agent.

## Repository inspection

Inspect the target repository before writing files.

Read the minimum target-repository documentation needed to design the milestone package. Usually this includes:

- `README.md`;
- `AGENTS.md`;
- `.guide-profile.json` if present;
- `docs/TERMINOLOGY.md`;
- `docs/SPECS.md`;
- relevant `docs/specs/*`;
- `docs/ENGINEERING.md`;
- relevant `docs/engineering/*`;
- relevant existing milestones and decisions.

Read architecture, public docs, workflows, `.review/`, `.guide-sync/`, research, or old copied guides only when the topic requires them.

Treat `docs/research/` and copied guides as non-authoritative legacy material unless explicitly marked otherwise.

## Deliverable boundary

Create a planning/design package, not an implementation patch.

The ZIP may contain repository-relative Markdown or metadata files such as:

```text
docs/milestones/M00XX-xxx.md
docs/specs/xxx.md
docs/architecture/xxx.md
docs/decisions/ADR-00XX-xxx.md
docs/engineering/xxx.md
docs/scenarios/xxx.md
docs/artifacts/xxx.md
docs/review/xxx.md
.review/pending/xxx.md
.guide-sync/pending/xxx.md
```

Only include files directly needed to make the milestone implementation-ready.

Do not include implementation source files, test files, generated code, workflow YAML, broad documentation synchronization edits, TBPs, issue templates, or copied guide documents.

## Required milestone document

Create exactly one primary milestone document under `docs/milestones/`.

The milestone must include:

1. goal;
2. repository role and maturity assumptions;
3. execution mode;
4. scope;
5. non-goals;
6. focus areas;
7. implementation constraints;
8. required authority documents;
9. files or areas likely affected;
10. validation tiers and concrete commands;
11. validation execution mode: direct, resumable-sharded, CI-only, or human-review;
12. acceptance criteria;
13. direct documentation impact;
14. deferred documentation synchronization hints;
15. human-review requirements;
16. constrained-runtime handling;
17. out-of-scope guide migration work if applicable.

## Additional authority documents

Create specs, architecture docs, decision records, engineering docs, review docs, scenario docs, artifact docs, or public docs only when required for implementation readiness.

Do not duplicate complete spec bodies inside the milestone once a spec file exists. Link to the spec instead.

## Documentation impact

If deferred documentation work must survive handoff, create one or more files under:

```text
.guide-sync/pending/
```

Do not require the implementation agent to read `.guide-sync/`.

## Human review

Determine whether automated validation can decide acceptance.

When human review applies, specify:

- applicability: none, recommended, required, or blocking;
- review class;
- review subject;
- required evidence;
- review request/record locations under `.review/`;
- re-review triggers;
- milestone completion behavior.

## Constrained execution and validation

Determine whether any validation command may exceed a constrained agent runtime.

For resumable suites, specify:

- suite command;
- `--plan-json` command;
- shard execution contract;
- receipt location;
- fingerprint scope;
- `--verify` command;
- expected aggregate evidence.

Do not accept partial child output as aggregate success.

## Provider versus consumer distinction

Capability-provider repositories validate capability implementation.

Capability-consumer repositories use capabilities for product validation.

Mixed/dogfood repositories may do both, but dogfood scope must be bounded and explicit.

## Mode requirements

Execution mode: `ai-executed-broad`.

Use this mode only when:

- design authority is normalized;
- specs are clear;
- public API choices are approved;
- work is systematic, mechanical, or strongly bounded;
- validation is strong;
- human-review boundaries are explicit.

Scope may be large and cross-cutting.

Focus areas should describe complete implementation families or vertical transformations, not arbitrary file batches.

Prefer resumable validation when aggregate work may exceed constrained runtime limits.

## Deliverable

Create a downloadable ZIP archive containing only new or replacement files that should be added to the target repository. Preserve repository-relative paths inside the ZIP.

Do not include implementation source files, generated code, broad unrelated documentation cleanup, TBPs, issue templates, or copied guide documents.

After creating the ZIP, respond with:

1. download link;
2. included file list;
3. reason each file is included;
4. confirmed or inferred profile, role, maturity, and execution mode;
5. a filled execution prompt for the later implementation agent;
6. documentation-sync hints and the `.guide-sync/pending/` files created;
7. required human-review items and evidence expectations;
8. constrained-execution or resumable-validation instructions, if applicable.

## Quality bar

The package is acceptable only if:

- the implementation goal is unambiguous;
- focus areas are sized appropriately for the selected execution mode;
- AI-executed milestones are not artificially split into tiny human-sized tasks;
- required authority documents are explicitly listed;
- the later implementation agent does not need to read the external guide repository;
- validation expectations are tiered and concrete;
- direct documentation updates are separated from deferred synchronization;
- provider and consumer responsibilities are not blurred;
- human-review requirements are explicit when automation cannot decide acceptance;
- long validation is made resumable when the execution environment may terminate long-running commands;
- aggregate success is never inferred from partial output;
- old copied guides are not treated as active authority;
- no TBP or issue-template dependency is introduced;
- the later implementation agent does not need to reconstruct planning context.
