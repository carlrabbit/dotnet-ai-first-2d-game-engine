# Planning and Dogfood Lessons — Conversation-Led Reconstruction

## Status

Project-specific planning context; non-authoritative.

This document does not replace the external agentic-project guide. It preserves project-specific lessons learned while applying and evolving that methodology.

## Question

Which planning and execution lessons are specific enough to this engine project that future planners should remember them, without copying generic guide methodology into the repository?

## Planning and implementation are different jobs

A recurring project lesson was that AI implementation works best when planning has already resolved material design choices.

Planning should settle:

- architecture;
- semantic ownership;
- compatibility;
- scope and exclusions;
- acceptance;
- validation target/depth;
- human-review question;
- handoff boundaries.

Implementation should own:

- concrete files/types;
- local decomposition;
- refactoring sequence;
- test arrangement;
- low-level mechanics;
- execution progress.

When implementation is forced to invent project-level semantics, agents tend to optimize locally and can produce coherent but wrong systems.

## Repository authority must be sufficient

Ordinary implementation should not depend on:

- old planning chats;
- external guide text;
- hidden planner memory;
- transport package prose that never becomes repository authority.

Planning packages are transport.

After application, the repository-local milestone/spec/architecture/decision set is authority.

This is especially important for disconnected implementation.

## Capability provider versus consumer

The engine is primarily a capability provider.

That means provider validation should prove the capability contract directly through appropriate:

- unit tests;
- integration tests;
- contract/golden tests;
- deterministic artifacts;
- CLI/process tests;
- consumer-boundary tests where distribution is involved.

A game/workbench scenario is useful bounded dogfood, but it should not silently redefine the reusable engine around one fixture.

## Bounded dogfood

Dogfood has two purposes:

1. prove the capability can be consumed realistically;
2. expose missing extension/authoring surfaces.

It should not:

- become hidden product authority for the engine;
- force every engine milestone to behave like a game feature milestone;
- require subjective review where the provider capability is mechanically decidable;
- make sample-specific concepts look universal.

The settlement game, Signal Passage, asset workbenches and Review Workbench have each served as bounded consumers.

## Coherent vertical work beats fragmented micro-milestones

The project repeatedly moved toward larger coherent transformations:

- simulation foundation;
- detailed autonomous slice;
- multi-fidelity execution;
- settlement infrastructure;
- hardening;
- product shell.

Reason:

Agents can implement substantial software cheaply enough that excessive milestone fragmentation creates planning/authority overhead and weakens the review seam.

A milestone should still be bounded and tractable, but not split purely to mimic human sprint sizing.

## Experiments should answer empirical questions

Another repeated correction:

Do not turn every new software subsystem into a tiny experiment.

Architecture can often be reasoned about sufficiently to implement a coherent first version.

Use experiments when the uncertain claim is genuinely empirical, such as:

- whether agents are more effective with a new authoring surface;
- whether humans find a review flow usable;
- performance/scaling limits;
- external platform/device behavior;
- renderer/audio/driver behavior that cannot be deduced reliably.

The software capability and the experiment evaluating its value are separate deliverables.

## Validation should target the claim, not the implementation's self-description

The project has repeatedly found bugs where:

- code existed;
- tests were green;
- an artifact said "passed";
- but the intended behavior did not occur.

Future planning should explicitly ask what independent observation would disprove a plausible shortcut implementation.

This project-specific experience strongly motivated the current AC/EC evidence topology adopted under guide 0.9.

## Human review should exist only at the subjective boundary

Humans are scarce compared with agent implementation/validation.

Use human review for judgments software cannot reliably decide.

Automate:

- structural correctness;
- identity;
- invariants;
- exact behavior;
- reproducibility;
- launchability/readiness.

Ask humans to judge:

- readability;
- usability;
- visual/audio quality;
- feel;
- creative intent.

This keeps the human loop high-value.

## Planning knowledge should survive without becoming authority

The project accumulated substantial useful reasoning in conversations.

The current research model gives that reasoning a place:

```text
conversation / exploratory reasoning
→ curated docs/research planning knowledge
→ project authority only when an operative conclusion is required
→ ready milestone
→ implementation
```

Research should preserve:

- rationale;
- rejected alternatives;
- historical context;
- uncertainty;
- revalidation triggers.

It should not become an accidental second spec tree.

## Deferred synchronization is intentional

Broad documentation cleanup should not expand every milestone.

Direct authority needed for the milestone belongs in the milestone.

Broader index/capability-summary synchronization can be deferred explicitly and resolved in a dedicated pass.

This has been useful while corrective and concurrent tracks were active.

## Planning should use tracks, not milestone numbers alone

Corrective milestones consumed numbers that had previously been mentally attached to future feature work.

The project now benefits from distinguishing:

- corrective/architectural closure;
- active UI research/capability work;
- paused product roadmap;
- supporting/meta work.

Numbers identify milestones; they do not explain strategy.

## Durable project-specific heuristics

Future planning should prefer:

1. one clear semantic authority;
2. explicit provider/consumer boundary;
3. bounded real dogfood;
4. coherent milestone-sized transformations;
5. project decisions resolved before implementation;
6. machine evidence up to the true subjective edge;
7. experiments only for empirical uncertainty;
8. durable research for expensive-to-rediscover reasoning;
9. current repository authority over planning-chat recollection;
10. exact completion evidence over broad green-suite assumptions.

## Relationship to external guide

Generic methodology remains owned by:

`carlrabbit/agentic-project-guides`

This document only records why particular aspects matter in this engine repository and the recurring failure modes that shaped their use here.

## Freshness

Reconstructed through 2026-10-14.

Revalidate if the repository changes role, implementation ceases to be predominantly agent-executed, or the external guide materially changes planning/research/execution boundaries.
