# Regression summary

Historical M019-M024 verifiers were previously refreshed; M022 and M025 remain review-gated. Current M026 receipt fingerprints are stale after launcher/review changes; this handoff preserves them as historical evidence and does not rerun earlier milestones.

| Suite | Verifier | Status |
|---|---|---|
| M019 | `./eng/m019-smoke.sh --verify` | `error: m019-smoke/replay: invalid receipt (repository fingerprint)` |
| M020 | `./eng/m020-smoke.sh --verify` | `error: m020-smoke/review: invalid receipt (repository fingerprint)` |
| M021 | `./eng/m021-smoke.sh --verify` | `error: m021-smoke/review: invalid receipt (repository fingerprint)` |
| M022 | `./eng/m022-smoke.sh --verify` | `error: m022-smoke/platform-and-leakage: invalid receipt (repository fingerprint)` |
| M023 | `./eng/m023-smoke.sh --verify` | `error: m023-smoke/integrated: invalid receipt (repository fingerprint)` |
| M024 | `./eng/m024-smoke.sh --verify` | `error: m024-smoke/integrated: invalid receipt (repository fingerprint)` |
| M025 | `./eng/m025-smoke.sh --verify` | `error: m025-smoke/integrated: invalid receipt (repository fingerprint)` |
| M026 | `./eng/m026-smoke.sh --verify` | `error: m026-smoke/integrated: invalid receipt (repository fingerprint)` |
