# Constrained Validation Execution

## Authority

This document is authoritative for choosing direct versus resumable validation execution, defining validation-suite shards, writing receipts, computing fingerprints, and establishing aggregate success.

## Execution modes

Every aggregate validation suite declares one mode:

```text
direct
resumable-sharded
CI-only
human-review
```

`direct` is appropriate only when the complete command is reliably bounded in ordinary agent environments.

`resumable-sharded` is required when the suite can exceed an agent invocation lifetime, produces excessive output, or combines multiple independently meaningful validations.

## Resumable suite interface

A resumable suite exposes:

```text
./eng/<suite>.sh --list
./eng/<suite>.sh --plan-json
./eng/<suite>.sh --shard <id>
./eng/<suite>.sh --verify
./eng/<suite>.sh
```

On Windows, the native equivalent is `pwsh ./eng/suite.ps1 <suite-id> --plan-json`, `--shard <id>`, or `--verify`. M036 additionally exposes `pwsh ./eng/m036-smoke.ps1` over the same suite definition. Receipts retain structured host metadata; host identity is provenance, not semantic input.

No-argument mode may execute all shards and then verify in an unconstrained local or CI environment. It is not the required agent path.

## Agent path

In a constrained environment:

1. run `--plan-json`;
2. execute each required shard in a separate invocation;
3. stop on a real shard failure;
4. run `--verify`;
5. report the verifier result;
6. never infer aggregate success from partial output.

## Validation plan

`--plan-json` produces a machine-readable plan containing at least:

- schema and suite ID;
- suite fingerprint;
- execution mode;
- ordered required shard IDs;
- command for each shard;
- expected receipt path;
- dependencies between shards, if any;
- final verifier command;
- relevant artifact paths;
- current repository fingerprint.

The plan must be fast and must not run validation work.

## Receipt location

```text
artifacts/validation/<suite-id>/<shard-id>.json
```

Receipts are generated evidence and are ignored by Git.

## Atomic receipt rule

A passing receipt is written only after the shard command and all shard-specific evidence checks succeed:

```text
run shard
→ validate exit status and required evidence
→ write temporary receipt
→ flush and close
→ atomically move to final path
```

Interrupted, killed, timed-out, cancelled, or failed shards leave no valid passing receipt.

A new shard invocation removes or invalidates its old receipt before running.

## Receipt contract

A receipt includes at least:

- schema;
- suite ID and suite fingerprint;
- shard ID;
- status;
- repository fingerprint;
- command fingerprint;
- input fingerprint;
- result fingerprint when meaningful;
- command and normalized arguments;
- referenced artifact paths and fingerprints;
- completion metadata excluded from semantic success identity;
- diagnostics.

The verifier rejects malformed, failed, stale, foreign-suite, foreign-shard, or fingerprint-mismatched receipts.

## Fingerprints

The repository fingerprint includes:

- current HEAD commit;
- tracked working-tree changes;
- relevant untracked source/configuration/content files.

Exclude:

- `artifacts/`;
- validation receipts;
- `bin/` and `obj/`;
- temporary workspaces and fixtures;
- generated caches;
- platform timestamps and process identity.

Suite and command fingerprints include the suite definition, shard definition, invoked command, normalized arguments, relevant scripts/host binaries, and declared inputs.

Fingerprint and receipt semantics live in tested .NET code, not ad hoc shell parsing.

## Verification

`--verify` is fast and does not rerun tests or scenarios. It validates:

- the current plan;
- all required receipts are present;
- receipt status is passing;
- suite and shard identities match;
- repository, suite, command, and input fingerprints are current;
- referenced required artifacts exist and match recorded fingerprints;
- no required shard is missing or duplicated.

Only a successful verifier establishes aggregate success for a resumable suite.

## Explicit non-solutions

Do not use these as remedies for a hard agent execution lifetime:

```text
nohup
backgrounding
disown
setsid
larger shell timeouts
output redirection
heartbeat-only wrappers
claiming success from child-process logs
```
