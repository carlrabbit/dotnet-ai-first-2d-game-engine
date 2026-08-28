# Canonical Asset Candidate Contract

## Authority

Authoritative for the exact game-local candidate descriptor that bridges M028 shared discovery/campaign proposals to M047 decisions and promotion.

The shared asset home remains authoring infrastructure. Raw source bytes remain source truth. A canonical candidate is game-local promotion input, not gameplay authority.

## Schema

```text
agentic2d.canonical-asset-candidate.v1
agentic2d.canonical-asset-variant.v1
```

## Candidate Shape

A promotable candidate identifies at least:

```text
campaignId
candidateId
sourceId
source-relative file path
exact source-file SHA-256
media kind
normalized source selection
promotion-relevant presentation role/proposal
typed variants
candidate fingerprint
```

Absolute source paths may be used operationally to read bytes but never participate in candidate semantic identity or fingerprint.

`candidateId` is opaque stable identity. Its spelling is not a media-type, consequence, gameplay, file-selection, or processing contract.

## Media Kind

Current kinds are structured values:

```text
image
animation
audio
```

They derive from validated discovery/campaign authority. Do not infer them from candidate-ID substrings.

## Source Selection

M047 canonical selections are:

```text
image-file
image-region
animation-sequence
audio-file
```

`image-region` references one exact image file plus normalized integer pixel bounds inside the validated source dimensions.

`animation-sequence` references an ordered non-empty sequence of exact validated image regions. Frame order is explicit authority, not filesystem order.

`audio-file` references one exact validated WAV/PCM source file. Sample-frame trim belongs to the processing recipe/decision correction rather than the base source selection.

Every referenced source file/region must exist in the referenced discovery authority and resolve to the exact recorded source-file hash.

## Variants

A candidate may expose typed alternatives. Each variant has stable `variantId`, normalized structured deltas to the base candidate/proposal/recipe inputs, and a `variantFingerprint`.

A variant cannot be an opaque note that promotion must interpret heuristically.

Selecting a variant does not change the candidate's logical `candidateId`; it changes the exact revision/materialization subject bound by the decision.

## Fingerprint

The canonical candidate fingerprint covers all promotion-relevant semantic input, including:

```text
candidate schema/version
campaignId
candidateId
sourceId
source-relative file path
exact source-file SHA-256
media kind
normalized selection
promotion-relevant presentation role/proposal
ordered typed variants and their normalized semantic content
```

It excludes:

```text
absolute asset-home path
timestamps
process/session IDs
workbench aliases
partial input/focus state
source enumeration order
non-semantic display-only operational metadata
```

Canonical ordering is ordinal and deterministic.

A profile/campaign container fingerprint is provenance/context, not by itself a candidate-staleness predicate. Rebuilding or editing unrelated profile/campaign content does not stale an exact candidate when the resolver reproduces the same candidate/variant fingerprints and referenced source-file hash.

## Current Resolution

A decision is current only if the resolver can reproduce its exact candidate fingerprint from current accessible source/profile/campaign authority.

The candidate is stale/non-promotable when any relevant fact is unavailable or differs, including:

- source unavailable;
- referenced file missing;
- referenced file SHA-256 differs;
- required discovery observation/region/frame reference missing or invalid;
- candidate removed;
- media kind changed;
- normalized selection changed;
- promotion-relevant proposal/role changed;
- selected variant missing or fingerprint changed.

Never silently retarget to another file, region, variant, or current first match.

## Determinism Cases

Current validation must prove at least:

1. two image candidates in different files;
2. two regions in one atlas;
3. an explicit animation frame sequence;
4. an audio candidate;
5. reordered source inventory;
6. same source copied to another absolute asset-home root;
7. changed unrelated profile/campaign entry;
8. changed referenced source bytes;
9. removed/unavailable source;
10. deliberately misleading candidate IDs such as an image candidate whose ID contains `audio` and an audio candidate whose ID contains `animation`.

The structured descriptor controls; the ID text does not.
