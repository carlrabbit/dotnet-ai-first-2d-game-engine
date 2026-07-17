# Prompt — Execute Planned Milestone

You are the implementation agent for this repository.

A planning/documentation agent has already created an implementation-ready milestone package. Do not redo planning.

Unpack or use the provided package, then start with:

```text
{primary-milestone-path}
```

## Required reading

Read only:

1. the primary milestone document;
2. the authority documents explicitly listed in that milestone;
3. source and test files needed for the focus areas.

Do not read the external guide repository.

Do not treat old copied setup or engineering guides as authority.

Do not read `.guide-profile.json`, `.guide-sync/`, or `.review/` unless the milestone explicitly requires them.

## Execution rules

Implement only the milestone scope and focus areas.

Do not broaden scope.

Do not redo architecture or planning decisions already made authoritative by the package.

Do not perform broad documentation synchronization.

Update direct documentation only when the milestone says it is required for implementation correctness.

Use canonical repository commands from `eng/`.

## Human review

When the milestone requires human review:

- create or update the review request under the specified `.review/` path;
- produce the required evidence;
- do not fabricate approval;
- do not create an approval record on behalf of a human reviewer;
- treat blocking review as incomplete until the required record exists;
- report review status clearly.

## Constrained execution

Assume the command-execution harness may terminate long-running process groups.

Do not attempt to escape the harness with `nohup`, backgrounding, `disown`, `setsid`, shell redirection, or longer shell-level timeouts.

When a validation suite exposes `--plan-json`:

1. run the plan command;
2. execute each required shard in a separate invocation;
3. stop and report an actual shard failure;
4. permit retries only where the suite contract allows them;
5. run the fast `--verify` command;
6. treat the verifier as aggregate success authority.

Do not claim aggregate success from partial child output.

## Validation

Run the validation tier and concrete commands specified in the milestone.

If the milestone specifies CI-only validation, report local validation separately and do not claim CI success before it runs.

## Completion report

When finished, report:

- implemented focus areas;
- files changed;
- validation commands run and results;
- resumable shard and verifier results, if applicable;
- direct documentation changes;
- `.guide-sync/pending/` hints created or updated, if explicitly required;
- human-review requests, evidence, and current status;
- blockers or deviations from the milestone.
