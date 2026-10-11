# Asset Preview Host IPC Contract

## Authority

Authoritative for current M048 communication between the persistent asset workbench and the restartable local preview host.

The preview host is a thin actual-engine presentation client. It owns no durable workbench decision or promotion authority.

## Current Schemas

```text
agentic2d.asset-preview-ipc.hello.v2
agentic2d.asset-preview-ipc.request.v2
agentic2d.asset-preview-ipc.response.v2
agentic2d.asset-preview-ipc.event.v2
agentic2d.asset-preview-scene.v2
agentic2d.asset-preview-capture.v2
```

Supporting preview payload:

```text
agentic2d.asset-preview-subject.v1
agentic2d.asset-preview-bundle.v1
agentic2d.asset-preview-observation.v1
```

Historical v1 IPC/scene messages are not current M048 compatibility authority.

## Process Boundary

One normal preview host process remains associated with a workbench session and is independently restartable.

Workbench owns:

```text
session
aliases
input
curation draft
durable v2 decision intent/commit
preview acknowledgement tracking
```

Preview host owns:

```text
bundle loading/validation
temporary playback state
temporary comparison mode
temporary overlays
capture
health
shutdown
```

## Required v2 Request Semantics

Requests cover at least:

```text
load-subject
set-comparison-mode
set-overlay
animation-play
animation-pause
animation-step
animation-reset
audio-play-raw
audio-play-processed
audio-stop
capture
reset
health
shutdown
```

`load-subject` includes:

```text
session ID
request ID
preview bundle locator
preview bundle fingerprint
materializationSubjectFingerprint
media kind
```

Paths are operational and validated as local/session-bounded inputs; they are not semantic preview identity.

## Successful Load Response

A successful load response independently verifies the bundle and includes:

```text
request ID
status
materializationSubjectFingerprint
bundle fingerprint
raw media fingerprint
processed media fingerprint
projection/input fingerprint(s)
media kind
capabilities
```

The workbench may mark the draft preview-current only when the returned materialization-subject fingerprint exactly matches the current draft.

## Actual Engine Boundary

The host/client must use current backend-neutral engine presentation paths:

- rendering projection for image presentation;
- animation selection/sampling plus render projection for animation;
- sound projection/commands for audio.

Raylib remains the isolated native adapter.

No second image/audio transform engine is permitted in preview.

## Audio

Audio never auto-plays.

Audio output failure is diagnostic and does not terminate the workbench.

A human audio-quality review requires actual output capability even though machine validation also supports a safe no-device path.

## Restart

On restart/reconnect, no old process acknowledgement remains authoritative.

Workbench re-resolves/rebuilds the exact subject and sends a new v2 load.

## Diagnostics

Malformed bundle, hash mismatch, stale subject, invalid path, unsupported media, projection failure, or playback failure returns a stable structured diagnostic.

The host remains available where safe.

No diagnostic state may be treated as a current successful preview.

## Compatibility

M048 intentionally advances the local operational preview protocol to v2 to carry exact subject identity.

V1 requests may be rejected explicitly.

Do not translate a v1 request into v2 by guessing candidate/source/variant identity.
