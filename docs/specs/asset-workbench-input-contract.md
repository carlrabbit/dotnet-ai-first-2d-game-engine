# Asset Workbench Input Contract

## Authority

Authoritative for M029 editable text-stream input, mouse/touch selection, command equivalence, focus behavior, and RDP/virtual-keyboard compatibility.

## Principle

Primary workbench interaction must not depend on raw physical key-down or scan-code events.

All input surfaces translate into the same canonical command model:

```text
editable text submission
mouse/touch selection
optional keyboard accelerator
headless command
→ agentic2d.asset-workbench-input-command.v1
→ validated canonical workbench action
```

## Schemas

```text
agentic2d.asset-workbench-input-state.v1
agentic2d.asset-workbench-input-command.v1
agentic2d.asset-workbench-input-result.v1
```

## Text-entry control

The control must:

- visibly display buffered text;
- permit correction before submission;
- accept text streams, paste, and composition input where supported;
- submit only through explicit Enter or Submit;
- provide Clear/Cancel;
- retain invalid input for correction;
- show validation messages;
- never submit because focus changed;
- remain usable after preview-host restart.

Partial text is operational state only and never a durable decision.

## Mouse and touch

Every ordinary numbered choice has a visible selectable row or button.

Click/touch and submitted number resolve to the same canonical command.

Controls use practical touch targets and do not require right-click, modifiers, function keys, or key chords.

## Bounded commands

The field may accept:

```text
<number>
open <number>
back
next
previous
find <text>
recent
help
cancel
```

It is not a general shell or unrestricted natural-language interface.

## Failure behavior

Invalid, stale, or ambiguous input:

- records no decision;
- leaves the session usable;
- presents a corrective message;
- preserves editable input when useful;
- never falls back to raw key interpretation.
