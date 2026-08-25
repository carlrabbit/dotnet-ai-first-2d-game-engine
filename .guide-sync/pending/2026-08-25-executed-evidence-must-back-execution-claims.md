# Deferred Guide Sync — Executed Evidence Must Back Execution Claims

## Source

M039 closure of the M031 simulation foundation in `carlrabbit/dotnet-ai-first-2d-game-engine`.

## Project conclusion

Artifact-first validation can become circular when an artifact producer writes a capability claim and a smoke command only checks that claim.

The project found concrete examples where generated evidence declared properties such as:

```text
freshProcessProof = true
classificationCompleteness = true
```

without the producing workflow actually observing the required process boundary or persistence-classification behavior.

M039 therefore treats execution-property evidence as valid only when the claim is derived from executed observations and, for aggregate validation, current fingerprinted validation receipts.

A "fresh process" claim specifically requires the runner to launch/observe separate process invocations. A scenario/artifact writer cannot establish that property by emitting a constant.

## Potential generic guide impact

During a future guide-system documentation pass, consider whether artifact-first validation guidance should explicitly distinguish:

- semantic/domain output produced by the test subject;
- execution provenance observed by the validation harness;
- aggregate success established by a current verifier.

Potential generic rule:

> A generated artifact must not be the sole authority for a claim about how that artifact was produced when the producer cannot itself observe the claimed execution property.

Examples include fresh-process, cross-platform, isolated-environment, crash-recovery, and similar execution-mode claims.

## Project-specific details not proposed as generic guide rules

Do not generalize:

- the M039 shard names;
- SimulationWorld persistence classifications;
- .NET process-runner mechanics;
- specific artifact file names;
- M031/M033 schema versions.

## Completion

Resolve this hint only when the external guide authority has explicitly considered evidence provenance for execution-property claims.

M039 implementation does not depend on this synchronization and ordinary implementation agents must not read this hint.
