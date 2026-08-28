# Approved Asset and Deterministic Promotion Contract

## Authority

Authoritative for current approved presentation assets, deterministic processing recipes, promotion plans/manifests, immutable promoted generations, atomic publication, provenance, and approved-content validation.

Real dependency-aware affected rebuild is explicitly deferred to M049 and is not current M047 capability.

## Current Schemas

```text
agentic2d.approved-asset.v2
agentic2d.approved-image-region.v2
agentic2d.approved-sprite.v2
agentic2d.approved-animation.v2
agentic2d.approved-audio.v2
agentic2d.approved-presentation-role.v2
agentic2d.asset-processing-recipe.v2
agentic2d.asset-promotion-plan.v2
agentic2d.asset-promotion-manifest.v2
agentic2d.asset-provenance.v2
agentic2d.asset-promoted-generation.v1
agentic2d.asset-current-generation.v1
agentic2d.asset-approved-validation.v2
agentic2d.asset-future-binding-suggestion.v1
```

Historical v1 approved/promotion schemas may be diagnosed/read for history but are not current trusted M047 promoted authority.

Approved assets are project-local presentation authority. Future-binding suggestions are not gameplay authority.

## Promotion Inputs

A promotable item requires:

```text
current canonical candidate
current effective v2 decision for exact candidate/variant
supported typed corrections
versioned deterministic recipe
exact accessible source bytes matching recorded SHA-256
```

Any mismatch or unsupported operation blocks the item/promotion. Do not substitute a same-kind file, first matching file, current nearest candidate, or filename heuristic.

## Stable Approved Identity

The stable logical identity tuple is:

```text
campaignId
candidateId
approved asset kind
presentation role
```

Approved ID is deterministically derived from that tuple using full SHA-256 canonical identity material or an equivalently complete collision-detectable digest representation.

Candidate fingerprint, selected-variant fingerprint, recipe fingerprint, source hash, and derivative hash are revision/provenance and do not change the stable approved identity for the same logical tuple.

If one approved ID resolves to two different identity tuples, validation/promotion fails. Truncated display IDs must never hide a collision.

## Processing Recipes

Every materialized derivative has one explicit recipe with stable operation IDs/versions, ordered parameters, exact input hashes, and deterministic recipe fingerprint.

M047 supported operations:

```text
copy-source
crop-image-region
preserve-padding
trim-transparent-padding-to-alpha-bounds
scale-image-nearest-integer
set-pivot-or-anchor-metadata
order-animation-frames
audio-copy
audio-trim-sample-frames
```

Rules:

- image crop uses integer pixel bounds inside the validated source image;
- alpha-bounds trim uses the deterministic discovery alpha policy/current validated alpha bounds;
- nearest-neighbor scale uses a positive integer factor and exact source pixels;
- pivot/anchor is metadata and does not silently modify source pixels;
- animation frame order is an explicit ordered reference list;
- audio trim uses integer PCM sample-frame start/end boundaries and preserves the supported deterministic WAV/PCM encoding unless a later contract explicitly adds conversion.

Unsupported transforms fail. No free-text correction is executable processing authority.

## Derivative and Provenance Linkage

Each derivative/approved definition records or resolves:

```text
approved stable ID/identity
sourceId
source-relative source file
exact source-file SHA-256
canonical candidate ID + fingerprint
selected variant ID + fingerprint when applicable
v2 decision ID + decision fingerprint
processing recipe ID/version/fingerprint
project-relative derivative path
exact derivative SHA-256
promoted generation fingerprint
```

Absolute source paths are operational only and never promoted semantic authority.

## Promotion Plan

The v2 plan is deterministic and contains only current promotable effective decisions.

It contains enough normalized structured information to reproduce materialization without consulting workbench aliases, text buffers, UI state, or candidate-ID heuristics.

Plan generation fails or reports explicit non-promotable items when required current source/candidate/variant/decision/recipe authority is missing.

## Immutable Generation

Promotion materializes a new immutable generation outside current authority.

The generation contains all approved definitions, derivatives, recipes/provenance needed by its manifest. It is self-validating using project-relative references and content hashes and has no runtime dependency on the shared asset home after materialization.

The generation fingerprint is independently reproducible from canonical generation semantic content and derivative hashes. It excludes absolute paths, timestamps, process IDs, random staging paths, and current-pointer location.

Once published, a generation is not mutated in place.

## Atomic Current Authority

One small current-generation authority record identifies the current immutable generation.

Publication sequence:

```text
materialize staged generation
→ validate staged generation independently
→ derive/recheck generation fingerprint
→ make immutable generation addressable
→ atomically replace current-generation authority record
→ read back current authority
→ independently validate resolved generation identity/content
→ success
```

A failure before current-authority replacement leaves the previous generation current.

A failure after a completed current-authority replacement may leave the new generation current, but it must be complete and previously validated. No partial generation may become current.

A whole-directory move/remove window is not sufficient as the sole current-authority mechanism.

## Independent Validation

`asset approved validate` resolves the current-generation authority and derives validity from observed files/content.

It verifies at least:

```text
current authority schema/reference
generation existence and immutability expectations
generation fingerprint
approved definition schemas
stable identity uniqueness/collision
safe project-relative paths
derivative existence and hashes
source provenance fields/hashes
current v2 decision exact candidate/variant binding
supported recipe IDs/versions/parameters/fingerprint
internal reference closure
absence of absolute local/asset-home paths
absence of workbench alias/session/input authority
canonical serialization where required
```

It must fail manipulated/corrupt content even if the producer wrote `status=passed`, `atomic=true`, `containsAliases=false`, `containsAbsolutePath=false`, or equivalent claims.

Legacy v1 workspaces are not accepted as current M047 authority; return an explicit legacy/untrusted diagnostic.

## Deterministic Serialization

Canonical promoted semantic JSON is deterministic:

```text
UTF-8
no BOM
LF line endings
ordinal stable collection ordering
stable normalized numeric/string representation under repository conventions
```

Equivalent semantic inputs produce byte-identical canonical promoted semantic files and derivative bytes.

Operational command result wrappers outside the immutable promoted generation may contain timestamps/environment data only when they do not participate in promotion identity or current authority.

## Path Independence

Relocating the same source corpus/asset home to another absolute path must not change candidate fingerprint, plan semantics, recipe fingerprint, derivative bytes, approved stable IDs, generation fingerprint, or canonical promoted semantic bytes.

Source-relative paths remain provenance. Absolute paths do not.

## Failure and Recovery Proof

Validation uses deterministic fault injection at bounded promotion boundaries, including at least:

```text
after plan
mid derivative materialization
after derivative materialization/before full manifest
before staged validation completion
after staged validation/before current-authority switch
immediately after current-authority switch/before success result
```

Every case must prove that current authority resolves to either the prior valid generation or the complete validated new generation, never partial content.

## Affected Rebuild Boundary

M047 does not claim dependency-aware affected rebuild.

Until M049 implements real dependency closure and rebuild, `asset rebuild --affected` must not return success or emit success-looking claims that dependencies were rebuilt/unchanged.

It may return a stable unsupported/deferred diagnostic. Removing the command is permissible only if current product-CLI compatibility authority permits it.
