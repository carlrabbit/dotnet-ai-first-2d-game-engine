# Milestone 001 — Base Engineering Substrate

## Goal

Create the minimal .NET engineering substrate for `dotnet-ai-first-2d-game-engine` so later implementation agents can restore, build, test, format, and validate the repository through canonical commands instead of inventing ad-hoc workflows.

This milestone is a repository-substrate milestone, not an engine-runtime milestone.

The milestone produces:

- shared repository configuration;
- canonical `eng/` shell commands;
- a minimal .NET solution;
- a minimal production/test project set;
- one smoke test proving that the test substrate works;
- direct documentation updates for the commands and project layout introduced by the milestone.

## Repository maturity and task mode

Repository maturity after this milestone:

```text
Implementation-ready for the engineering substrate.
Design-ready for the engine runtime.
```

Task mode for this milestone:

```text
Implementation
```

This is not a documentation synchronization task, release-readiness task, public documentation task, or runtime architecture task.

## Required authority

A later implementation agent must read only the following authority before implementing this milestone:

```text
README.md
AGENTS.md
docs/INITIALIZATION.md
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/validation-tiers.md
docs/engineering/future-dotnet-solution.md
docs/decisions/ADR-0006-establish-engineering-substrate-before-runtime.md
```

Do not require the implementation agent to read all documents under `docs/`.

Do not treat `docs/research/` as operational authority. Research documents may explain background, but any rule required for implementation must be present in the active authority documents listed above or in this milestone.

## Scope

Create or modify only the files needed to establish the base engineering substrate.

### Repository configuration

Create:

```text
.editorconfig
global.json
Directory.Build.props
Directory.Packages.props
```

Create `.config/dotnet-tools.json` only if a local .NET tool is actually required by the selected implementation.

### Engineering commands

Create:

```text
eng/common.sh
eng/restore.sh
eng/build.sh
eng/test.sh
eng/format.sh
eng/check.sh
```

All scripts must be executable.

Scripts must be POSIX-compatible enough for a normal Linux shell environment. Bash is acceptable if scripts declare it explicitly.

### Minimal .NET solution

Create:

```text
dotnet-ai-first-2d-game-engine.slnx
src/Agentic2D.Contracts/Agentic2D.Contracts.csproj
src/Agentic2D.Engine/Agentic2D.Engine.csproj
tests/unit/Agentic2D.Tests.Unit/Agentic2D.Tests.Unit.csproj
```

Add projects to the solution.

Project references:

```text
Agentic2D.Engine -> Agentic2D.Contracts
Agentic2D.Tests.Unit -> Agentic2D.Contracts
Agentic2D.Tests.Unit -> Agentic2D.Engine
```

### Minimal source shape

Add only enough production code to make the projects meaningful and compile.

Recommended minimal contract types:

```text
EntityId
Tick
```

Both should be small, deterministic value types. Do not design the full runtime model in this milestone.

Recommended minimal engine type:

```text
EngineAssemblyMarker
```

or another intentionally boring type that proves the assembly builds without introducing runtime semantics.

### Minimal unit test

Add one smoke unit test that proves:

- the TUnit/MTP test project is wired correctly;
- the contracts assembly can be referenced;
- the engine assembly can be referenced.

The test must be deterministic and fast.

## Non-goals

Do not implement any of the following in this milestone:

```text
agentic2d product CLI
runtime tick loop
component storage
command/event/query dispatch
scenario runner
JSON result artifacts
asset pipeline
asset metadata schemas
renderer
raylib-cs integration
MonoGame integration
SDL/Silk.NET integration
source generators
benchmarks
GitHub Actions workflows
NuGet packaging
public documentation
samples
TBPs
issue templates
behavior modules
F# projects
```

Do not create non-root `README.md` files.

Do not add runtime/game packages. In particular, do not add raylib-cs, MonoGame, Aether.Physics2D, Box2D.NET, Silk.NET, or SDL bindings in this milestone.

Do not introduce placeholder scripts that succeed without performing meaningful validation.

## Required implementation conventions

### SDK selection

Use .NET 10 or newer if available in the target environment. Pin or select the SDK through `global.json`.

If the exact SDK version cannot be known from the repository state, choose the current installed .NET 10 SDK available to the implementation environment and record the chosen version in the implementation summary.

