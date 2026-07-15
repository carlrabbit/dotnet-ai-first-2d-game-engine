# Milestone 018 — Game Workspace Manifest, Deterministic Scaffolding, and Unified Agent Execution Workflow

## Goal

Introduce a consumer-oriented workspace model that lets an agent create, validate, run, inspect, and review a game project without reconstructing engine internals or manually composing many subsystem commands.

Required journey:

```text
engine acquisition provider
→ transactional workspace creation
→ generated workspace manifest
→ generated game project manifest
→ deterministic minimal-game scaffold
→ workspace validation
→ project validation
→ unified scenario run
→ run manifest and linked evidence
→ inspect and review commands
```

The workspace model must separate:

```text
game project truth
from
workspace composition and engine acquisition
```

M018 implements source-based acquisition through directory and Git providers. It does not implement NuGet, workspace updating, or portable SDK distribution.

## Repository role and maturity assumptions

Repository role:

```text
capability-provider
```

The repository provides reusable engine/runtime/tooling capability. M018 additionally creates a bounded consumer-style workspace fixture to prove how an external game project uses the engine.

Maturity assumptions:

- implementation-ready and artifact-first;
- headless-first and CLI/API-first;
- Milestones 000 through 017 and accepted completion patches are implemented or available when M018 is integrated;
- the product CLI is the agent-facing engine API;
- authored content, scenarios, input, rendering, animation, artifacts, and review packs already exist;
- current repository-local `eng/` scripts are engineering wrappers, not the consumer product interface;
- existing stable-ID, deterministic artifact, diagnostics, validation, and authority rules remain authoritative.

## Execution mode

```text
ai-executed-broad
```

Implement the workspace/project contracts and deterministic scaffolding first, then unified execution, run evidence, review aggregation, and black-box integration validation.

## Locked design decisions

- workspace and game project are distinct concepts;
- `agentic2d.project.json` contains game/product truth;
- `agentic2d.workspace.json` contains checkout composition, engine acquisition, mutation policies, artifact root, and project location;
- NuGet acquisition and package references are not supported or planned by M018;
- implemented engine acquisition providers are `directory` and `git`;
- directory acquisition supports `reference` and `copy` placement;
- Git acquisition always materializes engine source under the workspace;
- `portable-sdk` is reserved in the manifest vocabulary but unsupported and has no implementation;
- provider extensibility means an internal interface and built-in registration, not dynamic third-party plugins;
- `workspace create` is supported;
- `workspace validate` is supported;
- `workspace update`, `workspace upgrade`, `workspace migrate`, and automatic engine replacement are explicitly unsupported;
- creation never merges into a non-empty target directory;
- no `--force` overwrite option exists;
- creation is transactional through a staging directory;
- target absence or emptiness is required;
- failed creation leaves no apparently valid partial workspace;
- Git acquisition requires Git to be available and an explicit requested revision;
- generated Git workspace records the resolved commit SHA;
- ordinary acceptance tests use directory acquisition and a local Git fixture, never public-network access;
- directory copy excludes `.git`, build outputs, artifacts, editor state, and temporary files;
- when source is a Git repository and Git is available, directory copy should prefer tracked files;
- generated workspace areas have explicit roles and mutation policies;
- `engine-src` is read-only unless the task explicitly authorizes engine changes;
- `game-src` and `game-content` are writable;
- `artifacts` are generated and replaceable;
- generated workspace commands resolve engine location from the workspace manifest;
- generated wrappers do not embed machine-specific absolute engine paths;
- one initial scaffold template is implemented: `minimal-game`;
- generated content is valid and runnable, not success-only placeholder content;
- the unified run directory contains one central `run-manifest.json` linking subsystem evidence;
- project/run commands are consumer workflows, while repository `eng/` scripts remain provider engineering validation;
- no graphical project wizard is introduced.

## Scope

