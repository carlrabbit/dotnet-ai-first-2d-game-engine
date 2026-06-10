# Future .NET Solution

## Authority

This document records the intended .NET project layout. It is not an instruction to create all projects immediately.

## Candidate project layout

```text
src/Agentic2D.Contracts
src/Agentic2D.Engine
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

## Recommended first slice

Create only:

```text
src/Agentic2D.Contracts
src/Agentic2D.Engine
tests/unit/Agentic2D.Tests.Unit
```

Add other projects when a milestone has a concrete need.
