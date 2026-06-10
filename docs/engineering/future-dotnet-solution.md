# Future .NET Solution

## Authority

This document records the intended .NET project layout. It is not an instruction to create all projects immediately.

## Current project layout

Milestone 001 created the smallest useful solution:

```text
dotnet-ai-first-2d-game-engine.slnx
src/Agentic2D.Contracts
src/Agentic2D.Engine
tests/unit/Agentic2D.Tests.Unit
```

Current project references:

```text
Agentic2D.Engine -> Agentic2D.Contracts
Agentic2D.Tests.Unit -> Agentic2D.Contracts
Agentic2D.Tests.Unit -> Agentic2D.Engine
```

## Candidate future project layout

```text
src/Agentic2D.Runtime
src/Agentic2D.Runtime.Debug
src/Agentic2D.Runtime.Packaged
src/Agentic2D.SourceGen
src/Agentic2D.Tools
src/Agentic2D.AssetPipeline
src/Agentic2D.ScenarioRunner
src/Agentic2D.Validation
tests/unit/Agentic2D.Tests.Unit
tests/integration/Agentic2D.Tests.Integration
tests/package-smoke/Agentic2D.Tests.PackageSmoke
benchmarks/Agentic2D.Benchmarks
```

## Slice policy

The current projects are enough for the base engineering substrate. Add candidate future projects only when a milestone has a concrete need.

```text
Agentic2D.Tools: first product CLI command
Agentic2D.AssetPipeline: asset inspection or import work
Agentic2D.ScenarioRunner: deterministic scenario execution
Agentic2D.Runtime*: runtime host implementation
Agentic2D.SourceGen: source generation implementation
Agentic2D.Validation: shared validation behavior
```
