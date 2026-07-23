# Internal Testing Runbook

## Purpose

Operational project truth for running heavy internal tests after M035 implementation.

M035 supports a five-region settlement with 50 workers, at least 1,000 authoritative entities, 150 infrastructure/planning entities, 500 work opportunities, 100 active activities/reservations, and a 10,000-entry queue stress fixture. Exactly one region is detailed. This is a bounded internal-testing envelope, not a universal hardware claim.

## Host and graphics prerequisites

Use Linux, Bash, Git, and the tested .NET SDK. Headless structural checks are supported everywhere on that host. The four-hour graphical soak additionally requires a verified Raylib-capable supervised session that explicitly sets `M035_GRAPHICS_CAPABLE=1`; a display variable alone is not sufficient evidence. It is intentionally not emulated by a headless skip. The graphical adapter displays a live five-region `SimulationWorld` fixture: its simulated instant, world fingerprint, event count, detailed-region ownership, and resource cycle must change while it is running. The supplied M034 operations dashboard is validated as launch input, but is not treated as mutable graphical authority.

## Start, inspect, and save a reference session

Generate the reference evidence and saves:

```bash
dotnet run --project src/Agentic2D.Tools -- simulation m035-readiness --output artifacts/readiness/M035
```

Reference saves are listed in `artifacts/readiness/M035/reference-save-manifest.json`; the generated save files are beneath `artifacts/readiness/M035/reference-saves/`. Use checkpoint or continuous monitoring through the session manifest at `artifacts/readiness/M035/tester-session-index.json`. Health, activity, reservation, trigger, alert, queue, transition, and causal-history evidence is indexed from the readiness root rather than requiring source inspection.

Validate compatibility and recovery with:

```bash
./eng/save-compatibility-smoke.sh
./eng/save-recovery-smoke.sh
```

Recovery preserves `<save>.previous-good` and never replaces the only known-good save without explicit recovery authority.

## Tester sessions and defect reporting

Give each test session a stable ID, record the generated build fingerprint, seed, diagnostics mode, scenario/save, start/end, and operator notes in the session manifest. On a failure, retain the bounded causal window and capture the bundle identified by `reproduction-bundle-index.json`. Every bundle records a repository-relative run command such as:

```bash
dotnet run --project src/Agentic2D.Tools -- simulation m035-readiness --mode fault --output artifacts/readiness/M035
```

Use a bundle without source inspection:

```bash
dotnet run --project src/Agentic2D.Tools -- simulation m035-repro inspect --bundle artifacts/readiness/M035/reproductions/fault.command-before-commit --output artifacts/readiness/M035/repro-inspect
dotnet run --project src/Agentic2D.Tools -- simulation m035-repro verify --bundle artifacts/readiness/M035/reproductions/fault.command-before-commit --output artifacts/readiness/M035/repro-verify
dotnet run --project src/Agentic2D.Tools -- simulation m035-repro run --bundle artifacts/readiness/M035/reproductions/fault.command-before-commit --output artifacts/readiness/M035/repro-run
dotnet run --project src/Agentic2D.Tools -- simulation m035-repro reduce --bundle artifacts/readiness/M035/reproductions/fault.command-before-commit --output artifacts/readiness/M035/repro-reduce
```

Do not add absolute personal paths, secrets, native dumps, or unbounded logs. Report the stable diagnostic/failure signature, affected IDs, simulation instant, seed, bundle ID, and expected versus observed result.

## Campaigns and readiness gate

Run focused structural checks before the aggregate suite:

```bash
./eng/performance-budget-smoke.sh
./eng/runtime-health-smoke.sh
./eng/deadlock-detection-smoke.sh
./eng/fault-injection-smoke.sh
./eng/reproduction-bundle-smoke.sh
./eng/internal-test-session-smoke.sh
```

For the resumable suite, run `./eng/m035-smoke.sh --plan-json`, each listed `--shard <id>` in a separate foreground invocation, and then `./eng/m035-smoke.sh --verify`. The nested campaign verifier and the parent verifier are both required. Run the graphical command only in the supported graphical session:

```bash
./eng/m035-graphical-soak-smoke.sh
```

The graphical path measures a full 14,400 seconds; setting a shorter duration results in `failed-duration-too-short`, not a pass. During the supervised session the operator must record pause/resume, speed increase and decrease, detailed-region switch, save, load, and diagnostics-overlay actions. The session report rejects a duration-only unattended window because it cannot prove the required interactive workflow.

If a completed four-hour session has valid live-progress evidence but missed the controls, retain it and add a linked supervised workflow continuation instead of repeating the four-hour duration:

```bash
M035_TESTER_SESSION_ID=session.m035.operator-workflow \
dotnet run --project src/Agentic2D.DebugClient.Raylib -- m035 \
  --input artifacts/simulation/M034/world-dashboard.json \
  --duration-seconds 60 --workflow-only \
  --continuation-session artifacts/readiness/M035/graphical-soak/session.json \
  --capture artifacts/readiness/M035/graphical-soak/operator-workflow.png \
  --output artifacts/readiness/M035/graphical-soak/operator-workflow.json
```

Press each required control once during that short window. Pause and Resume are separate controls and turn green independently. You may use the on-screen mouse buttons—Pause, Resume, Faster, Slower, Switch region, Save, Load, Diagnostics—instead of the keyboard. For RDP/touch clients, hold the pointer over each button for 0.8 seconds until its yellow progress bar fills; this activates the control without a click. The readiness writer accepts it only when its continuation fingerprint matches the completed primary session.

## Blocking defects and evidence limits

Blocking defects include corruption or failed safe recovery, resource duplication/loss, reservation/executor leaks, unexplained deterministic replay divergence, unreproducible crashes, persistent supported-envelope deadlock/livelock, incomplete required campaigns, or an operations surface that cannot expose the cause of a critical failure. Bounded retention is 64 health/log entries, 512 journal entries, and 128 generated-artifact entries; truncation is explicit and cannot represent a complete campaign.

## Required sections

1. Supported envelope and known limitations.
2. Host/runtime and graphics prerequisites.
3. Starting the reference settlement.
4. Loading retained reference saves.
5. Starting and finishing a tester session.
6. Enabling checkpoint or continuous health monitoring.
7. Using region, activity, reservation, trigger, alert, and event inspection.
8. Saving, validating, and recovering worlds.
9. Capturing a reproduction bundle.
10. Running and verifying a reproduction bundle.
11. Running focused stress campaigns.
12. Running the 365-day headless soak.
13. Running the four-hour graphical soak.
14. Locating receipts, readiness reports, and review evidence.
15. Blocking defect criteria and reporting format.
16. Sanitization and evidence-size rules.

## Blocking defect categories

At minimum:

- data corruption or failed safe recovery;
- unexplained resource duplication/loss;
- reservation or executor ownership leak;
- deterministic replay divergence without diagnosis;
- unreproducible crash in the supported envelope;
- persistent deadlock/livelock inside supported normal play;
- required campaign/soak cannot complete;
- operations UI cannot expose the cause of a critical supported failure.

## Authority boundary

This runbook references repository commands and project documents only. It must not reference external guide documents as operational authority.
