# Asset Workbench Decision and Consequence Contract

## Authority

Authoritative for current asset review decisions, exact candidate/variant binding, typed corrections, groups, consequence confirmation, history, conflicts, staleness, and promotion eligibility.

M029 workbench session/input mechanics remain separately authoritative in their existing contracts. M047 corrects decision authority without requiring preview/UX redesign.

## Schemas

Current:

```text
agentic2d.asset-review-decision.v2
agentic2d.asset-review-summary.v2
agentic2d.asset-review-diagnostics.v2
agentic2d.asset-consequence-presentation.v1
```

Historical/readable only:

```text
agentic2d.asset-review-decision.v1
```

A v1 decision is not current promotion authority and is never automatically migrated to v2.

## Decision Subject

A v2 decision binds the exact subject:

```text
campaignId
candidateId
candidateFingerprint
selectedVariantId + selectedVariantFingerprint when applicable
typed corrections
consequence presentation/response when applicable
sequence
superseded decision reference/status
```

The current candidate and variant are defined by `docs/specs/canonical-asset-candidate-contract.md`.

Input method does not alter semantics. Text, mouse/touch, optional accelerators, and headless commands must resolve to the same canonical v2 decision subject.

## Actions

The existing bounded action vocabulary may remain:

```text
accept-proposal
choose-alternative
approve-with-corrections
reject
defer
mark-unused
split-group
merge-group
approve-group
review-exceptions
request-another-proposal
mark-source-unsuitable
```

Only an effective current approval action with a valid exact candidate subject can authorize promotion.

`choose-alternative` requires one current typed variant and stores its exact variant fingerprint.

`approve-with-corrections` requires typed corrections. Free-text reason/note is non-processing metadata.

Unknown correction/action values fail validation or are non-promotable. Promotion never guesses their meaning.

## Typed Corrections

M047 promotable correction kinds are bounded by the processing contract:

```text
image-region override within validated source bounds
padding preserve / deterministic alpha-bounds trim
positive integer nearest-neighbor scale
pivot/anchor metadata
explicit animation frame order
audio trim by integer PCM sample-frame boundaries
```

An exact-copy approval may have no corrections.

Gain, channel conversion, resampling, opaque enhancement, generative alteration, and unrestricted free-form transforms are unsupported in M047 and block promotion if requested.

## Consequences and Gameplay Boundary

Collision, walkability, damage, interaction, collection, progression, rendering, animation-event, and sound-cue proposals may still require explicit consequence presentation in the workbench flow.

Presentation-only approval remains supported and never creates gameplay binding.

M047 validates structural decision/consequence records mechanically. M048 owns whether the actual human preview/review experience is visually/audibly correct and usable.

## History and Effective Decisions

Decisions are append/history-preserving. Later changes supersede rather than erase.

Effective candidate-level state is deterministic by decision sequence and explicit supersession rules.

A group decision stores the exact candidate IDs and candidate fingerprints in the group at decision time. Promotion resolves group state to candidate-level effective decisions. A later individual decision affects only that candidate unless another explicit group operation changes the group.

A reject/defer/unused/superseded/stale decision does not authorize promotion.

## Staleness

Promotion re-resolves the exact candidate and selected variant against current source/profile/campaign authority.

A v2 decision is current only when:

```text
candidateFingerprint == current resolved candidateFingerprint
AND selected variant fingerprint matches when selected
AND every typed correction remains valid for the resolved source/selection
AND exact referenced source-file SHA-256 is current and accessible
```

Unrelated profile/campaign changes that do not change the canonical candidate/variant do not stale the decision.

Source unavailable, source hash mismatch, candidate/variant removal/change, invalid source bounds/frame references, or invalid correction makes the decision stale/non-promotable.

Never silently retarget or refresh a decision subject.

## V1 Handling

V1 decision logs may be listed, inspected, summarized, and retained as historical authoring evidence.

Because v1 lacks the exact candidate/variant/structured-recipe binding required by M047, it cannot authorize an M047 promotion plan or current promoted generation.

The only transition to trusted current authority is explicit re-review producing a v2 decision over the current canonical candidate.

No heuristic or inferred migration is allowed.
