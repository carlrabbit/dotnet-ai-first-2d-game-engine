# Guide Sync Hint — Audit Execution Prompt Handoff After Guide System v0.3.0

## Status

pending

## Origin

Milestone 009 guide-system v0.3.0 migration package.

## Purpose

Guide system v0.3.0 adds an execution-handoff expectation for disconnected planning and implementation agents. Planning packages should now be accompanied by a filled execution prompt in chat so a later implementation agent can work from the package without reconstructing planning context or reading the external guide repository.

## Deferred work

During a later documentation synchronization or planning-process review, inspect recent and future planning workflows for:

```text
filled execution prompt included in chat
primary milestone document identified
required target-repository authority documents listed
external guide repository explicitly not required for implementation
.guide-profile.json and .guide-sync/ ignored unless task mode requires them
validation commands listed concretely
non-goals and forbidden changes stated
```

Do not copy guide prompt templates into the repository.

## Completion criteria

This hint can be deleted when:

- future planning responses consistently include filled execution prompts for disconnected implementation agents;
- any repository-local process notes, if added, describe the expectation without copying external prompt templates;
- implementation prompts do not say only “upgrade to latest” or “implement the ZIP” without execution constraints;
- ordinary implementation agents still are not required to read the external guide repository.

## Notes

This hint is process synchronization metadata, not implementation authority.
