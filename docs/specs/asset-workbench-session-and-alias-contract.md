# Asset Workbench Session and Alias Contract

## Authority

Authoritative for M029 workbench sessions, resume, ephemeral numbered aliases, local search context, recent navigation, and references to operational input state.

## Schemas

```text
agentic2d.asset-workbench-session.v1
agentic2d.asset-workbench-alias-map.v1
agentic2d.asset-workbench-recent-items.v1
agentic2d.asset-workbench-status.v1
```

Aliases are numeric, ephemeral, scoped to one session/list/filter generation, regenerated after resume, invalidated by relevant profile changes, and never durable identity. Stale aliases fail with instructions to list again.

The session may reference an operational input-state record, but partial text is not decision authority.

Search is local and bounded. Recent items are operational convenience only.
