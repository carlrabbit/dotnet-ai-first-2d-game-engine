# Workspace Scaffolding Contract

## Authority

Authoritative for engine acquisition providers, `workspace create`, target safety, transactional creation, template rendering, and generated workspace content.

## Command

```text
agentic2d workspace create <target>
```

Required template:

```text
minimal-game
```

## Providers

Directory supports `reference` and `copy` placement.

Git requires an explicit revision and records the resolved full commit SHA.

Portable SDK is contract-reserved only.

Provider extensibility is internal built-in registration, not dynamic plugins.

## Target safety

- missing target: allowed;
- existing empty target: allowed through staging/finalization;
- existing non-empty target: rejected;
- no force overwrite;
- no merge;
- no update/migration semantics.

## Transaction

```text
validate
→ stage
→ acquire
→ scaffold
→ validate generated workspace
→ finalize target
```

Failure must not leave an apparently valid target.

## Copy policy

Exclude `.git`, `bin`, `obj`, artifacts, editor state, and temporary files. Prefer tracked files when source is Git and Git is available.

## Generated content

The `minimal-game` template must build, validate, and run a real deterministic scenario. Success-only placeholders are forbidden.

## Generated launcher

The template emits `eng/agentic2d.sh` and `eng/engine-bootstrap.env` in addition to the four workflow wrappers. The launcher needs Bash and the .NET SDK only; wrappers do not parse JSON and do not require Python, jq, Node.js, Bun, Ruby, Perl, PowerShell, or another JSON runtime. Directory-reference bootstrap paths are relative to `eng/` when possible. Copy and Git paths resolve to the workspace-local `engine-src/` directory.
