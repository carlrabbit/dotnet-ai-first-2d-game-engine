# Asset Workbench Decision and Consequence Contract

## Authority

Authoritative for M029 decisions, structured corrections, groups, consequence confirmation, history, conflicts, and staleness.

## Schemas

```text
agentic2d.asset-review-session.v1
agentic2d.asset-review-decision.v1
agentic2d.asset-review-summary.v1
agentic2d.asset-review-diagnostics.v1
agentic2d.asset-consequence-presentation.v1
```

Decisions are history-preserving. Later changes supersede rather than erase.

Collision, walkability, damage, interaction, collection, progression, rendering, animation-event, and sound-cue implications require explicit consequence presentation. Presentation-only approval is supported and never creates gameplay binding.

Fingerprint mismatch makes decisions stale and blocks promotion until explicit current review.

Input method does not alter decision semantics: text, mouse/touch, optional accelerators, and headless commands must resolve to identical canonical decisions.
