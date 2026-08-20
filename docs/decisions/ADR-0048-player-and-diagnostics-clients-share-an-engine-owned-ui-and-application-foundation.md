# ADR-0048 — Player and Diagnostics Clients Share an Engine-Owned UI and Application Foundation

## Status

Accepted for M037 implementation.

## Context

The current Raylib client is primarily an engineering/debug surface. M037 needs a player-facing shell, reusable menus/dialogs, settings, saves, and input rebinding without leaking diagnostics concepts into ordinary play.

## Decision

Create an engine-owned retained UI toolkit and shared application foundation. Compose distinct player and diagnostics clients over that foundation. Separate executables are preferred; explicit modes are temporarily acceptable if dependency isolation is preserved.

Use explicit state refresh and callbacks. Do not add data binding.

## Consequences

Positive: clean player terminology, preserved diagnostics power, headless-testable UI, reusable later presentation shell, reusable save/settings/input services, one semantic application model across Linux/Windows.

Negative: substantial application/UI infrastructure, explicit native resource/callback ownership, and a bounded engine-owned UI toolkit to maintain.

## Rejected alternatives

Extending debug panels directly into the player product is rejected because diagnostics/player concerns remain coupled.

A large data-binding/reactive UI framework is rejected because the game needs bounded UI infrastructure rather than a general application framework.

UI state as simulation authority is rejected because headless operation, saves, determinism, and cross-platform validation must remain independent.
