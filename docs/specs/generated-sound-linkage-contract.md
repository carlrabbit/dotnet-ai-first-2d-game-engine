# Generated Sound Linkage Contract

## Authority

Authoritative for stable M027 linkage between synthesis definitions, generated WAV/provenance, ordinary sound definitions, export inclusion, diagnostics, inspection, and review packs.

## Stable schemas

```text
agentic2d.generated-sound-linkage.v1
agentic2d.generated-sound-provenance.v1
agentic2d.generated-sound-linkage-report.v1
agentic2d.generated-sound-review-pack.v1
```

## Authority boundary

```text
synthesis definition: generation source truth
generated WAV and provenance: derived artifact
ordinary sound definition: runtime authority
linkage: validation and inspection relationship
```

## Diagnostics

```text
SNDL001 generated output missing
SNDL002 output hash differs from provenance
SNDL003 sound definition references unknown output
SNDL004 generated output has no ordinary sound definition
SNDL005 duplicate incompatible linkage
SNDL006 stale synthesis-definition fingerprint
SNDL007 unknown synthesis definition
SNDL008 missing or malformed provenance
SNDL009 export omitted linked output
SNDL010 linkage schema/version unsupported
```

Each diagnostic identifies exact linkage field and related source, output, provenance, runtime definition, and export member.

## Export

Every generated output referenced by an included ordinary sound definition must be included in export. Missing inclusion fails validation.

## Review pack

Include manifest, index, linkage report, identities, hashes, rebuild status, export status, representative cue inventory, and omissions.
