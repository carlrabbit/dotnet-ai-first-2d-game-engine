# Architectural Correction Lessons

## Status

Current reconstructed planning findings; non-authoritative.

## Question

What reusable architectural lessons emerged from the corrective milestone train, beyond the individual contracts that now encode the operative rules?

## Finding 1 — labels and wrappers are not semantic identity

Observed repeatedly in asset and runtime work:

- a candidate label is not proof that displayed media is that candidate;
- a save wrapper is not proof that the real authoritative world was persisted;
- a scenario name is not proof that the intended execution path ran;
- a producer-authored success field is not independent evidence.

Planning implication: whenever an abstraction claims to represent an authoritative subject, trace the identity chain to underlying semantic data and require an independent observation at the relevant boundary.

Current authority examples include canonical asset candidate fingerprints, materialization-subject fingerprints, canonical save/content identity, and runtime snapshot fingerprints.

## Finding 2 — one semantic authority is safer than parallel representations

Several corrections converged on removing parallel truth:

- one authoritative component world rather than a persistence-only or evaluator-owned shadow;
- one SimulationWorld payload for durable simulation state;
- shared executor-neutral semantics across detailed and abstract simulation;
- one M047 materialization path for preview and promotion;
- one semantic UI model for headless/Raylib/future rendering in the new UI direction.

Planning implication: a second representation is acceptable as derived evidence, cache, projection, or adapter state only when authority and reconstruction are explicit.

## Finding 3 — proposal and commit boundaries need to be explicit

Corrected systems increasingly use:

```text
snapshot/read
→ evaluate/propose
→ validate
→ atomic commit
→ factual evidence
```

Examples include behavior/spatial evaluation returning proposed mutations, transactional runtime mutation, curation drafts before durable asset decisions, and staged asset generations before atomic current-generation publication.

Planning implication: separate exploratory/proposed state from durable authority. Avoid APIs where observation, decision and mutation happen implicitly in one step.

## Finding 4 — equivalence needs an explicit tolerance model

Multi-fidelity simulation showed that "roughly equivalent" is too weak.

Useful distinction:

- semantic/rule invariants: zero tolerance;
- timing differences: bounded by explicit execution quanta;
- fixed-horizon state: bounded only by current in-flight allowances;
- same-schedule continuation: exact equality.

Planning implication: when multiple execution strategies, adapters or representations are intended to be equivalent, define equivalence classes and tolerances before comparative validation.

## Finding 5 — recovery must preserve a valid authority state

Persistence and promotion corrections both converged on:

```text
prepare
→ independently validate
→ publish atomically
→ re-read/verify authority
```

Planning implication: durable replacement should leave either the prior valid state or the complete new valid state. Partial publication must never become current authority.

## Finding 6 — human-visible tools are part of the capability

M048/M050 showed that technically correct plumbing can still fail its purpose if the human review surface is confusing or disconnected from the real subject.

Planning implication: when a capability exists to support human judgment, reviewer-facing context and modality-specific interaction are part of the capability contract. Machine validation proves structural truth; human review judges the irreducibly perceptual result.

## Finding 7 — complete software can precede the experiment

The UI direction clarifies a methodology point: building coherent software is ordinary engineering. For agentic-development ideas, the experiment should test whether the capability improves agent outcomes, not whether a toy implementation can be made to run.

Planning implication: do not overuse spike milestones where software architecture can be reasonably deduced and implemented. Reserve experiments for uncertain empirical claims, especially agent effectiveness, usability, performance envelopes, or external-runtime behavior.

## Related authority

- `docs/specs/runtime-snapshot-and-mutation-authority-contract.md`
- `docs/specs/multi-fidelity-equivalence-contract.md`
- `docs/specs/canonical-runtime-persistence-contract.md`
- `docs/specs/canonical-asset-candidate-contract.md`
- `docs/specs/approved-asset-and-deterministic-promotion-contract.md`
- `docs/specs/actual-candidate-preview-and-curation-contract.md`
- `docs/architecture/agentic-ui-direction.md`

## Freshness

Last reconstructed: 2026-10-12.

Revalidate when a future subsystem intentionally departs from these patterns or a new renderer/authoring model introduces a materially different authority boundary.
