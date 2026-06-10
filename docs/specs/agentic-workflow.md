# Agentic Workflow

## Authority

This document is authoritative for the intended human-agent workflow.

## Workflow

```text
human design intent
→ agent modifies project
→ engine validates project
→ engine runs scenarios
→ engine generates artifacts/reports/previews
→ human reviews result
→ change is accepted or revised
```

## Agent affordances

Agents should operate through:

- stable IDs;
- documented CLI/API commands;
- schema-validated content;
- structured diagnostics;
- machine-readable artifacts;
- review packs for human judgment.

Agents should not rely on:

- visual guessing from screenshots alone;
- hidden editor state;
- incidental file order;
- display names as source identity;
- manually patching many generated files.
