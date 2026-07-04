# ADR-0014 — Use One Bounded Smoke Journey for Broad Capability Expansion

## Status

Proposed for Milestone 011. Accept when implementation is accepted.

## Context

After Milestone 010, the repository can inspect assets, aggregate evidence, and generate a static non-mutating workbench. The next useful capabilities span safe review application, deterministic perception, maps, and runtime inspection.

Independent implementation risks incompatible diagnostics, provenance, references, review states, and artifacts. Unbounded combination risks an unreviewable general engine expansion.

## Decision

Implement one ordered smoke journey using:

```text
one synthetic atlas
one authored review-decision fixture
one deterministic perception run
one map
one runtime inspection scenario
one final review pack
```

All focus areas reuse stable-ID, fingerprint, provenance, diagnostic, status, exit-code, deterministic-ordering, and artifact-reference conventions. Each retains a separate permanent contract and acceptance gate.

## Consequences

Benefits: integration is proven, map/runtime consume existing truth, review packs gain cross-domain evidence, and broad AI work remains testable.

Costs: the milestone is large, order matters, mutation tests need isolation, and direct documentation spans several indexes/contracts.

## Rejected alternatives

- Four unrelated milestones with no shared journey.
- One unrestricted framework expansion.
- Interactive editor before explicit mutation contracts.
- Hosted vision/AI dependency for the smoke path.
