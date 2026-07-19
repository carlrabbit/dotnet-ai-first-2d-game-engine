# Reusable Asset Discovery Profile Contract

## Authority

Authoritative for deterministic reusable observations derived from raw image and audio sources.

## Truth boundary

```text
raw bytes: source truth
discovery profile: observed facts and low-interpretation proposals
campaign: game-specific relevance and proposals
approved game content: future authoritative presentation semantics
```

A discovery profile never approves gameplay or game-specific meaning.

## Schemas

```text
agentic2d.asset-source.v1
agentic2d.asset-source-file.v1
agentic2d.asset-discovery-profile.v1
agentic2d.asset-image-observation.v1
agentic2d.asset-audio-observation.v1
agentic2d.asset-region-candidate.v1
agentic2d.asset-duplicate-group.v1
agentic2d.asset-animation-candidate.v1
agentic2d.asset-license-observation.v1
```

## Required files

```text
source-profile.json
source-files.jsonl
image-observations.jsonl
audio-observations.jsonl
region-candidates.jsonl
duplicate-groups.json
animation-candidates.json
license-observations.json
discovery-diagnostics.json
```

## Image baseline

Bounded PNG support: dimensions, alpha bounds, exact grid candidates, irregular regions, transparent padding, deterministic duplicates, conservative sequence proposals, and contact-sheet/overlay inputs. No player/wall/hazard/collectible/progression approval.

## Audio baseline

Bounded WAV support: duration, sample rate, channels, sample format, peak/summary observations, exact duplicates, conservative family proposals, and waveform/preview inputs. No gameplay cue approval.

## Determinism and compatibility

Order by source-relative path and canonical candidate identity. Exclude absolute paths, timestamps, and non-semantic encoding metadata from semantic fingerprints. Generated profiles are rebuildable; incompatible generated schemas may be cleaned rather than migrated. Retained annotations apply separately and may diagnose incompatibility.
