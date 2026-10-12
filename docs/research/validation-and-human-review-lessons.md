# Validation and Human-Review Lessons

## Status

Current reconstructed planning findings; non-authoritative.

## Question

What validation mistakes repeatedly appeared during corrective audits, and what planning heuristics should future milestones carry forward?

## Finding 1 — aggregate green is not criterion proof

Several historical defects survived broad passing validation because a suite proved a nearby capability rather than the exact obligation.

The recurring failure pattern is:

```text
suite/shard says PASS
but
one materially distinct behavior path was never exercised
```

This is the planning problem that guide-system 0.9's stable AC/EC topology addresses.

Future planning should ask:

- What exact obligation is independently verifiable?
- Are there materially different proof paths?
- Which validation gate actually exercises each path?
- What independent observation establishes success?

## Finding 2 — evidence must cross the boundary being claimed

Weak evidence patterns include file existence, producer-authored success flags, expected labels, screenshot existence, or one broad command returning zero.

Stronger evidence derives the claimed fact from the observed target: reload persisted state, independently compare materialized content, inspect real frame progression, verify audio identity/duration, or prove the exact review fixture → bundle → materialization-subject chain.

## Finding 3 — validation should fail substitutes

A strong gate should reject plausible wrong implementations, such as:

- static texture with changing animation status text;
- wrong candidate media carrying the correct label;
- audio-copy standing in for a trim comparison;
- legacy promoted workspace accepted as current authority;
- sibling evidence standing in for another evidence case.

Planning implication: where a shortcut is likely, include a negative or contrast case that makes the shortcut fail.

## Finding 4 — human review is subjective, not manual machine validation

Humans should judge visual readability, interaction clarity, audio quality, gameplay feel, and creative/presentation quality.

Machines should judge fingerprints, schemas, exact identity binding, determinism, path/reference integrity, staleness, readiness, and persistence correctness.

Do not ask the human to verify what software can prove.

## Finding 5 — review launch is support, not human-decision authority

The simple Review Workbench owns Accept/Reject, but launching a preview from that exact session is optional. A reviewer may already have inspected the content elsewhere.

This is distinct from domain workflows where an interactive asset-approval action requires an exact current preview acknowledgement. Do not conflate milestone-review UX with asset-curation decision authority.

## Finding 6 — reviewability is machine-checkable up to the subjective edge

Before subjective judgment, machine validation should establish that the intended fixture is launchable, the correct modality-specific surface is selected, irrelevant controls are absent, visible controls affect the subject, review context is clear, and environment prerequisites are diagnosed truthfully.

Only then does the human answer the perceptual question.

## Finding 7 — active-platform capability and harness capability are different

A graphical test failing from a non-interactive Windows session does not prove the machine lacks graphics capability.

Recent project work showed Raylib/OpenGL became available after entering an interactive desktop session.

Planning implication: for native graphics/audio integration, state target and execution locus/capability. Distinguish machine capability, interactive-session capability, automation-harness capability, and product failure.

## Relationship to guide-system 0.9

New AI-executed milestone planning should:

- assign stable AC IDs;
- use selective EC IDs for materially distinct proof paths;
- seed the lossless execution ledger before ready;
- map VAL gates to exact AC/EC coverage;
- preserve durable criterion-to-proof completion traces.

This artifact preserves why those practices matter in this repository.

## Related authority

- `AGENTS.md`
- `docs/engineering/validation-tiers.md`
- `docs/specs/simple-human-review-workbench-contract.md`
- `docs/HUMAN-REVIEW.md`
- `docs/engineering/platform-verification.md`
- `docs/milestones/MILESTONE-051-adopt-guide-system-v0.9.1.md`

## Freshness

Last reconstructed: 2026-10-12.

Revalidate after substantial changes to the review framework, validation host, or guide execution model.
