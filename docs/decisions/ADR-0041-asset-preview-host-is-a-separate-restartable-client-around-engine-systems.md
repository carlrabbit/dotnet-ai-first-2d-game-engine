# ADR-0041 — Asset Preview Host Is a Separate Restartable Client Around Engine Systems

## Status

Accepted.

Clarified by ADR-0060 for M048 exact candidate/materialization-subject binding.

## Decision

Use one separate restartable preview-host process per workbench session.

It communicates through a versioned local IPC contract and uses actual engine rendering, animation, sound projection, and isolated native adapters.

The workbench owns UI input, sessions, aliases, operational curation state, decisions, and promotion requests.

The preview host owns temporary preview/playback/comparison/capture state only.

M048 requires the host to acknowledge the exact current materialization subject rather than merely displaying a candidate label.

## Consequences

- preview crashes/restarts do not erase workbench input or durable decisions;
- reconnect requires rebuilding/revalidating and acknowledging the current exact subject;
- no second renderer, audio engine, animation evaluator, candidate resolver, or media processor is introduced;
- the operational IPC may advance when needed because both local components are updated together;
- preview state never becomes promotion authority.
