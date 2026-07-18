# Generated Sound Linkage Contract

## Authority

Authoritative for explicit linkage between offline synthesis outputs and ordinary sound definitions, including validation, provenance, drift detection, and inspection.

```text
synthesis definition: generation source truth
generated WAV + provenance: derived asset
sound definition: runtime sound authority
```

Each linkage connects synthesis definition ID, expected output path/asset, provenance, output hash, sound-definition ID, and variant/raw-asset reference. Filename convention alone is insufficient.

Validation detects missing definition/output/provenance, stale definition fingerprint, output hash drift, wrong sound mapping, duplicate incompatible linkage, orphan output, and incompatible multiple claims.

Artifacts:

```text
generated-sound-linkage-report.json
generated-sound-linkage-report.md
```

Runtime cue selection and playback remain unchanged.
