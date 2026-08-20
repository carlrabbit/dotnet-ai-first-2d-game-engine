# Player and Diagnostics Application Architecture

## Purpose

Define M037 application/UI composition and dependency direction.

## Shape

```text
backend-neutral application foundation
├── retained UI tree/layout/focus/projection
├── settings/display safety
├── save catalog/autosave
├── input registry/context routing
├── world-configuration access
├── rendering/audio adapter interfaces
└── world lifecycle services
        │
        ├── player composition
        │     menus/options/save browser/game operations
        │
        └── diagnostics composition
              runtime health/queues/activities/artifacts/fault/repro tools
```

## Dependency rules

- player composition does not reference diagnostics composition;
- shared application/UI foundation does not reference raylib-cs;
- native adapters translate UI/render/audio/input boundaries;
- simulation does not reference application screens;
- UI consumes projections and issues explicit commands;
- settings/autosave wall time do not become simulation authority.

## World replacement

```text
confirm operation
-> optional save
-> stop input/world advancement ownership
-> detach projections/callbacks
-> dispose old world/native resources
-> load/validate candidate
-> transfer ownership atomically
-> rebuild player screens/projections
```

## Diagnostics preservation

Engineering diagnostics remain available and may share controls/themes, but diagnostics services are composed only for diagnostics.

## UI strategy

Small retained tree, explicit state refresh/callbacks, container layout, backend-neutral projection, native adapter rendering, no data binding.

## Platform strategy

The same application/UI contracts run on Linux and Windows. Native display/input/audio differences are adapter concerns proven by platform-specific graphical shards.