1. Workspace manifest schema and validation.
2. Game project manifest schema and validation.
3. Workspace area roles and mutation policies.
4. Engine acquisition provider abstraction.
5. Directory reference provider.
6. Directory copy provider.
7. Git clone/checkout provider.
8. Future portable-SDK provider contract without implementation.
9. Transactional `workspace create` command.
10. `workspace validate` command.
11. Non-empty target rejection.
12. Deterministic copy exclusion policy.
13. Exact Git revision resolution and provenance.
14. `minimal-game` scaffold template.
15. Generated `AGENTS.md`, `README.md`, `.gitignore`, project/solution files, wrappers, manifests, game source, content, and tests.
16. Unified project validation.
17. Unified project run command.
18. Unified run directory layout.
19. Central run manifest.
20. Run inspection command.
21. Run review command.
22. Agent-facing recommended next diagnostic actions.
23. Workspace creation artifacts and diagnostics.
24. Black-box directory-reference integration smoke.
25. Black-box directory-copy integration smoke.
26. Local Git repository integration smoke.
27. Provider-versus-consumer validation separation.

## Non-goals

Do not implement:

- NuGet support;
- NuGet package publishing;
- package-reference engine consumption;
- workspace update;
- workspace upgrade;
- workspace migration;
- automatic engine revision replacement;
- force overwrite;
- merge into existing non-empty directories;
- Internet-dependent acceptance tests;
- dynamic plugin discovery;
- third-party acquisition provider loading;
- portable SDK construction or extraction;
- installer creation;
- global tool installation;
- graphical project wizard;
- broad template ecosystem;
- template marketplace;
- dependency registry;
- game packaging;
- deployment;
- save/load;
- broad unrelated documentation cleanup;
- workflows, TBPs, issue templates, public docs, release docs, or guide migration.

## Focus areas

### 1. Workspace versus project authority

`agentic2d.project.json` describes the game:

- stable project ID;
- game source roots;
- authored content roots;
- default scenario;
- runtime configuration;
- presentation configuration;
- project-defined assemblies/extensions;
- project fingerprint inputs.

`agentic2d.workspace.json` describes the checkout:

- stable workspace ID;
- project manifest path;
- engine acquisition record;
- area roles and mutation policies;
- artifact root;
- generated wrapper locations;
- workspace schema/version.

A project manifest must remain usable in multiple workspaces.

### 2. Recommended workspace shape

Generated `minimal-game` layout:

```text
GameFolder/
├─ engine-src/                         # copy/git placement only
├─ game-src/
│  ├─ MinimalGame/
│  ├─ MinimalGame.Behaviors/
│  └─ MinimalGame.Tests/
├─ game-content/
│  ├─ assets/
│  ├─ maps/
│  ├─ entities/
│  ├─ visuals/
│  ├─ animations/
│  ├─ input/
│  └─ scenarios/
├─ eng/
│  ├─ validate.sh
│  ├─ run.sh
│  ├─ inspect.sh
│  └─ review.sh
├─ artifacts/
├─ agentic2d.project.json
├─ agentic2d.workspace.json
├─ Directory.Build.props
├─ Directory.Packages.props
├─ MinimalGame.slnx
├─ AGENTS.md
├─ README.md
├─ .gitignore
└─ artifacts/.gitkeep
```

Directory-reference mode omits `engine-src/` and records a relative or resolvable external path.

### 3. Workspace area model

Required area roles:

```text
engine-provider
game-code
authored-content
generated-artifacts
tooling
```

Required mutation policies:

```text
read-only-unless-authorized
writable
replaceable-generated
```

Initial policy:

| Area | Role | Mutation policy |
|---|---|---|
| `engine-src` or referenced engine | engine-provider | read-only-unless-authorized |
| `game-src` | game-code | writable |
| `game-content` | authored-content | writable |
| `artifacts` | generated-artifacts | replaceable-generated |
| `eng` | tooling | writable only for workspace/tooling tasks |

Generated `AGENTS.md` must explain these rules directly.

### 4. Engine acquisition provider abstraction

Introduce an internal provider boundary such as:

```text
IEngineAcquisitionProvider
```

Required built-in provider IDs:

```text
directory-reference
directory-copy
git-clone
```

