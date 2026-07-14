# Game Workspace Manifest Contract

## Authority

Authoritative for `agentic2d.workspace.json`, which describes how a checkout composes a game project, engine source, tooling, mutation policies, and generated artifacts.

## Required fields

- schema/version;
- stable workspace ID;
- project manifest path;
- engine acquisition record;
- area declarations;
- artifact root;
- generated wrapper roots;
- workspace fingerprint inputs.

## Engine providers

Implemented:

```text
directory-reference
directory-copy
git-clone
```

Reserved but unsupported:

```text
portable-sdk
```

NuGet is not a valid provider.

## Areas

Roles:

```text
engine-provider
game-code
authored-content
generated-artifacts
tooling
```

Mutation policies:

```text
read-only-unless-authorized
writable
replaceable-generated
```

## Validation

Validate project path, provider data, engine compatibility, area non-overlap, mutation policies, wrappers, artifact root, provider-specific revision/fingerprint data, and unsupported-provider diagnostics.

Absolute diagnostic paths may be emitted, but must not influence semantic fingerprinting.
