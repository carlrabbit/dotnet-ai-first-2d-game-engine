# Asset Preview Host IPC Contract

## Authority

Authoritative for communication between the M029 workbench and restartable preview host.

The host is a thin actual-engine client and owns no durable decisions or workbench input state.

## Schemas

```text
agentic2d.asset-preview-ipc.hello.v1
agentic2d.asset-preview-ipc.request.v1
agentic2d.asset-preview-ipc.response.v1
agentic2d.asset-preview-ipc.event.v1
agentic2d.asset-preview-scene.v1
agentic2d.asset-preview-capture.v1
```

Requests cover candidate/comparison loading, backgrounds, overlays, animation/audio controls, capture, reset, health, and shutdown.

Messages are versioned, bounded, session/request identified, capability-negotiated, path validated, and diagnostically structured. Audio never auto-plays. Malformed assets do not terminate the session. Restart/reconnect is supported.