Reserved future provider ID:

```text
portable-sdk
```

The reserved provider is not executable in M018. Attempting to use it returns a stable unsupported-provider diagnostic.

No runtime assembly discovery or third-party plugin loading.

### 5. Directory reference provider

Command shape:

```bash
agentic2d workspace create <target> \
  --template minimal-game \
  --engine-directory <path> \
  --engine-placement reference
```

Rules:

- source path must exist;
- source must validate as a compatible engine checkout;
- generated workspace references the engine without copying it;
- generated wrappers resolve the reference through the workspace manifest;
- semantic workspace fingerprint excludes machine-specific absolute path text;
- diagnostics may include resolved absolute paths;
- generated manifest should prefer a relative path when representable.

### 6. Directory copy provider

Command shape:

```bash
agentic2d workspace create <target> \
  --template minimal-game \
  --engine-directory <path> \
  --engine-placement copy
```

Rules:

- copy into `engine-src/`;
- never copy `.git` metadata;
- exclude `bin/`, `obj/`, `artifacts/`, editor state, temporary files, and other declared generated roots;
- if Git is available and source is a Git checkout, prefer copying tracked files plus required non-ignored project files defined by policy;
- preserve executable bits where supported;
- canonicalize file ordering;
- record source and resolved fingerprints;
- reject destination collisions.

### 7. Git provider

Command shape:

```bash
agentic2d workspace create <target> \
  --template minimal-game \
  --engine-git <repository-or-local-url> \
  --engine-revision <revision>
```

Rules:

- Git executable must be discoverable;
- revision is mandatory;
- repository may be a local path/`file://` URL or remote URL;
- clone/fetch only what is necessary where practical;
- checkout exact requested revision;
- resolve and record full commit SHA;
- workspace uses `engine-src/`;
- generated semantic identity uses the resolved commit, not branch name alone;
- failure to find Git returns a stable diagnostic;
- no silent fallback to directory acquisition;
- acceptance tests use a temporary local Git repository.

### 8. Acquisition result

Every provider returns structured data:

- provider ID;
- placement;
- source descriptor;
- resolved engine path;
- requested revision if applicable;
- resolved commit/fingerprint;
- copied/acquired file count;
- exclusions/copy policy ID;
- diagnostics;
- provenance.

Provider output feeds workspace manifest generation and creation artifacts.

### 9. Transactional workspace creation

Required behavior:

```text
validate arguments
→ verify target absent or empty
→ create staging directory beside target where possible
→ acquire engine
→ render scaffold
→ validate generated workspace
→ move staging directory to target
→ emit success artifacts
```

If creation fails:

- target must not appear valid;
- staging data is removed when safe;
- cleanup failures are diagnosed;
- no merge into existing user files occurs.

If the target exists and contains any entry, fail.

Do not add `--force`.

### 10. Scaffold template

Initial template ID:

```text
minimal-game
```

The template must contain a real, valid vertical slice:

- one small map;
- one player entity definition;
- one visual definition;
- one animation definition if M017 is available;
- one input map;
- one deterministic scenario;
- one game behavior project or bounded extension point;
- one game test project;
- generated wrappers;
- valid manifests.

The generated project must build, validate, and run.

Do not generate empty placeholder projects that only return success.

### 11. Generated wrapper bootstrap

Generated wrappers:

```text
./eng/validate.sh
./eng/run.sh <scenario-id>
./eng/inspect.sh <run-directory>
./eng/review.sh <run-directory>
```

`agentic2d.workspace.json` remains authoritative. Generation also writes `eng/engine-bootstrap.env`, a non-executable, shell-safe projection containing the provider, engine path relative to `eng/`, tools project, placement, resolved identity, and fingerprint. `eng/agentic2d.sh` is the only engine launcher; it sources that projection and executes `dotnet run --project <resolved-engine>/src/Agentic2D.Tools/Agentic2D.Tools.csproj -- <args>`. The four user wrappers delegate to it and never parse JSON.

