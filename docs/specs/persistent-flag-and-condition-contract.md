# Persistent Flag and Condition Contract

## Authority

Authoritative for boolean/enum flag definitions, runtime state, transitions, persistence, and bounded conditions.

Flags contain stable ID, type, value, revision, transition linkage, and runtime tick. Setting the same value is an accepted evidenced no-op.

Supported condition atoms: `flag-equals`, `inventory-contains`, `entity-lifecycle-equals`. Supported composition: `all`, `any`, `not`. Conditions are deterministic, immutable, side-effect-free, and inspectable. No scripting or arbitrary expressions.