Do not use preview SDK features unless the selected SDK itself requires preview status and the repository explicitly accepts it.

### Central package management

Use `Directory.Packages.props` for package versions.

Keep initial package references minimal. The expected test foundation is:

```text
TUnit
Microsoft.Testing.Platform
```

Add additional test infrastructure packages only if required by the current TUnit/MTP setup. Do not add assertion, mocking, snapshot, benchmark, or coverage packages unless a failing compile/test setup proves they are necessary.

### Build configuration

`Directory.Build.props` should establish baseline project behavior, including:

- nullable reference types enabled;
- implicit usings enabled where appropriate;
- deterministic builds where applicable;
- warnings treated consistently;
- analyzer settings appropriate for a new .NET repository.

Do not overfit package metadata, XML documentation generation, SourceLink, public API validation, or NuGet publishing settings. Those belong to later maturity stages.

### Formatting configuration

`.editorconfig` should be opinionated enough that agents do not infer style from examples.

It should cover at minimum:

- UTF-8 charset;
- LF line endings;
- final newline;
- C# indentation;
- basic C# style preferences;
- severity for formatting/style consistency where safe.

Do not make the first milestone fail on highly subjective style rules that are likely to generate noise before real code exists.

### Engineering command behavior

`eng/common.sh` should centralize shared script behavior such as:

- repository-root discovery;
- strict shell options;
- useful error messages;
- command existence checks where appropriate.

`eng/restore.sh` must restore the solution.

`eng/build.sh` must build the solution.

`eng/test.sh` must run fast tests only. At this milestone, that means the unit test project under `tests/unit/`.

`eng/format.sh` must support:

```text
./eng/format.sh
./eng/format.sh --verify
```

`eng/check.sh` must run the standard local gate:

```text
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/format.sh --verify
```

Every script must fail with a non-zero exit code when its validation fails.

### Solution and project naming

Use the repository/product stem consistently:

```text
Solution: dotnet-ai-first-2d-game-engine.slnx
Root namespace stem: Agentic2D
Projects:
  Agentic2D.Contracts
  Agentic2D.Engine
  Agentic2D.Tests.Unit
```

If `.slnx` is not supported by the selected SDK/tooling in the implementation environment, create a `.sln` file instead, record the reason in the implementation summary, and update direct documentation accordingly.

### No local README files

Do not create:

```text
docs/**/README.md
eng/README.md
src/**/README.md
tests/**/README.md
```

Use named Markdown files only when documentation is required.

## Focus areas

### Focus Area A — Shared repository configuration

#### Goal

Create the shared .NET and editor configuration needed by all later implementation work.

#### Scope

Create:

```text
.editorconfig
global.json
Directory.Build.props
Directory.Packages.props
```

Create `.config/dotnet-tools.json` only if a local tool is needed.

#### Likely commands

```bash
dotnet --version
dotnet new globaljson --sdk-version <selected-sdk-version>
```

The exact commands may vary, but the resulting files must be clear and deterministic.

#### Validation tier

Tier 1 — Focused implementation.

#### Required validation

After this focus area, the repository should be ready for project creation. If projects do not exist yet, full build/test validation is not required for this focus area alone.

#### Direct documentation impact

Update `docs/ENGINEERING.md` only if it currently claims these files do not exist or describes a different configuration state.

#### Deferred documentation impact

A later documentation synchronization pass may normalize setup descriptions, indexes, and cross-links.

### Focus Area B — Canonical engineering commands

#### Goal

Create the public engineering API under `eng/`.

#### Scope

Create:

```text
eng/common.sh
eng/restore.sh
eng/build.sh
eng/test.sh
eng/format.sh
eng/check.sh
```

Make scripts executable.

#### Required behavior

The commands must validate actual repository state. They must not silently pass when the solution or expected projects are missing after Focus Area C is complete.

#### Validation tier

Tier 1 — Focused implementation for individual commands.

Tier 2 — Standard local gate when `eng/check.sh` is complete.

#### Required validation

After Focus Area C exists, run:

```bash
./eng/check.sh
```

#### Direct documentation impact

Update:

```text
docs/ENGINEERING.md
docs/engineering/command-contract.md
```

The documentation must describe the commands that now exist, what they do, and which validation tier they support.

#### Deferred documentation impact

A later documentation synchronization pass may improve command examples in `README.md` and `AGENTS.md` if needed.