Workspace validation compares the bootstrap projection with the manifest and fails stable diagnostics for missing, malformed, executable, or drifting bootstrap data. Directory-reference wrappers use a relative engine path whenever possible; absolute paths are diagnostic-only and do not participate in semantic fingerprints. Portable SDK construction remains unsupported.

### 12. Workspace validation

Command:

```bash
agentic2d workspace validate <workspace-path> --output <directory>
```

Validate:

- workspace manifest schema;
- project manifest path;
- engine provider record;
- engine path/revision/fingerprint compatibility;
- area roots and non-overlap;
- mutation policies;
- generated wrapper presence/executability;
- artifact root;
- project manifest validity;
- referenced game source/content roots;
- absence of unsupported provider modes;
- expected engine project/CLI entry points.

### 13. Project validation

Command:

```bash
agentic2d project validate <project-or-workspace-path> --output <directory>
```

Project validation resolves all declared content roots and validates current supported domains through one workflow.

It must produce a project reference graph and deterministic project fingerprint.

The command must not require the user to invoke each domain validator manually.

### 14. Unified project run

Command:

```bash
agentic2d project run <project-or-workspace-path> \
  --scenario <scenario-id> \
  --output <run-directory>
```

The command:

1. validates workspace/project;
2. resolves engine and content;
3. executes the scenario;
4. produces normal subsystem evidence;
5. creates the unified run manifest;
6. emits a concise status and recommended next actions.

It must not hide subsystem diagnostics.

### 15. Unified run directory

Required layout:

```text
<run-directory>/
├─ run-manifest.json
├─ content/
├─ input/
├─ runtime/
├─ animation/
├─ render/
├─ review/
└─ diagnostics/
```

Only create subsystem directories that have evidence, but the manifest must state which families are present or absent.

Run IDs are explicit or deterministic. Wall-clock timestamps must not become semantic identity.

### 16. Run manifest

`run-manifest.json` is the central discovery artifact.

It links:

- workspace ID/fingerprint;
- project ID/fingerprint;
- engine provider and resolved revision/fingerprint;
- scenario ID/fingerprint;
- run ID;
- runtime seed/tick configuration;
- content validation evidence;
- input evidence;
- runtime/entity/spatial/interaction evidence;
- animation evidence;
- render evidence;
- screenshots where explicitly present;
- diagnostics;
- review-pack evidence;
- status;
- recommended next diagnostic actions.

It does not duplicate complete subsystem artifacts.

### 17. Run inspection

Command:

```bash
agentic2d run inspect <run-directory> --output <directory>
```

It validates the run manifest and referenced artifacts, then emits:

- concise run summary;
- failed/missing evidence;
- primary diagnostics;
- object IDs and paths involved;
- recommended next commands;
- integrity/fingerprint status.

### 18. Run review

Command:

```bash
agentic2d run review <run-directory> --output <directory>
```

It builds or refreshes the review pack from the run manifest and current artifact families.

Review output must distinguish:

- semantic structural evidence;
- optional visual screenshot evidence;
- automated pass/fail;
- explicit human-review questions.

### 19. Recommended next actions

Failures should emit stable recommendations such as:

```text
agentic2d workspace validate <workspace>
agentic2d project validate <project>
agentic2d run inspect <run-directory>
agentic2d animation inspect <animation-id>
agentic2d input inspect <sequence-id> --input-map <map-id>
```

Recommendations are structured evidence, not free-form guesses only.

### 20. Provider versus consumer validation

Provider repository validation proves:

- scaffolding implementation;
- provider behavior;
- generated workspace correctness;
- black-box consumer operation;
- no unintended engine mutation.

Generated consumer workspace validation proves:

- game source/content builds and validates;
- scenario runs through workspace/project commands;
- artifacts and review are discoverable.

Do not require consumer agents to understand provider repository `eng/` internals.

### 21. Black-box integration smokes

Required smokes:

```text
workspace-directory-reference-smoke
workspace-directory-copy-smoke
workspace-local-git-smoke
workspace-minimal-game-run-smoke
```

Directory-reference smoke:

