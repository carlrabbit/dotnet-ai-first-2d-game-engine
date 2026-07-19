# Consumer Authoring Review Pack Artifact Contract

## Authority

Authoritative for bounded durable evidence used by milestone-owned human review of consumer-authoring capabilities.

## Structure

```text
review-pack/
├─ manifest.json
├─ index.md
├─ geometry/
├─ sound-linkage/
├─ scenarios/
├─ persistence/
├─ performance/
└─ captures/
```

Manifest schema: `agentic2d.consumer-authoring-review-pack.v1`.

Required fields: owning milestone, canonical review ID, source revision, artifact versions, evidence entries, paths, sizes, SHA-256 hashes, required/optional classification, capture status, omissions, and pack fingerprint.

Include representative evidence sufficient for one milestone decision. Exclude complete exports, build outputs, repeated logs, every animation frame, unbounded streams, and unrelated historical evidence.

Unavailable graphical evidence is explicit. A later milestone creates a new request and pack rather than reopening this one.
