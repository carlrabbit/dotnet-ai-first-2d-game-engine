# Persistence Contributor Contract

## Authority

Authoritative for persistence contributor registration, schemas, capture, validation, load planning, reconstruction, and required/optional policy.

Each contributor declares stable ID, schema version, required/optional status, canonical records, reference validation, compatibility rules, load-plan creation, transactional application, diagnostics, and fingerprint.

Capture reads immutable state. Load applies to a fresh runtime only after every contributor validates. Unknown required contributors reject load; unknown optional contributors may be ignored only when explicitly optional and safe.
