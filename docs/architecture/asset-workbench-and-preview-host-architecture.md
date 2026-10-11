# Asset Workbench and Preview Host Architecture

## Authority

Authoritative for current asset-workbench / preview-host process ownership after M047 and M048.

## Architecture

```text
Shared asset home
  raw source registry
  discovery profiles
        |
        v
M047 canonical candidate resolver
        |
        v
Asset Workbench
  M029 session / aliases / RDP-safe input
  M048 operational curation draft
  v2 decision commit guard
        |
        | builds through M047 recipe/materializer
        v
Disposable exact preview bundle
        |
        | preview IPC v2
        v
Preview Host
  validate exact subject/bundle
  playback/comparison state
        |
        v
Actual engine presentation
  rendering
  animation
  sound projection
        |
        v
Raylib asset preview surface
```

## Durable Authority

Durable authoring/promotion authority remains:

```text
M047 canonical candidate
M047 asset-review-decision.v2
M047 promoted generation
```

M048 preview bundle, playback state, curation draft, process acknowledgement and captures are operational/rebuildable authoring state.

## Workbench Ownership

Workbench owns:

- session and navigation;
- ephemeral aliases;
- text/mouse/touch canonical actions;
- operational curation draft;
- candidate/variant/correction selection;
- consequence interaction;
- exact preview acknowledgement guard;
- durable v2 decision write.

Workbench does not own a duplicate media processor.

## Preview Host Ownership

Preview host owns:

- local IPC lifecycle;
- validated preview-bundle load;
- current subject acknowledgement;
- raw/processed display mode;
- animation playback/step state;
- manual audio playback state;
- overlays/capture;
- restartable transient resources.

Preview host never creates durable asset decisions or promoted generations.

## Shared Materialization

Preview and promotion share M047 candidate resolution, recipe semantics and deterministic materialization.

Preview may produce disposable media in session/cache space.

Promotion separately creates immutable project-local promoted generations after decisions exist.

## Raylib Boundary

Only the isolated Raylib adapter owns native graphics/audio resources.

The asset preview UI and M048 human-review content surface reuse one candidate-preview rendering/control implementation.

Do not create separate "milestone review" fake candidate media.

## Generic Human Review Boundary

The generic Review Workbench remains the repository-wide Restart/Reject/Accept shell.

M048 extends engineering/debug infrastructure with a bounded explicit registry that maps simple-review IDs to actual content experiences/readiness.

It does not add a plugin framework or durable review session.

M038 remains registered and behavior-compatible.

M048 registers its three actual asset-curation review experiences.
