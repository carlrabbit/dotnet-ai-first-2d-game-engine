# Runtime Snapshot and Mutation Architecture

```text
              EntityComponentWorld
              authoritative stores
                     |
              capture phase S
                     v
        ImmutableRuntimeSnapshot
          /          |           \
   behaviors      domain       spatial
      |             |             |
      +-------------+-------------+
                    |
             ordered proposals
                    v
          RuntimeTransaction
       validate -> stage -> commit
                    |
                    v
              new authority S2
```

Rules:

- `EntityComponentWorld` is the sole component authority.
- snapshots are detached, typed, deterministic and read-only;
- same behavior phase uses one snapshot;
- resolvers do not own live mutation APIs;
- stable component IDs define semantic identity;
- generic CLR lookup is only valid when unambiguous;
- transaction commit owns factual lifecycle/component success evidence;
- `SimulationWorld` higher-level semantics remain above this boundary.
