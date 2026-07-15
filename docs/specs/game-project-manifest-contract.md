# Game Project Manifest Contract

## Authority

Authoritative for `agentic2d.project.json`, which contains game/product truth independent of engine acquisition and checkout composition.

## Required identity

```text
schema
project ID
project structural version/fingerprint inputs
```

## Required areas

- game source roots;
- authored content roots;
- default scenario ID;
- runtime configuration;
- presentation configuration;
- declared game assemblies/extensions;
- supported content domains.

## Boundary

The project manifest must not contain:

- engine checkout paths;
- Git repository URLs;
- acquisition provider IDs;
- mutation policies;
- workspace artifact locations;
- external guide authority.

The same project manifest may be composed into multiple workspaces.

## Validation

Validate stable IDs, relative roots, non-overlapping product roots, required default scenario references, finite runtime/presentation values, and current content-domain compatibility.

Canonical project fingerprints exclude machine-specific absolute paths and generated artifacts.
