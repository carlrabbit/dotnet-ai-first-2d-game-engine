# Asset Discovery and Campaign Review Pack Artifact Contract

## Authority

Authoritative for bounded M028 headless evidence covering discovery, cleanup, annotations, campaign reuse, and M029 readiness.

Schema: `agentic2d.asset-discovery-campaign-review-pack.v1`.

## Structure

```text
asset-review-pack/
├─ manifest.json
├─ index.md
├─ source/
├─ discovery/
├─ campaign/
├─ images/
├─ audio/
└─ diagnostics/
```

Manifest records source/profile IDs and fingerprints, annotation summary, campaign IDs, evidence paths, sizes, SHA-256 hashes, required/optional state, omissions, and pack fingerprint.

Required visual evidence:

```text
source-preview.png
indexed-contact-sheet.png
candidate-regions-overlay.png
duplicate-groups.png
animation-candidates.png
uncertainty-overlay.png
```

Required audio evidence when applicable:

```text
audio-properties.json
waveform-preview.png
raw-preview.wav
comparison-summary.json
```

Preview audio is bounded and never auto-played. The pack remains inspectable after copying away from the live asset home. It is not a portable source-profile bundle.
