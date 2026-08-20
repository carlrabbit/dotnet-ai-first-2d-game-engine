# User Settings and Display Safety Contract

## Authority

Authoritative for M037 mutable user configuration, persistence, migration, display preview/rollback, and safe mode.

## Boundary

User settings are local application configuration outside world authority. World generation/simulation configuration is not a user setting.

## Required settings

Audio: master, music, effects, optional mute.

Display: windowed, borderless-windowed, fullscreen, native/desktop resolution, supported common resolutions, UI scale.

General: autosave enabled/interval/retention, pause on focus loss, pause on configured critical event/alert, supported camera/edge-scroll/accessibility preferences.

Input: user binding overrides keyed by registered action ID.

## Persistence

Settings have schema/version, validation, bounded migration, atomic replacement, rejected-input backup, and per-setting fallback where safe. Corruption must not prevent startup.

## Display preview

```text
record previous known-good
-> persist provisional marker
-> apply candidate
-> countdown
-> keep or revert
```

Default countdown: 15 seconds. Timeout, failed apply, or process termination causes rollback. Known-good state survives restart.

## Safe mode

Required options:

```text
--safe-mode
--reset-user-settings
```

Safe mode applies safe windowed display, supported resolution, default UI scale/theme/bindings, and optional audio disable. It never changes saves or world configurations.

## Cross-platform

Settings schema is shared. Display adapters expose host-supported modes. Unsupported modes cannot become known-good. A settings file from one platform remains parseable on the other even when display fallback is required.
