# ADR-0032 — First Export Is a Self-Contained linux-x64 Directory

## Status

Proposed for Milestone 024. Accept when implementation is accepted.

## Context

The engine needs a runnable game export, but multi-platform distribution and publish-mode optimization would add premature complexity.

## Decision

The first supported export is Release, self-contained, `linux-x64`, directory-based, untrimmed, and not single-file.

It bundles managed runtime files, the current Linux raylib native dependency, selected game content, startup configuration, and an inspectable file inventory.

## Consequences

The export is larger than an optimized package but easy to inspect, validate, and run on the current development machine. Other platforms, trimming, single-file, installers, and release packages remain unsupported.