- create under temporary directory using current engine checkout;
- validate generated workspace;
- run generated scenario;
- inspect run;
- verify engine source remains unchanged.

Directory-copy smoke:

- copy current engine through provider policy;
- verify exclusions;
- validate and run generated workspace.

Local Git smoke:

- create temporary local Git repository fixture;
- commit engine fixture;
- create workspace from local repository at exact revision;
- verify resolved SHA;
- validate/run without Internet.

## Implementation constraints

- No NuGet vocabulary or implementation.
- Workspace/project contracts contain no external guide authority.
- Acquisition providers are internal built-ins, not dynamic plugins.
- Creation is transactional and conservative.
- No non-empty target merge.
- No force overwrite.
- No update/migration command.
- Git revision is explicit and resolved to a full commit.
- Acceptance is network-independent.
- Generated workspaces contain real runnable content.
- Generated wrappers resolve engine acquisition through the manifest.
- Machine-specific absolute paths do not affect semantic fingerprints.
- Engine source is read-only by default in consumer tasks.
- Workspace/project/run artifacts are deterministic and structured.
- Existing subsystem commands remain available; unified commands orchestrate rather than replace their authority.
- Ordinary implementation agents must not read `.guide-profile.json`, `.guide-sync/`, external guides, copied guides, or prompt templates.

## Required authority documents

Read only:

```text
README.md
AGENTS.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/SCENARIOS.md
docs/CONTENT.md
docs/ARTIFACTS.md
docs/HUMAN-REVIEW.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/validation-tiers.md
docs/engineering/future-dotnet-solution.md
docs/specs/product-cli-contract.md
docs/specs/content-validation-contract.md
docs/specs/scenario-runner-contract.md
docs/specs/review-pack-contract.md
docs/specs/game-project-manifest-contract.md
docs/specs/game-workspace-manifest-contract.md
docs/specs/workspace-scaffolding-contract.md
docs/specs/unified-agent-execution-workflow-contract.md
docs/artifacts/workspace-creation-artifact-contract.md
docs/artifacts/unified-run-artifact-contract.md
docs/decisions/ADR-0008-product-cli-is-the-agent-facing-product-api.md
docs/decisions/ADR-0009-adopt-external-guide-system-v0.2.0.md
docs/decisions/ADR-0018-rendering-is-read-only-and-raylib-is-an-isolated-adapter.md
docs/decisions/ADR-0019-input-is-tick-bound-semantic-data-and-replay-uses-resolved-frames.md
docs/decisions/ADR-0020-animation-produces-typed-presentation-patches.md
docs/decisions/ADR-0021-workspaces-separate-game-truth-from-engine-acquisition.md
docs/milestones/MILESTONE-016-deterministic-multi-device-input-frames-action-mapping-and-semantic-replay.md
docs/milestones/MILESTONE-017-deterministic-keyframe-animation-base-overlay-markers-and-animated-render-projection.md
docs/milestones/MILESTONE-018-game-workspace-manifest-deterministic-scaffolding-and-unified-agent-execution-workflow.md
```

Do not read external guide documents for implementation.

## Files or areas likely affected

Likely source areas:

```text
src/Agentic2D.Contracts
src/Agentic2D.Tools
src/Agentic2D.Validation
src/Agentic2D.ScenarioRunner
src/Agentic2D.Engine
src/Agentic2D.Rendering
src/Agentic2D.Animation
tests/unit/Agentic2D.Tests.Unit
```

A focused project is justified:

```text
src/Agentic2D.Workspaces
```

Likely templates/fixtures:

```text
templates/workspaces/minimal-game/
tests/fixtures/workspaces/
tests/fixtures/git-engine-source/
```

Required engineering wrappers:

```text
eng/workspace-directory-reference-smoke.sh
eng/workspace-directory-copy-smoke.sh
eng/workspace-local-git-smoke.sh
eng/workspace-minimal-game-run-smoke.sh
./eng/m018-smoke.sh
./eng/m018-directory-reference-smoke.sh
./eng/m018-directory-copy-smoke.sh
./eng/m018-local-git-smoke.sh
./eng/m018-consumer-workflow-smoke.sh
./eng/m018-consumer-bootstrap-smoke.sh <temporary-root>
./eng/m018-consumer-run-smoke.sh <workspace>
./eng/m018-consumer-review-smoke.sh <workspace>
```

