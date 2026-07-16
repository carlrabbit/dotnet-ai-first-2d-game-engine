# ADR-0028 — Engineering Validation Uses Resumable Fingerprinted Suites

## Status

Accepted with Milestone 022.

## Context

The repository now has many aggregate milestone and regression commands. A single long process is not reliable evidence in agent environments with hard invocation lifetimes, process-group cleanup, or output limits.

## Decision

Aggregate suites that are not reliably bounded expose machine-readable plans, bounded shards, atomic fingerprinted receipts, and a fast verifier.

The verifier is aggregate success authority. Fingerprint, plan, receipt, and verification semantics live in tested .NET engineering code. Shell launchers only forward arguments and exit status.

## Consequences

Validation can resume across agent invocations and stale receipts cannot prove success. The repository accepts additional engineering-host code and generated receipt artifacts.

Backgrounding, `nohup`, `setsid`, larger shell timeouts, and partial child logs are not accepted substitutes.
