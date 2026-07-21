# Asset Workbench and Preview Host Architecture

## Authority

Authoritative for M029 process ownership.

```text
Workbench
  UI/input control
  sessions and aliases
  decisions
  promotion
        │ versioned local IPC
        ▼
Preview Host
  temporary worlds
  comparison
  playback
  capture
        │
        ▼
Actual engine content, rendering, animation, sound, adapters
```

The workbench owns editable input, mouse/touch command translation, durable decisions, and promotion. The preview host owns no input authority.

One normal preview host/window persists per active session. The host is independently restartable. Workbench input and decisions survive its failure. No duplicate renderer, audio engine, animation evaluator, or content model is permitted.