Do not add a consumer game as a large permanent product inside engine source. Keep fixtures bounded.

## Validation tiers and concrete repository commands

### Tier 1 — Focused unit and contract validation

Cover:

- manifest schema validation;
- provider selection;
- relative-path normalization;
- copy exclusion policy;
- Git absence and revision errors;
- target-directory safety;
- transactional cleanup;
- template rendering;
- deterministic fingerprints;
- run-manifest reference integrity;
- mutation policy validation.

### Tier 2 — Headless workspace smokes

```bash
./eng/workspace-directory-reference-smoke.sh
./eng/workspace-directory-copy-smoke.sh
./eng/workspace-local-git-smoke.sh
./eng/workspace-minimal-game-run-smoke.sh
```

### Tier 3 — Complete regression and milestone gate

```bash
./eng/check.sh
./eng/cli-smoke.sh
./eng/product-validate.sh
./eng/m015-smoke.sh
./eng/m016-smoke.sh
./eng/m017-smoke.sh
./eng/m018-smoke.sh
./eng/m018-directory-reference-smoke.sh
./eng/m018-directory-copy-smoke.sh
./eng/m018-local-git-smoke.sh
./eng/m018-consumer-workflow-smoke.sh
./eng/m018-consumer-bootstrap-smoke.sh <temporary-root>
./eng/m018-consumer-run-smoke.sh <workspace>
./eng/m018-consumer-review-smoke.sh <workspace>
```

M018 acceptance is headless and network-independent.

### Required direct checks

Expected command shapes:

```bash
dotnet run --project src/Agentic2D.Tools -- \
  workspace create /tmp/agentic2d-minimal-reference \
  --template minimal-game \
  --engine-directory "$PWD" \
  --engine-placement reference \
  --output artifacts/workspaces/create-reference

dotnet run --project src/Agentic2D.Tools -- \
  workspace create /tmp/agentic2d-minimal-copy \
  --template minimal-game \
  --engine-directory "$PWD" \
  --engine-placement copy \
  --output artifacts/workspaces/create-copy

dotnet run --project src/Agentic2D.Tools -- \
  workspace validate /tmp/agentic2d-minimal-reference \
  --output artifacts/workspaces/validate-reference

dotnet run --project src/Agentic2D.Tools -- \
  project validate /tmp/agentic2d-minimal-reference \
  --output artifacts/projects/validate-minimal

dotnet run --project src/Agentic2D.Tools -- \
  project run /tmp/agentic2d-minimal-reference \
  --scenario scenario.minimal.smoke \
  --output artifacts/runs/minimal-smoke

dotnet run --project src/Agentic2D.Tools -- \
  run inspect artifacts/runs/minimal-smoke \
  --output artifacts/runs/minimal-smoke-inspection

dotnet run --project src/Agentic2D.Tools -- \
  run review artifacts/runs/minimal-smoke \
  --output artifacts/runs/minimal-smoke-review
```

Use current parser conventions if exact syntax differs, while preserving the command semantics.

## Acceptance criteria

### Manifest boundaries

1. Workspace and project manifests are separate schemas.
2. Project manifest contains game truth only.
3. Workspace manifest contains acquisition/composition only.
4. Project manifest can be reused across multiple workspaces.
5. Area roles and mutation policies validate.
6. NuGet is absent from schemas, commands, docs, and implementation.

### Providers

7. Directory reference validates and resolves an engine checkout.
8. Directory copy produces `engine-src/` with exclusions applied.
9. Git provider requires Git and explicit revision.
10. Git provider records full resolved commit SHA.
11. Missing Git produces a stable diagnostic.
12. Provider failures do not silently fall back.
13. Portable SDK is recognized only as unsupported/reserved if represented at all.
14. No dynamic plugin loading exists.

