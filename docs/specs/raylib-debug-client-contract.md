# raylib-cs Debug Client Contract

## Authority

Authoritative for the isolated graphical client, native resources, live/snapshot modes, debug controls, viewport presentation, and explicit screenshots.

## Dependency boundary

Only `src/Agentic2D.DebugClient.Raylib` references raylib-cs. Planning baseline: `Raylib-cs 8.0.0`; implementation verifies and pins the exact version. Core/runtime/rendering contracts contain no raylib types.

## Lifecycle

All raylib calls execute on the owning client thread. Lifecycle: initialize, load/cache, run/draw, unload, close. Cleanup occurs on failure.

## Presentation

Logical viewport `320 × 180`, render texture, largest fitting integer scale, letterboxing, point filtering, no interpolation.

## Texture cache

Key by stable asset ID plus source fingerprint. Load once, reuse atlas regions, provide deterministic fallback/diagnostics, unload explicitly.

## Modes

Live scenario supports runtime run/pause/step/reset APIs. Snapshot mode renders recorded state without simulation. Both use the same projector/compiler.

## Controls

`Space` pause/resume; `Period` step one; `Shift+Period` step ten; `R` reset; `F1` overlays; `Tab`/`Shift+Tab` entity cycle; arrows pan; mouse wheel or `+/-` zoom; `F12` screenshot; `Escape` close. No mouse selection.

Scenario completion pauses without implicit reset.

## Screenshots

Only `F12` or explicit `--capture`. No automatic capture. Metadata uses source/tick/projection identity and capture sequence, not wall-clock semantic identity.

## Graphics smoke

Requires a documented graphics-capable environment and proves context creation, real PNG load, texture reuse, one frame draw, clean shutdown, and optional explicit capture. Headless projection validation remains separate and mandatory.
