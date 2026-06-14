# Guide Sync Hint — Milestone 006 Validation Evidence Review Follow-Up

## Status

pending

## Origin

Milestone 006 planning package.

## Purpose

Capture post-implementation human review findings about content validation evidence quality.

Milestone 006 requires human review for evidence usefulness, not for deciding whether validation passed.

## Review questions

After implementation, inspect generated validation artifacts for passing and failing content and answer:

- Can malformed scenario content be diagnosed from `result.json` and `diagnostics.json` without guessing?
- Are diagnostic IDs stable and granular enough for future agents?
- Are target paths, fields, and item IDs represented clearly?
- Is `validated-items.json`, if produced, useful and deterministic?
- Is the content validation foundation reusable for future asset metadata and map metadata?
- Did the implementation avoid overbuilding a full schema registry too early?

## Suggested documentation-sync action

If review identifies durable project rules, move them into active project docs such as:

```text
docs/specs/content-validation-contract.md
docs/artifacts/content-validation-artifact-contract.md
docs/CONTENT.md
```

If review identifies only cleanup or examples, narrow this hint or delete it after the cleanup is complete.

## Completion criteria

This hint can be deleted when the human review findings are either:

- incorporated into active docs;
- explicitly deferred to a later milestone; or
- judged unnecessary after reviewing the implementation artifacts.

## Notes

This file is deferred documentation synchronization metadata. Ordinary implementation agents must ignore `.guide-sync/`.