### Creation safety

15. Missing target can be created.
16. Existing empty target can be used transactionally.
17. Existing non-empty target is rejected.
18. No force overwrite exists.
19. No update/migrate/upgrade command exists.
20. Failed creation leaves no apparently valid partial workspace.
21. Staging cleanup failures are diagnosed.
22. Equivalent inputs produce equivalent scaffold fingerprints.

### Generated workspace

23. `minimal-game` creates the required structure.
24. Generated manifests validate.
25. Generated solution/projects build.
26. Generated game content validates.
27. Generated scenario runs real behavior.
28. Generated `AGENTS.md` states mutation boundaries.
29. Generated wrappers resolve engine through workspace manifest.
30. Directory-reference mode avoids copied `engine-src/`.
31. Copy/Git mode uses `engine-src/`.
32. Generated artifacts root is ignored except committed placeholder.

### Unified workflow

33. `workspace validate` produces structured evidence.
34. `project validate` validates all declared current domains.
35. `project run` creates subsystem evidence and `run-manifest.json`.
36. Run manifest links all present artifact families.
37. Missing artifact families are explicit.
38. `run inspect` validates references and reports primary diagnostics.
39. `run review` produces review-pack evidence from the run manifest.
40. Recommended next actions are structured and point to valid commands.
41. Unified workflow does not suppress subsystem diagnostics.

### Provider/consumer proof

42. Directory-reference black-box smoke passes.
43. Directory-copy black-box smoke passes.
44. Local Git black-box smoke passes without Internet.
45. Generated minimal game runs from outside provider source layout.
46. Ordinary consumer workflow does not require provider `eng/` knowledge.
47. Consumer task does not mutate engine source.
48. Provider repository regressions remain green.

### Scope

49. No NuGet support is introduced.
50. No workspace update/migration is introduced.
51. No portable SDK implementation is introduced.
52. No graphical wizard, packaging, deployment, guide, workflow, TBP, issue-template, public-doc, or release work is introduced.
53. M018 and ADR-0021 are indexed after acceptance.

## Direct documentation impact

Update only where implementation would otherwise be false or undiscoverable:

```text
README.md
AGENTS.md
docs/TERMINOLOGY.md
docs/SPECS.md
docs/SCENARIOS.md
docs/CONTENT.md
docs/ARTIFACTS.md
docs/HUMAN-REVIEW.md
docs/ENGINEERING.md
docs/MILESTONES.md
docs/DECISIONS.md
docs/engineering/command-contract.md
docs/engineering/product-cli.md
docs/engineering/validation-tiers.md
docs/engineering/future-dotnet-solution.md
docs/specs/product-cli-contract.md
docs/specs/content-validation-contract.md
docs/specs/scenario-runner-contract.md
docs/specs/review-pack-contract.md
docs/artifacts/review-pack-artifact-contract.md
```

Do not perform unrelated cleanup.

## Deferred documentation synchronization hints

The package adds:

```text
.guide-sync/pending/2026-07-14-m018-index-and-crosslink-sync.md
.guide-sync/pending/2026-07-14-m018-human-review-and-workspace-boundary-followup.md
```

Ordinary implementation agents must not read them.

## Human review requirements

Verify:

1. workspace and project authority remain distinct;
2. engine source is readable but protected by default mutation policy;
3. generated wrappers do not embed brittle machine paths;
4. directory copy exclusions are safe and understandable;
5. Git acquisition is deterministic and network-independent in tests;
6. transactional creation cannot damage non-empty targets;
7. generated content is genuinely runnable;
8. the consumer workflow is understandable without provider internals;
9. run manifest gives a useful central entry point;
10. recommended next actions are actionable rather than generic prose;
11. no update/migration semantics have leaked into creation;
12. no NuGet assumptions remain;

## Out-of-scope guide migration work

No guide migration is included.

Do not:

- modify `.guide-profile.json`;
- copy guide documents or prompt templates;
- reference external guides as repository operational authority;
- require ordinary implementation agents to read `.guide-sync/`.
