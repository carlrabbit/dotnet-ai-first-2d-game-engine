# Future .NET Solution

## Authority

This document records the intended .NET project layout. It is not an instruction to create all projects immediately.

## Current project layout

The current solution contains the base contracts, engine, scenario runner, content validation, product CLI, and unit tests:

```text
dotnet-ai-first-2d-game-engine.slnx
src/Agentic2D.Contracts
src/Agentic2D.Engine
src/Agentic2D.ScenarioRunner
src/Agentic2D.Tools
src/Agentic2D.Validation
tests/unit/Agentic2D.Tests.Unit
```

Current project references:

```text
Agentic2D.Engine -> Agentic2D.Contracts
Agentic2D.ScenarioRunner -> Agentic2D.Contracts
Agentic2D.ScenarioRunner -> Agentic2D.Engine
Agentic2D.ScenarioRunner -> Agentic2D.Validation
Agentic2D.Tools -> Agentic2D.Contracts
Agentic2D.Tools -> Agentic2D.Engine
Agentic2D.Tools -> Agentic2D.ScenarioRunner
src/Agentic2D.Workspaces
Agentic2D.Tools -> Agentic2D.Validation
Agentic2D.Tests.Unit -> Agentic2D.Contracts
Agentic2D.Tests.Unit -> Agentic2D.Engine
Agentic2D.Tests.Unit -> Agentic2D.ScenarioRunner
Agentic2D.Tests.Unit -> Agentic2D.Tools
Agentic2D.Tests.Unit -> Agentic2D.Validation
```

## Candidate future project layout

```text
src/Agentic2D.Runtime
src/Agentic2D.Runtime.Debug
src/Agentic2D.Runtime.Packaged
src/Agentic2D.SourceGen
src/Agentic2D.AssetPipeline
tests/unit/Agentic2D.Tests.Unit
tests/integration/Agentic2D.Tests.Integration
tests/package-smoke/Agentic2D.Tests.PackageSmoke
benchmarks/Agentic2D.Benchmarks
```

## Slice policy

The current projects are enough for the base engineering substrate and content validation foundation. Add candidate future projects only when a milestone has a concrete need.

```text
Agentic2D.AssetPipeline: asset inspection or import work
Agentic2D.Runtime*: runtime host implementation
Agentic2D.SourceGen: source generation implementation
```
