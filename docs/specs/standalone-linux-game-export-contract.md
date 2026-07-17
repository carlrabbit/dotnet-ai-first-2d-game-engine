# Standalone Linux Game Export Contract

## Authority

Authoritative for the initial exported-game target, publish mode, export assembly, content inclusion, startup manifest, file inventory, fingerprints, transactional output, inspection, and validation.

## Supported target

```text
RID: linux-x64
configuration: Release
deployment: self-contained
layout: directory
trimmed: false
single-file: false
network required: false
```

No other target or publish mode is supported in M024.

## Required properties

An export contains:

- one game executable;
- required managed runtime files;
- required Linux native dependencies;
- selected game runtime content and assets;
- `agentic2d.export.json`;
- declared license files;
- complete file inventory.

It excludes source, tests, documentation, guide/review metadata, repository engineering commands, and unrelated generated artifacts.

## Assembly

Export assembly validates the project and content, publishes the minimal game host, copies required files, generates manifests and hashes, validates the assembled result, and transactionally replaces the requested final directory.

Partial exports are never authoritative.

## Identity

The export fingerprint includes semantic configuration and file hashes but excludes output absolute paths, timestamps, process identity, and temporary directories.

Byte-for-byte equivalence across machines is not required.
