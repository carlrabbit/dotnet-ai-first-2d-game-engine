# Save, Load, and Resume Contract

## Authority

Authoritative for save creation, inspection, validation, transactional fresh-runtime load, semantic resume, and equivalence.

```text
parse → compatibility/contributor/reference validation → complete load plan
→ fresh runtime construction → one transaction → reconstructed-state validation
```

Required equivalence:

```text
save A → load → save B without advance → A == B
```

and uninterrupted `0..M` equals `0..N → save → fresh load → resume N..M` for authoritative state and post-resume behavior. No automatic migrations are included.
