# Exported Game Host Contract

## Authority

Authoritative for standalone executable startup, manifest loading, bundled-content resolution, normal graphical execution, headless validation execution, writable paths, metrics integration, diagnostics, and shutdown.

## Boundary

The game host is a minimal runtime host. It is not the product CLI, engineering host, debug shell, SDK, or source-workspace manager.

## Startup

The host:

1. locates the export root;
2. validates `agentic2d.export.json`;
3. resolves content using relative paths;
4. verifies project/content compatibility;
5. initializes existing engine services;
6. runs graphical mode by default or explicit headless mode;
7. resolves writable saves/diagnostics outside bundled content;
8. exits cleanly.

## Initial options

```text
--headless
--scenario <id>
--recording <path>
--ticks <count-or-final>
--metrics off|summary|per-tick
--output <path>
--help
--version
```

No export/build commands are exposed by the host.

## Determinism

Headless and graphical host modes use the same runtime authority. Adapter presence cannot change authoritative simulation results.
