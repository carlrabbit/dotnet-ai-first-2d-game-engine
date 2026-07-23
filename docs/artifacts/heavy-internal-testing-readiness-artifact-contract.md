# Heavy Internal Testing Readiness Artifact Contract

## Root

```text
artifacts/readiness/M035/
```

## Required artifacts

```text
m035-manifest.json
support-envelope.json
performance-budgets.json
performance-baseline.json
performance-comparison.json
runtime-health-summary.json
invariant-violations.jsonl
deadlock-livelock-report.json
fault-campaign-report.json
save-compatibility-matrix.json
save-recovery-report.json
reference-save-manifest.json
reproduction-bundle-index.json
tester-session-index.json
headless-soak-report.json
graphical-soak-report.json
memory-throughput-trends.json
optimization-dispositions.json
blocking-defects.json
known-limitations.json
readiness-report.json
diagnostics.json
review-pack/
  review-manifest.json
  evidence-index.json
  support-envelope-summary.md
  campaign-summary.md
  compatibility-and-recovery.md
  performance-and-trends.md
  tester-workflow-review.md
  graphical-soak-index.md
  blocking-defect-disposition.md
```

## Required schemas

```text
agentic2d.m035.support-envelope.v1
agentic2d.performance-budget.v1
agentic2d.performance-comparison.v1
agentic2d.runtime-health-summary.v1
agentic2d.stress-campaign.v1
agentic2d.fault-campaign.v1
agentic2d.save-compatibility-matrix.v1
agentic2d.reproduction-bundle-index.v1
agentic2d.tester-session.v1
agentic2d.soak-report.v1
agentic2d.readiness-report.v1
agentic2d.m035.review-pack.v1
```

## Readiness report

Contains:

- support envelope;
- aggregate verification reference;
- blocking and advisory budget status;
- campaign completion matrix;
- invariant and fault results;
- compatibility/recovery status;
- reproduction coverage;
- unresolved defects;
- declared limitations;
- decision;
- review reference.

## Trend evidence

Memory, queue, journal, artifact, projection, and throughput trends include sample intervals and bounded regression analysis.

## Failure evidence

Every blocking failure references a reproduction bundle or explicitly records why process termination prevented capture and what fallback evidence exists.

## Boundedness

Artifacts declare retention, truncation, and maximum-size policy. Partial/truncated evidence cannot be represented as complete.

## Verification

Fail on missing campaigns, early termination, stale receipts, failed invariant, unsupported save regression, missing reproduction coverage, absent graphical soak, unresolved blocking defect, invalid readiness decision, or pending review.
