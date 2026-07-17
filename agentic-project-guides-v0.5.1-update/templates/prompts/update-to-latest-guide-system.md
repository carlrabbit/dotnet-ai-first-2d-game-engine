# Prompt — Update Repository to Latest Agentic Project Guide System

Our planning task is to create an implementation-ready migration package for updating a repository from any existing `carlrabbit/agentic-project-guides` version to the latest available guide-system version.

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

## Resolve current and latest guide versions

Read the target repository's `.guide-profile.json` if present to identify the current recorded guide-system version.

Then inspect the guide repository to determine the latest available version.

Read at minimum:

- `README.md`;
- `CHANGELOG.md`;
- `meta/VERSIONING.md`;
- `meta/MIGRATION-MODEL.md`;
- all migration documents newer than the current version;
- relevant decisions;
- `templates/PROMPTS.md`.

State both the current and resolved latest versions in the chat response and in the migration milestone.

Do not assume a hard-coded latest version.

## Repository inspection

Inspect the target repository before writing files.

Usually read:

- `.guide-profile.json`;
- `.guide-sync/pending/` if present;
- `.review/` if present and relevant;
- `README.md`;
- `AGENTS.md`;
- `docs/TERMINOLOGY.md`;
- `docs/SPECS.md`;
- relevant `docs/specs/*`;
- `docs/ENGINEERING.md`;
- relevant `docs/engineering/*`;
- `docs/MILESTONES.md`;
- relevant milestones, decisions, architecture docs, public docs, workflows, and command scripts.

Treat copied guides under `docs/research/` as non-authoritative unless explicitly marked otherwise.

## Required output

Create exactly one migration milestone under `docs/milestones/`.

The milestone must include:

- current and target guide-system versions;
- repository role and maturity;
- applicable profiles;
- execution mode;
- required, conditional, deprecated, manual-review, and no-op changes;
- authority documents;
- likely affected files;
- validation tiers and concrete commands;
- human-review requirements;
- constrained-execution implications;
- direct documentation impact;
- deferred `.guide-sync/pending/` hints;
- acceptance criteria;
- rollback or recovery notes where appropriate.

Update `.guide-profile.json` only if the migration package includes that change.

Create `.guide-sync/pending/` hints for deferred documentation synchronization.

Do not copy guide documents into the target repository.

Do not perform broad implementation work.

## Human review

Determine whether migration items require human judgment, including:

- public API decisions;
- platform-support claims;
- review-policy choices;
- fingerprint-scope choices;
- deletion of historical documentation;
- release or compatibility changes.

When human review is required, define:

- review class;
- blocking behavior;
- evidence;
- completion condition;
- expected repository location under `.review/`.

## Constrained execution

Inspect engineering commands for long aggregate wrappers or agent-harness sensitivity.

When migration validation may exceed runtime limits, require resumable validation with:

- `--plan-json`;
- bounded `--shard <id>` commands;
- atomic fingerprinted receipts;
- fast `--verify`;
- no inference from partial output.

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
