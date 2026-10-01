# Asset Preview Review Surface Contract

## Authority

Authoritative for the reviewer-facing presentation of an exact M048 asset-preview subject.

M048 remains authoritative for preview identity, materialization subject, bundle contents, animation/audio semantics, and review launch mapping.

## Principle

One preview executable does not imply one visible union of controls.

```text
shared preview infrastructure
+ structured mediaKind
→ modality-specific review surface
```

A reviewer sees only controls and context that can meaningfully affect or explain the current subject.

## Modality Dispatch

Canonical current modalities:

```text
image
animation
audio
```

Dispatch uses structured preview bundle/scene media kind.

Never infer modality from opaque candidate IDs, review IDs, filenames, display names, or available buttons.

Unknown/unsupported modality renders a diagnostic surface with no misleading modality controls.

## Common Review Context

Every modality surface shows human-readable context equivalent to:

```text
modality / review type
candidate identity
presentation role/purpose
selected variant when any
typed correction summary when any
raw/base meaning
processed/current-draft meaning
subjective review question
```

Fingerprints and hashes are diagnostics, not required human context.

The reviewer should not need to remember the parent Review Workbench question after opening the child preview.

## Common Infrastructure

May be shared:

```text
window lifecycle
theme/high contrast
context header
bundle/scene loading
diagnostics
capture
layout helpers
native resource lifecycle
```

Modality controls are not shared merely for convenience.

## Image Surface

May expose, when meaningful:

```text
Source
Processed
Isolated region
Overlay toggle
Nearest / Smooth
```

Each visible control must have an observable purpose for the current image subject.

Must not expose:

```text
Play animation
Pause animation
Step animation
Reset animation
animation speed
Play Raw audio
Play Processed audio
Stop audio
```

## Animation Surface

Baseline controls:

```text
Play
Pause
Step
Reset
```

Optional when meaningful:

```text
speed
variant/order comparison
bounds/pivot overlay
```

Must expose bounded playback state such as current frame/frame count or equivalent.

Must not expose:

```text
audio playback controls
generic image-only source/filter/isolation controls with no animation-review purpose
```

Buttons must manipulate actual animation state, not only labels.

## Audio Surface

Baseline controls:

```text
Play Raw
Play Processed
Stop
```

Context includes:

```text
raw duration
processed duration
selected variant/trim summary
audio-device readiness/diagnostic
```

No auto-play.

Must not expose:

```text
animation controls
animation speed
image filtering
image region isolation
image overlays
```

## Control Relevance Rule

For every visible control:

```text
current subject
→ documented state/effect
→ observable outcome or necessary diagnostic
```

If no meaningful edge exists, omit the control.

Do not keep irrelevant controls disabled as decorative placeholders unless the disabled state communicates a material condition the reviewer must understand.

## Review Decision Boundary

This surface does not own Accept/Reject.

The parent Review Workbench remains review-decision authority.

Launching the preview is optional for recording a human review decision.

M050 does not add a launch-before-Accept guard.

## Evidence

Machine validation may use a backend-neutral surface/view model or structured UI observation to prove:

```text
selected modality
visible controls
absent controls
review context
control-state transitions
```

A producer-authored `correctControls=true` boolean alone is not sufficient.
