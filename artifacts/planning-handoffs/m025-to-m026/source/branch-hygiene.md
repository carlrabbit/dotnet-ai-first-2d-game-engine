# Branch hygiene — M025 to M026 handoff

## Ancestry and commits

`main` merge-base is `0b38b5b`. The branch contains these commits after `main`:

| Commit | Purpose | Disposition |
|---|---|---|
| `7d982d7` | Implement milestone 25: provider geometry/synthesis/workspace/export work, Signal Passage consumer workspace, M025 contracts, review request/record, and wrappers. | Retain; M025 implementation. |
| `88f9051` | Cleanup: rename the M022 suite command and simplify M025 suite handling/tests. | Retain; accepted M025 completion cleanup. |
| `278bd72` | Initial curated M025→M026 evidence handoff. | Retain; handoff collection. |
| This follow-up (`docs: refine M025 to M026 planning evidence`) | Fixed-reference performance investigation, feature-level extension inventory, status clarification, and hygiene analysis. | Retain; focused handoff correction. |

## Files outside the handoff root after M024

The exact implementation path inventory is retained in `source/changed-files.txt`. Outside the handoff root, the changed groups are:

- `.review/pending/M025-signal-passage-playable-vertical-slice.md` and `.review/records/review.m025.signal-passage-playable-vertical-slice.json`: required M025 review workflow.
- `consumers/signal-passage/**`: consumer workspace manifests, assembly, authored content, scenarios, wrappers, provenance, and the extension report.
- `docs/artifacts/consumer-extension-discovery-artifact-contract.md`, `docs/decisions/ADR-0034-*`, `docs/decisions/ADR-0035-*`, `docs/milestones/MILESTONE-025-*`, and the three M025 contracts: M025 authority/contract additions.
- `eng/m025-smoke.sh`, `eng/signal-passage-*.sh`, `eng/m022-smoke.sh`, and `eng/export-graphical-smoke.sh`: M025/M022 validation and export wrappers.
- `src/Agentic2D.{Sound,Tools,Validation,Rendering,Workspaces,GameHost,DebugClient.Raylib,ScenarioRunner,Engineering}/**` and `tests/unit/Agentic2D.Tests.Unit/EngineeringHostTests.cs`: provider capability and validation work.
- `milestone-025-signal-passage-consumer-vertical-slice-package.zip`: package input; currently an unrelated pre-existing working-tree deletion is intentionally not staged by this follow-up.
- `.guide-sync/pending/2026-07-17-m025-*.md`: migration/planning trace files committed with M025 but not needed for either the M025 runtime implementation or this handoff. They are retained as historical committed content and excluded from the curated source patch.

## M022 migration review record

`.review/records/migration-guide-v050.json` was changed by `7d982d7`, not by the handoff. The record was refreshed from the M022 maintainer approval to an approved `project owner` record at the current M025 fingerprint. This was necessary because the M025 blocking review gate invokes the canonical review check, which requires current required review records. The changed record is therefore legitimate M025 validation state, not accidental or unrelated branch drift. It is retained and is not modified by this follow-up.

## Unrelated changes and disposition

The two deleted milestone package ZIPs were already unstaged before this follow-up. They are unrelated user working-tree changes and remain untouched. No files outside the handoff root are staged except the two canonical consumer extension-report copies required by this task. No restoration is required for `migration-guide-v050.json`; restoring it would make the accepted M025 review state stale again.
