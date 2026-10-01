# ADR-0060 — Actual Candidate Preview Shares the M047 Materialization Subject

## Status

Accepted for M048.

## Context

M047 corrected candidate, decision, recipe and promotion authority, but the historical M029 preview still renders fixed smoke audiovisual content.

A candidate label is not proof that the user saw or heard the material later bound by the decision.

The generic simple-review implementation is also currently M038-specific and presents placeholder content, while project review policy requires a launchable actual experience for subjective acceptance.

## Decision

Interactive M048 curation derives one canonical materialization subject from the current M047 candidate, selected variant and typed corrections.

Preview media is generated through the same M047 resolver/recipe/materializer used by promotion and packaged as disposable preview authority.

The preview host loads and acknowledges that exact subject through preview IPC v2 and presents it through actual engine rendering/animation/sound paths.

An approval-like interactive v2 decision may commit only while the exact current materialization subject has a matching successful preview acknowledgement.

Preview exploration is operational draft state until explicit decision commit.

Interactive group approval cannot claim human preview for unpreviewed candidates.

The generic simple Review Workbench keeps its v2 durable review authority and Restart/Reject/Accept shell. Engineering/debug infrastructure gains only a bounded explicit registry for actual simple-review content experiences. M038 remains registered; M048 registers its three asset-curation experiences.

## Consequences

- the user sees/hears the subject they are actually approving;
- variant/correction preview and promotion use one deterministic processing implementation;
- preview restart/staleness can be proven by exact fingerprints;
- headless M047 decisions remain valid but are not automatically called human-preview-backed;
- M048 human review judges visual/UX/audio quality only;
- M049 can later consume promoted generations without redefining curation authority.
