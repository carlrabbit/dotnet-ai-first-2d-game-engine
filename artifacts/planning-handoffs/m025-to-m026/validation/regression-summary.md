# Regression verifier summary

Collection observes the current worktree, which differs from the reviewed M025 fingerprint. Statuses below are verifier results only; child logs are not treated as aggregate success.

| Suite | Revision/fingerprint context | Verifier command | Status | Receipt count | Failure or waiver |
|---|---|---|---|---:|---|
| M019 | current collection worktree | `./eng/m019-smoke.sh --verify` | not current | 5 | stale: all 5 receipts report repository fingerprint mismatch |
| M020 | current collection worktree | `./eng/m020-smoke.sh --verify` | not current | 7 | stale: all 7 receipts report repository fingerprint mismatch |
| M021 | current collection worktree | `./eng/m021-smoke.sh --verify` | not current | 9 | stale: all 9 receipts report repository fingerprint mismatch |
| M022 | current collection worktree | `./eng/m022-smoke.sh --verify` | not current | 0 | waived/unavailable: 6 required receipts are missing |
| M023 | current collection worktree | `./eng/m023-smoke.sh --verify` | not current | 6 | stale: receipts report suite and repository fingerprint mismatch; guide-v051 receipt absent |
| M024 | current collection worktree | `./eng/m024-smoke.sh --verify` | not current | 8 | stale: receipts report repository mismatch; game-host also input/evidence mismatch |
