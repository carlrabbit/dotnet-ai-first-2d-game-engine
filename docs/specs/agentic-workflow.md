# Agentic Workflow

## Authority

This document is authoritative for the intended human/planning/execution/review workflow.

## Workflow

```text
human design intent / planning
-> ready milestone
-> execution agent modifies project
-> engine performs automated validation and scenarios
-> engine generates machine evidence
-> when required, human reviews only the remaining subjective result
   -> accept: milestone may complete
   -> small correction: execution agent iterates; reviewer chooses when to restart/review
   -> material contract issue: return to planning
```

Human review does not duplicate automated validation.

The reviewer's normal job is to perceive the actual result and make a small accept/reject judgment, not to reconstruct correctness from JSON, reports, hashes, or source code.

Conversational feedback remains in the planning/execution loop. The simple review workbench is not an issue tracker or durable comment system.

## Agent affordances

Agents should operate through:

- stable IDs;
- documented CLI/API commands;
- schema-validated content;
- structured diagnostics;
- machine-readable artifacts;
- deterministic scenarios/shards;
- bounded review experiences for genuinely human judgment.

Agents should not rely on:

- visual guessing from screenshots alone when live interaction is the review subject;
- hidden editor state;
- incidental file order;
- display names as source identity;
- manually patching many generated files;
- asking a human to verify predicates that automation can decide.
