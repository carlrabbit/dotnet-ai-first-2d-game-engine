# Terminology

## Authority

This document is authoritative for project vocabulary.

## Core terms

| Term | Meaning |
|---|---|
| Agentic engine | A game engine designed so AI agents can implement, modify, validate, and iterate using structured interfaces and evidence. |
| Headless-first | The engine can be operated through CLI/API commands without a graphical editor. |
| Artifact-first | Commands produce machine-readable and reviewable evidence such as reports, diagnostics, traces, previews, and overlays. |
| Stable ID | A durable identifier used instead of filenames, display text, hierarchy position, or visual coordinates. |
| Scenario | Deterministic product/runtime validation with structured inputs, assertions, and artifacts. |
| Content | Authored non-code game/project data such as entities, maps, assets, taxonomies, rules, UI, and scenarios. |
| Generated artifact | A file derived from source content or code. It must be reproducible or explicitly marked as non-source. |
| Human review gate | An explicit validation step for outputs automation cannot fully judge. |
| Visual label | Semantic visual interpretation, such as `grass`, `flower`, `wall`, or `water`. |
| Physical behavior | Gameplay-relevant behavior, such as walkability, collision, navigation cost, or damage. |
| Behavior module | C# or optional F# code that reads queries and emits commands instead of mutating the world directly. |
| Debug runtime | Development representation optimized for inspection, diagnostics, JSON, source locations, and agent use. |
| Packaged runtime | Release representation optimized for performance, compact resources, generated dispatch, and minimized diagnostics. |
