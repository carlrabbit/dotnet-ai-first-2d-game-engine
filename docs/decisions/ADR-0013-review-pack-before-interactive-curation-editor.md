# ADR-0013 — Build Review Pack and Generated Curation Workbench Before Interactive Curation Editor

## Status

Accepted.

## Context

The repository now has:

- a deterministic runtime smoke path;
- product CLI commands for runtime, scenario, content, and asset inspection work;
- authored scenario JSON;
- authored asset metadata for a smoke tile atlas;
- content validation artifacts;
- asset inspection artifacts.

The next asset-curation step needs human review. Without a review-pack model, humans and agents must inspect scattered artifact folders and infer what matters. Without a generated workbench artifact, a future interactive editor would lack a stable evidence model to present.

## Decision

Build a review pack and generated asset curation workbench before building an interactive curation editor.

The review pack aggregates existing artifacts into:

```text
review-summary.md
review-manifest.json
diagnostics.json
```

The asset curation workbench generates static review artifacts for the existing smoke asset:

```text
index.html
review-data.json
diagnostics.json
```

The workbench is generated output, not an editor application. It must not mutate source asset metadata.

## Consequences

Positive consequences:

- humans get a coherent evidence package before interactive UI work;
- agents get a machine-readable review manifest for follow-up work;
- future curation UI can consume a durable review data model;
- proposed visual labels remain separated from approved physical/gameplay semantics;
- the implementation remains headless-first and artifact-first.

Tradeoffs:

- the first workbench is less powerful than an interactive editor;
- approval-writing is deferred;
- image recognition and automatic semantic tagging remain out of scope.

## Non-goals

This decision does not approve:

- AI image recognition;
- automatic semantic tagging;
- interactive editing;
- renderer integration;
- map editing;
- public documentation or release-readiness work.