### Focus Area C — Minimal .NET solution and projects

#### Goal

Create the smallest useful .NET solution that supports future engine implementation.

#### Scope

Create:

```text
dotnet-ai-first-2d-game-engine.slnx
src/Agentic2D.Contracts/Agentic2D.Contracts.csproj
src/Agentic2D.Engine/Agentic2D.Engine.csproj
tests/unit/Agentic2D.Tests.Unit/Agentic2D.Tests.Unit.csproj
```

Add minimal compileable source and one smoke test.

#### Validation tier

Tier 2 — Standard local gate.

#### Required validation

Run:

```bash
./eng/check.sh
```

#### Direct documentation impact

Update:

```text
docs/engineering/future-dotnet-solution.md
```

The document must distinguish between projects that now exist and candidate future projects.

Update `docs/ENGINEERING.md` if its current-status section still says that the repository has no .NET projects or no executable `eng/` scripts.

#### Deferred documentation impact

A later documentation synchronization pass may update milestone indexes, architecture indexes, and root README contributor guidance.

### Focus Area D — Final local gate and implementation report

#### Goal

Prove that the engineering substrate works end-to-end and summarize the result.

#### Scope

Run the milestone validation commands and provide an implementation summary.

#### Required validation

Run:

```bash
./eng/check.sh
```

The implementation is incomplete unless this command exits with code 0 or the implementation summary reports the exact command failure and concise failure reason.

#### Validation tier

Tier 2 — Standard local gate.

#### Direct documentation impact

If validation cannot pass because of an environmental limitation, update `docs/ENGINEERING.md` only if the limitation changes repository command expectations.

#### Deferred documentation impact

A later documentation synchronization pass may update milestone status and command examples.

## Validation expectations

### Required final validation

The final required validation for the milestone is:

```bash
./eng/check.sh
```

This is Tier 2 — Standard local gate.

### Expected successful command chain

`./eng/check.sh` is expected to run:

```bash
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/format.sh --verify
```

### What must not be required

Do not require:

```text
release validation
package smoke tests
benchmarks
scenario validation
human review gates
packaged runtime validation
E2E tests
public documentation validation
```

Those validation classes are outside this milestone.

## Direct documentation impact

The implementation agent must update documentation directly only where repository behavior changes immediately.

Required direct documentation updates:

```text
docs/ENGINEERING.md
docs/engineering/command-contract.md
docs/engineering/future-dotnet-solution.md
```

Update `README.md` and `AGENTS.md` only if their existing instructions would become misleading after scripts and projects are created.

Do not perform a broad documentation synchronization pass.

## Deferred documentation synchronization hints

This milestone intentionally defers broad documentation cleanup.

Potential follow-up documentation synchronization areas:

```text
docs/MILESTONES.md milestone status/index updates
README.md contributor quickstart polishing
AGENTS.md routing refinements after commands exist
docs/ARCHITECTURE.md cross-link updates if project layout changed
docs/DECISIONS.md index update if ADR-0006 is added
docs/engineering/* examples and consistency cleanup
```

These are not required to complete this milestone unless the implementation itself makes an existing authoritative statement false.

## Completion criteria

The milestone is complete when all of the following are true:

- `global.json`, `.editorconfig`, `Directory.Build.props`, and `Directory.Packages.props` exist.
- Canonical `eng/` scripts exist and are executable.
- The minimal solution exists.
- `Agentic2D.Contracts`, `Agentic2D.Engine`, and `Agentic2D.Tests.Unit` exist and are included in the solution.
- `Agentic2D.Engine` references `Agentic2D.Contracts`.
- `Agentic2D.Tests.Unit` references the production projects it tests.
- A deterministic smoke unit test exists and runs through TUnit/MTP.
- `./eng/check.sh` exits with code 0, or the implementation summary reports the exact failing command and concise failure reason.
- Documentation directly affected by the new commands/projects has been updated.
- No runtime/game package dependency has been added.
- No product CLI has been implemented.
- No non-root `README.md`, TBP, or issue-template dependency has been introduced.

## Implementation summary requirements

The implementation agent’s final response must include:

```text
Files created/modified
SDK version selected
Projects created
Commands created
Validation command executed
Validation result
Any deviations from this milestone and why
Deferred documentation synchronization notes
```
