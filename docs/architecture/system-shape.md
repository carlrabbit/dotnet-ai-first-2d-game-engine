# System Shape

## Authority

This document records the initial intended subsystem seams.

## Candidate seams

```text
Agentic2D.Contracts       shared IDs, commands, events, queries, diagnostics contracts
Agentic2D.Engine          deterministic simulation and command/event processing
Agentic2D.Runtime         runtime abstraction and host integration
Agentic2D.Runtime.Debug   inspectable development representation
Agentic2D.Runtime.Packaged optimized packaged representation
Agentic2D.Tools           product CLI
Agentic2D.Validation      schemas, content validators, diagnostics
Agentic2D.ScenarioRunner  deterministic scenario execution and artifacts
Agentic2D.AssetPipeline   asset inspection/import/provenance/previews
Agentic2D.SourceGen       generated IDs, registries, dispatch, serializers
```

Do not create all seams as projects until milestones justify them.
