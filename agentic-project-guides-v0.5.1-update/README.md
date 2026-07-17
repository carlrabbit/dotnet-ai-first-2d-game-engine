# Agentic Project Guides

Version: 0.5.1

This repository contains a versioned guide system for planning, executing, validating, reviewing, documenting, and migrating AI-first software projects.

## Patch purpose

Version 0.5.1 corrects a packaging defect introduced in v0.5.0: several prompt templates were unintentionally shortened and lost important operational requirements.

The corrected templates restore the full v0.3.0 planning and execution structure while retaining the v0.4.0 and v0.5.0 additions for:

- repository-local human review;
- thin cross-platform launchers over tested .NET command implementations;
- constrained agent runtime environments;
- resumable validation suites;
- fingerprinted validation receipts;
- fast aggregate verification.

## Migration note

Repositories already updated to v0.5.0 should apply:

```text
migrations/guide-system-v0.5.0-to-v0.5.1.md
```

Repositories that already used the shortened v0.5.0 `update-to-latest-guide-system.md` prompt should additionally use:

```text
templates/prompts/repair-v0.5.0-shortened-prompt-application.md
```

That repair prompt creates a repository-specific assessment and migration package rather than assuming every affected repository needs the same changes.
