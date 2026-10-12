# Platform and Development Context

## Status

Current planning context; non-authoritative.

## Question

Which development-environment facts are expensive enough to remember because they materially affect planning and validation?

## Current observed context

Project authority currently defines:

```text
active development platform: Windows
native launcher: PowerShell 7
platform epoch starts: M036
inactive supported platform: Linux
```

The engine remains architecturally cross-platform; ordinary milestones do not need to execute every platform-sensitive check on every supported platform.

## Important planning distinction

```text
supported target
!=
active verified platform
!=
automation harness capability
```

A platform-sensitive failure must be classified before being treated as product failure.

## Interactive graphics observation

During M048/M050 review work, Raylib/OpenGL failed when invoked without a suitable interactive Windows graphics context, then worked after the user entered an interactive TeamViewer desktop session.

Planning value:

- do not infer missing GPU/OpenGL support solely from a service/non-interactive session failure;
- graphical validation should name the required interactive execution locus;
- evidence should come from the actual graphical process when claiming graphical operation.

This observation is environment-specific and should be revalidated if the graphics stack, remote-access method, Windows configuration, or renderer changes.

## Audio observation

Audio capability is separate from graphics capability.

A working interactive graphics context does not guarantee a usable Windows playback endpoint. Preview tooling should distinguish audio-device initialization, endpoint readiness, source availability, decoded/loaded audio validity, and actual playback ability.

Subjective audio quality remains human review where required.

## Deferred Linux verification

Current authority permits Linux-sensitive checks to accumulate as explicit deferred verification debt during the Windows epoch.

When Linux becomes active:

- run current portable baseline;
- collect deferred Linux obligations;
- validate/fix against the current repository;
- do not replay every historical milestone commit;
- do not reopen completed subjective reviews merely because the platform changed.

## Planning implication for future UI/renderer work

The active UI track targets headless semantic rendering/inspection, Raylib, and a future next-generation rendering adapter.

Future planning should keep renderer-neutral semantic behavior mechanically testable and isolate native/window/device validation to explicit adapter targets and loci.

## Related authority

- `docs/engineering/platform-verification.md`
- `eng/platform-verification.json`
- `docs/architecture/agentic-ui-direction.md`

## Freshness

Last verified against project authority and recent graphical-review experience: 2026-10-12.

Revalidate on platform-epoch switch, renderer change, remote-access change, graphics-driver/runtime change, or audio-device topology change.
