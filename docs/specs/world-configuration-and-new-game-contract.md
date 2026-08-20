# World Configuration and New Game Contract

## Authority

Authoritative for M037 authored world-configuration resources, preset identity, seed selection, world title, and fixed tutorial entry.

## World configuration

A world configuration is documented validated JSON content in the game resource area. It contains only generation/fixed-simulation values already supported by current gameplay authority.

Required metadata: configuration ID, schema version, display name, description, effective values, content fingerprint, compatibility constraints.

Bundled identities: relaxed, standard, demanding, stress-test. These are configurations, not scenarios.

## Immutability

A created world retains the canonical effective configuration and fingerprint. Later resource edits do not silently change an existing world. M037 provides no world-rule editor or runtime fixed-rule mutation.

## New Game

Inputs: configuration, generated/entered seed, pre-filled editable world title. Validate before world ownership commits.

## Tutorial

Uses fixed registered seed + fixed standard configuration + tutorial-guidance marker. Creates an endless world. M037 does not define objectives, victory, scenario completion, or full tutorial state machine.

## Validation

Reject unsupported schema, duplicate IDs, invalid ranges, unsupported values, unstable canonicalization, missing tutorial config, and invalid seeds.
