# Reusable Asset Annotation and Cleanup Contract

## Authority

Authoritative for durable local human corrections to reusable discovery and safe generated-metadata cleanup.

## Metadata classes

```text
generated observations: disposable and rebuildable
reusable human annotations: retained until explicitly removed
```

Schema: `agentic2d.asset-reusable-annotation.v1`.

Required fields: canonical annotation ID, source ID, fingerprint scope, action, target, correction/value, reason, author when provided, provenance, and status.

Allowed actions:

```text
exclude-file
correct-grid
reject-grid
split-region-group
merge-region-group
reject-animation-group
correct-animation-order
exclude-audio
correct-source-scope
associate-license-observation
note
```

Reusable annotations must not encode game-specific semantics such as player, walkable, blocked, damaging, interactable, collectible, quest item, or progression blocker.

An annotation is applicable, incompatible, unresolved, or explicitly removed. Source changes never silently retarget it. Incompatible annotations remain retained and produce diagnostics.

Generated cleanup may remove profiles, previews, sessions, and cache. It must not remove raw source, annotations, campaign truth, or committed review decisions.
