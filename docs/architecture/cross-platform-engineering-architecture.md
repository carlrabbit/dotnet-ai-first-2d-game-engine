# Cross-Platform Engineering Architecture

## Purpose

Define one engineering semantic core with native Linux and Windows launcher adapters.

## Target architecture

```text
                        Git project truth
                              |
                 Agentic2D.Engineering
       suite registry / receipts / fingerprints /
       process / temp / atomic files / environment /
             review / platform verification
                    /                 \
                   /                   \
             eng/*.sh                eng/*.ps1
             Bash adapter            PowerShell 7 adapter
                   |                    |
                 Linux                Windows
```

## Dependency direction

- Bash and PowerShell depend on the engineering host contract.
- Engineering host does not depend on Bash.
- Engineering host does not depend on PowerShell.
- Product runtime does not depend on engineering infrastructure.
- Platform-specific export code remains outside generic development parity semantics.
- Platform-specific graphics/native loading is isolated in graphical adapters.

## Suite model

Suite and shard identity is platform-neutral.

A shard may declare:

```text
platform: any | linux | windows
graphics: false | required
semantic-parity-group: optional stable ID
```

The host rejects execution of a platform-specific shard on the wrong host rather than recording a skip as success.

## Platform evidence

Each host emits a platform verification report bound to:

- source revision;
- relevant repository fingerprint;
- suite definition;
- platform metadata;
- semantic result hashes;
- graphics result where required.

Cross-platform comparison accepts host metadata differences and rejects semantic differences not explicitly classified as platform-specific.

## Historical launcher cleanup

The engineering host or a dedicated inventory command discovers tracked `eng/*.sh` files and active references.

Deletion decisions are evidence-backed and recorded under M036 artifacts.

Git history, not dead wrapper retention, preserves historical milestone implementation details.
