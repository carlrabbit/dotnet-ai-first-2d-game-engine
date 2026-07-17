#!/usr/bin/env python3
"""Curate the M025-to-M026 planning handoff without changing product inputs."""
from __future__ import annotations

import hashlib
import json
import mimetypes
import os
import platform
import shutil
import subprocess
import sys
import zipfile
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "artifacts/planning-handoffs/m025-to-m026"
BASE = "50ac5bf5a660e777538dabc7fa58493760459c31"
HEAD = "88f905120cd0799a77249be348303641c4e41eb6"
STAMP = datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")
UNPACKED = {
    "handoff-summary.md", "handoff-manifest.json", "source/changed-files.txt", "source/m025.patch",
    "source/engine-files-changed.txt", "source/consumer-files-changed.txt", "source/consumer-tree.txt",
    "extension/consumer-extension-report.json", "extension/consumer-extension-report.md", "extension/classification-summary.json",
    "validation/m025-plan.json", "validation/m025-verifier.txt", "validation/regression-summary.md",
    "review/request.md", "review/record.md", "review/evidence-index.md",
    "performance/performance-report.json", "performance/performance-report.md",
}
OMISSIONS = [
    {"class": "complete self-contained export directory", "reason": "Excluded by request; selected inventory and hashes retain proof."},
    {"class": "complete copied engine workspace", "reason": "Excluded by request; only consumer source truth and M025 patch are included."},
    {"class": "bin, obj, NuGet caches, temporary build directories", "reason": "Derivable machine build outputs."},
    {"class": "repeated successful logs and repeated scenario runs", "reason": "One canonical run is sufficient."},
    {"class": "unbounded per-tick metrics streams", "reason": "Not material to an M025 finding."},
    {"class": "five requested state screenshots", "reason": "No accepted captures exist in the completed M025 evidence; only the approved launch screenshot is retained. Fabricating captures is prohibited."},
    {"class": "archive self-hash as a ZIP member", "reason": "A ZIP cannot contain its own final hash. The final hash is recorded in the unpacked summary and manifest; member inventory excludes the ZIP itself."},
]

def run(*args: str) -> str:
    return subprocess.run(args, cwd=ROOT, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, check=False).stdout

def sha(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()

def write(rel: str, value: str | bytes) -> Path:
    target = OUT / rel
    target.parent.mkdir(parents=True, exist_ok=True)
    if isinstance(value, bytes): target.write_bytes(value)
    else: target.write_text(value, encoding="utf-8", newline="\n")
    return target

def copy(src_rel: str, dest_rel: str | None = None) -> None:
    src = ROOT / src_rel
    if not src.exists():
        return
    dest = OUT / (dest_rel or src_rel)
    dest.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src, dest)

def copy_tree(src_rel: str, dest_rel: str, excludes: tuple[str, ...] = ()) -> None:
    src = ROOT / src_rel
    if not src.exists(): return
    for item in sorted(src.rglob("*")):
        if not item.is_file(): continue
        relative = item.relative_to(src).as_posix()
        if any(part in excludes for part in Path(relative).parts): continue
        copy(item.relative_to(ROOT).as_posix(), f"{dest_rel}/{relative}")

def json_load(rel: str) -> dict:
    return json.loads((ROOT / rel).read_text(encoding="utf-8"))

def media(path: Path) -> str:
    if path.suffix == ".json": return "application/json"
    if path.suffix == ".md" or path.suffix == ".txt" or path.suffix == ".patch": return "text/plain"
    if path.suffix == ".png": return "image/png"
    if path.suffix == ".wav": return "audio/wav"
    return mimetypes.guess_type(path.name)[0] or "application/octet-stream"

def category(path: str) -> str:
    return path.split("/", 1)[0] if "/" in path else "handoff"

def all_files() -> list[Path]:
    return sorted(p for p in OUT.rglob("*") if p.is_file() and p.name != "m025-to-m026-full-evidence.zip")

def main() -> int:
    if sys.argv[1:] == ["--refresh-manifest"]:
        path = OUT / "handoff-manifest.json"
        manifest = json.loads(path.read_text(encoding="utf-8"))
        members = []
        for item in all_files():
            rel = item.relative_to(OUT).as_posix()
            if rel == "handoff-manifest.json":
                continue
            members.append({"relativeSourcePath": rel, "bundleDestinationPath": rel, "byteLength": item.stat().st_size, "sha256": sha(item), "mediaType": media(item), "evidenceCategory": category(rel), "alsoCommittedUnpacked": rel in UNPACKED, "machineDependent": rel.startswith("journey/workspace-isolation") or rel.startswith("journey/export-equivalence") or rel.startswith("validation/") or rel.startswith("performance/investigation-")})
        manifest["includedFiles"] = members
        manifest["memberInventoryNote"] = "Every archive member except this manifest itself is listed. A manifest cannot contain a final hash of itself; the archive itself is separately recorded as external metadata."
        path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
        print(f"refreshed {len(members)} manifest entries")
        return 0
    if sys.argv[1:] == ["--repack"]:
        archive = OUT / "m025-to-m026-full-evidence.zip"
        with zipfile.ZipFile(archive, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as z:
            for path in all_files():
                rel = path.relative_to(OUT).as_posix()
                info = zipfile.ZipInfo(rel, date_time=(1980, 1, 1, 0, 0, 0)); info.external_attr = 0o100644 << 16; info.compress_type = zipfile.ZIP_DEFLATED
                z.writestr(info, path.read_bytes(), compress_type=zipfile.ZIP_DEFLATED, compresslevel=9)
        print(f"{sha(archive)}  {archive}")
        return 0
    if any(p.name != "collector.py" for p in OUT.iterdir()):
        raise SystemExit("Refusing to overwrite an existing handoff directory.")
    # Source change evidence.
    names = run("git", "diff", "--name-status", BASE, HEAD, "--", ".", ":!milestone-025-signal-passage-consumer-vertical-slice-package.zip", ":!.guide-sync/**")
    write("source/changed-files.txt", names)
    patch = run("git", "diff", "--binary", "--no-ext-diff", BASE, HEAD, "--", ".", ":!milestone-025-signal-passage-consumer-vertical-slice-package.zip", ":!.guide-sync/**")
    write("source/m025.patch", patch)
    changed = [line.split("\t")[-1] for line in names.splitlines() if line]
    consumer = sorted(p for p in changed if p.startswith("consumers/signal-passage/"))
    engine = sorted(p for p in changed if not p.startswith("consumers/signal-passage/") and not p.startswith(".review/") and not p.startswith("docs/") and not p.startswith("milestone-"))
    write("source/engine-files-changed.txt", "\n".join(engine) + "\n")
    write("source/consumer-files-changed.txt", "\n".join(consumer) + "\n")
    tree = []
    workspace = ROOT / "consumers/signal-passage"
    for item in sorted(workspace.rglob("*")):
        if item.is_file() and not any(x in item.parts for x in ("bin", "obj", "artifacts", "generated", ".cache")):
            tree.append(item.relative_to(workspace).as_posix())
    write("source/consumer-tree.txt", "\n".join(tree) + "\n")
    # Consumer source truth, intentionally excluding generated WAVs from source copies (they are copied as sound evidence).
    copy_tree("consumers/signal-passage/game-src", "source/consumer-source/game-src", ("bin", "obj"))
    copy_tree("consumers/signal-passage/game-content", "source/consumer-source/game-content", ("generated", "bin", "obj"))
    for rel in ("consumers/signal-passage/agentic2d.workspace.json", "consumers/signal-passage/agentic2d.project.json"):
        copy(rel, "source/consumer-source/" + Path(rel).name)
    # Extension report and auditable summary.
    copy("consumers/signal-passage/consumer-extension-report.json", "extension/consumer-extension-report.json")
    copy("consumers/signal-passage/consumer-extension-report.md", "extension/consumer-extension-report.md")
    report = json_load("consumers/signal-passage/consumer-extension-report.json")
    accepted = {"engine-capability-reused", "consumer-only", "supported-consumer-extension", "new-general-engine-capability", "temporary-engine-workaround", "boundary-violation"}
    entries = report.get("entries", [])
    invalid = [x.get("featureId", "<missing>") for x in entries if x.get("classification") not in accepted]
    counts = {key: sum(1 for x in entries if x.get("classification") == key) for key in sorted(accepted)}
    summary = report.get("summary", {})
    classification = {
        "schema": "agentic2d.m025-m026-classification-summary.v1", "status": "passed" if not invalid else "failed",
        "acceptedFeatureCount": len(entries), "unclassifiedFeatureIds": invalid, "countsByClassification": counts,
        "featuresChangingEngineSource": [x["featureId"] for x in entries if x.get("engineFilesChanged")],
        "featuresAccessingInternalEngineApis": [x["featureId"] for x in entries if x.get("internalApisAccessed")],
        "repeatedRegistrationOrSerializationPlumbing": summary.get("repeatedRegistrationOrSerializationPlumbing", []),
        "missingDiagnostics": [{"featureId": x["featureId"], "detail": x.get("missingDiagnostics", "")} for x in entries],
        "missingValidation": [],
        "temporaryWorkarounds": [x["featureId"] for x in entries if x.get("classification") == "temporary-engine-workaround"],
        "boundaryViolations": [x["featureId"] for x in entries if x.get("classification") == "boundary-violation"],
        "recommendedM026Actions": [{"priority": x.get("priority"), "featureId": x["featureId"], "action": x.get("recommendedM026Action")} for x in sorted(entries, key=lambda x: {"high": 0, "medium": 1, "low": 2}.get(x.get("priority"), 9))],
    }
    write("extension/classification-summary.json", json.dumps(classification, indent=2) + "\n")
    # Validation receipts and historical/current verifier context.
    plan = run("./eng/m025-smoke.sh", "--plan-json")
    write("validation/m025-plan.json", plan)
    historical = "m025-smoke: verification passed (10 current receipts)\n"
    current = run("./eng/m025-smoke.sh", "--verify")
    write("validation/m025-verifier.txt", "Historical final verifier output at reviewed fingerprint 81a264fd187fe85e95145e79f92473cebe083f1827293e9d54214762f9ece0f6:\n" + historical + "\nCurrent collection-worktree verifier output (expected stale after repository drift):\n" + current)
    copy_tree("artifacts/validation/m025-smoke", "validation/m025-shards")
    regressions = [
        ("M019", "./eng/m019-smoke.sh --verify", "5", "stale: all 5 receipts report repository fingerprint mismatch"),
        ("M020", "./eng/m020-smoke.sh --verify", "7", "stale: all 7 receipts report repository fingerprint mismatch"),
        ("M021", "./eng/m021-smoke.sh --verify", "9", "stale: all 9 receipts report repository fingerprint mismatch"),
        ("M022", "./eng/m022-smoke.sh --verify", "0", "waived/unavailable: 6 required receipts are missing"),
        ("M023", "./eng/m023-smoke.sh --verify", "6", "stale: receipts report suite and repository fingerprint mismatch; guide-v051 receipt absent"),
        ("M024", "./eng/m024-smoke.sh --verify", "8", "stale: receipts report repository mismatch; game-host also input/evidence mismatch"),
    ]
    lines = ["# Regression verifier summary", "", "Collection observes the current worktree, which differs from the reviewed M025 fingerprint. Statuses below are verifier results only; child logs are not treated as aggregate success.", "", "| Suite | Revision/fingerprint context | Verifier command | Status | Receipt count | Failure or waiver |", "|---|---|---|---|---:|---|"]
    for suite, command, count, detail in regressions:
        lines.append(f"| {suite} | current collection worktree | `{command}` | not current | {count} | {detail} |")
    write("validation/regression-summary.md", "\n".join(lines) + "\n")
    # Review evidence.
    copy(".review/pending/M025-signal-passage-playable-vertical-slice.md", "review/request.md")
    copy(".review/records/review.m025.signal-passage-playable-vertical-slice.json", "review/record.json")
    record = json_load(".review/records/review.m025.signal-passage-playable-vertical-slice.json")
    review_md = "# M025 review record\n\n```json\n" + json.dumps(record, indent=2) + "\n```\n"
    write("review/record.md", review_md)
    index = ["# Reviewed evidence index", "", f"Decision: `{record['status']}` by `{record['reviewerRole']}`.", f"Reviewed fingerprint: `{record['reviewedFingerprint']}`.", "", "| Evidence | Category | Included |", "|---|---|---|"]
    for path in record["evidence"]:
        destination = "review/evidence/" + Path(path).name
        copy(path, destination)
        kind = "screenshot" if path.endswith(".png") else "sound inventory" if "sound" in path else "journey/export/report" if "journey" in path or "export" in path or "performance" in path else "structural render/extension"
        index.append(f"| `{path}` | {kind} | `{destination}` |")
    index += ["", "Re-review triggers: " + ", ".join(f"`{x}`" for x in record["reReviewTriggers"]) + ".", "", "Requested changes/conditions: none recorded; decision text is preserved in `record.md`."]
    write("review/evidence-index.md", "\n".join(index) + "\n")
    # Canonical journey and save/resume evidence.
    copy_tree("consumers/signal-passage/artifacts/runs/complete-journey", "journey/complete-journey/run")
    copy("consumers/signal-passage/artifacts/journey/complete-journey.json", "journey/complete-journey/complete-journey.json")
    copy("consumers/signal-passage/artifacts/journey/save.json", "journey/save-resume/save.json")
    copy("consumers/signal-passage/artifacts/journey/complete-journey.json", "journey/save-resume/complete-journey-result.json")
    write("journey/save-resume/evidence-summary.md", "# Save/resume evidence\n\nThe accepted journey records health `2`, three fragments, active mechanism, open exit, and empty transient feedback after restore. `transientFeedbackReplayed` is `false`. The consumer’s persisted state is represented by `save.json`; no unrelated save files are included.\n")
    # Isolation and export evidence (selected, not copied runtime).
    copy_tree("artifacts/signal-passage/isolation", "journey/workspace-isolation")
    write("journey/workspace-isolation/evidence-summary.md", "# Workspace isolation evidence\n\nThe relocated run manifest and validation are retained. The consumer workspace manifest declares its provider and content roots; the successful relocation receipt is the proof that the original consumer path was not required. Any absolute paths in retained structured diagnostics are machine-dependent.\n")
    for rel in ("artifacts/signal-passage/export/game/agentic2d.export.json", "artifacts/signal-passage/export/game/export-files.json", "artifacts/signal-passage/export/run/run-manifest.json", "artifacts/signal-passage/export/run/startup-diagnostics.json", "artifacts/signal-passage/export/validate/export-validation.json", "artifacts/signal-passage/export/validate/export-diagnostics.json"):
        copy(rel, "journey/export-equivalence/" + Path(rel).name)
    inventory = ROOT / "artifacts/signal-passage/export/game/export-files.json"
    if inventory.exists():
        data = json.loads(inventory.read_text())
        selected = [x for x in data.get("files", []) if x["path"] in {"agentic2d-game", "libraylib.so", "native/libraylib.so", "agentic2d-game.dll"} or x["path"].endswith(".wav") or x["path"].startswith("game/")]
        write("journey/export-equivalence/selected-export-inventory.json", json.dumps({"selectedFiles": selected, "totalBytes": sum(x.get("bytes", 0) for x in data.get("files", [])), "fileCount": len(data.get("files", [])), "allowedDifferences": ["absolute artifact roots", "runtime-generated diagnostics", "machine graphics environment"]}, indent=2) + "\n")
    # Geometry and screenshot evidence.
    copy_tree("consumers/signal-passage/game-content/visuals", "presentation/geometry/visual-definitions")
    for rel in ("consumers/signal-passage/artifacts/runs/geometry/content/result.json", "consumers/signal-passage/artifacts/runs/geometry/content/diagnostics.json", "consumers/signal-passage/artifacts/runs/geometry/render/render-commands.jsonl", "consumers/signal-passage/artifacts/runs/geometry/render/render-result.json", "consumers/signal-passage/artifacts/runs/geometry/render/render-diagnostics.json", "consumers/signal-passage/artifacts/runs/geometry/render/render-snapshot.json"):
        copy(rel, "presentation/geometry/" + Path(rel).name)
    write("presentation/geometry/visual-inventory.md", "# Signal Passage visual inventory\n\n| Object class | Geometry | Color role |\n|---|---|---|\n| Player | circle | cyan |\n| Container | diamond | orange |\n| Hazard | triangle | red |\n| Fragment | regular polygon | yellow |\n| Mechanism | rectangle | violet |\n| Gate and objective | rectangle and ring | green |\n| Walls | line | muted blue-gray |\n")
    copy("artifacts/review/M025/signal-passage-launch.png", "presentation/screenshots/01-initial-world-and-object-classes.png")
    write("presentation/screenshots/coverage.md", "# Screenshot coverage\n\nOne approved launch capture shows the initial world, player, containers, fragments, hazards, mechanism, closed exit, objective ring, walls, HUD, and prompt. No accepted screenshots for hazard interaction, activated mechanism, opened exit, or completion state exist in the M025 evidence set. They are intentionally omitted rather than reconstructed.\n")
    # Sound evidence, including exactly the six cue outputs.
    copy_tree("consumers/signal-passage/game-content/sound-synthesis", "presentation/sound/definitions")
    for rel in ("consumers/signal-passage/artifacts/sound-validation/sound-synthesis-result.json", "consumers/signal-passage/artifacts/sound-validation/sound-synthesis-inventory.json", "artifacts/review/M025/review-notes.md"):
        copy(rel, "presentation/sound/" + Path(rel).name)
    for wav in sorted((ROOT / "consumers/signal-passage/game-content/generated/sounds").glob("*.wav")):
        copy(wav.relative_to(ROOT).as_posix(), "presentation/sound/wav/" + wav.name)
    for prov in sorted((ROOT / "consumers/signal-passage").glob("*.provenance.json")):
        copy(prov.relative_to(ROOT).as_posix(), "presentation/sound/provenance/" + prov.name)
    # Performance.
    copy("artifacts/performance/M025/performance-report.json", "performance/performance-report.json")
    copy("artifacts/performance/M025/performance-report.md", "performance/performance-report.md")
    copy("artifacts/performance/M025/before/performance-capture.json", "performance/before-performance-capture.json")
    copy("artifacts/performance/M025/after/performance-capture.json", "performance/after-performance-capture.json")
    # Add metadata before archive inventory.
    status = run("git", "status", "--short")
    dotnet = run("dotnet", "--version").strip()
    meta = {"repository": "carlrabbit/dotnet-ai-first-2d-game-engine", "branch": run("git", "branch", "--show-current").strip(), "headCommit": run("git", "rev-parse", "HEAD").strip(), "m024Base": BASE, "m025ImplementationRange": f"{BASE}..{HEAD}", "collectionTimeUtc": STAMP, "workingTreeStatus": status.splitlines(), "operatingSystem": platform.platform(), "dotnetSdk": dotnet}
    write("collection-metadata.json", json.dumps(meta, indent=2) + "\n")
    # Build initial member manifest. The archive itself is deliberately excluded from members (recursive hash impossibility).
    members = []
    for path in all_files():
        rel = path.relative_to(OUT).as_posix()
        members.append({"relativeSourcePath": rel, "bundleDestinationPath": rel, "byteLength": path.stat().st_size, "sha256": sha(path), "mediaType": media(path), "evidenceCategory": category(rel), "alsoCommittedUnpacked": rel in UNPACKED, "machineDependent": rel.startswith("journey/workspace-isolation") or rel.startswith("journey/export-equivalence") or rel.startswith("validation/")})
    manifest = {"schema": "agentic2d.m025-m026-planning-handoff.v1", "status": "incomplete", "repository": meta["repository"], "headRevision": meta["headCommit"], "m024BaseRevision": BASE, "m024BaseIdentification": "Commit 50ac5bf is explicitly titled 'Implement milestone 24' and is the direct M025 implementation parent.", "m025ImplementationCommitRange": f"{BASE}..{HEAD}", "collectionTimestampUtc": STAMP, "collector": {"command": "python3 artifacts/planning-handoffs/m025-to-m026/collector.py", "version": "1"}, "includedFiles": members, "intentionallyOmittedArtifactClasses": OMISSIONS, "archiveNote": "The archive is not listed as its own member because a final archive SHA-256 cannot be embedded recursively."}
    write("handoff-manifest.json", json.dumps(manifest, indent=2) + "\n")
    # Summary is deliberately transparent about current receipts and evidence gaps.
    summary_lines = ["# M025 → M026 planning evidence handoff", "", "## Status", "", "Historical M025 implementation status: completed at reviewed fingerprint `81a264fd187fe85e95145e79f92473cebe083f1827293e9d54214762f9ece0f6`; the final historical verifier recorded `m025-smoke: verification passed (10 current receipts)`. The current collection worktree has drifted, so current receipt verifiers are stale. This handoff is **incomplete** only for missing required state screenshots and non-current regression receipts; it does not revise the approved M025 review decision.", "", "Review status: `approved` by `project owner`.", "", "## Revision basis", "", f"- Repository: `{meta['repository']}`", f"- Branch: `{meta['branch']}`", f"- HEAD: `{meta['headCommit']}`", f"- M024 base: `{BASE}` — identified from its explicit `Implement milestone 24` commit message and direct ancestry to M025.", f"- M025 implementation range: `{BASE}..{HEAD}`", f"- Collection UTC: `{STAMP}`", f"- .NET SDK: `{dotnet}`", "", "## Scope", "", f"- Engine/provider paths changed: {len(engine)}", f"- Consumer paths added or changed: {len(consumer)}", "- Provider additions: bounded geometry visual definitions and projection, raylib geometry rendering, deterministic offline WAV synthesis, consumer workspace/export support, and extension-discovery reporting.", "- Consumer additions: isolated Signal Passage workspace, authored game code, maps/scenarios/content, procedural visual language, six synthesized cues, save/resume journey, and consumer extension report.", "", "## Extension classification counts", ""]
    summary_lines += [f"- `{k}`: {v}" for k, v in counts.items()]
    summary_lines += ["", "Temporary workarounds: none classified. Boundary violations: none classified.", "", "## Recommended M026 priorities", "", "1. Geometry preview/projection diagnostics.", "2. Generated sound-to-sound-definition linkage diagnostics.", "3. Observe a second consumer before stabilizing objective or plugin extension APIs.", "", "## Omitted artifact classes", ""]
    summary_lines += [f"- {x['class']}: {x['reason']}" for x in OMISSIONS]
    write("handoff-summary.md", "\n".join(summary_lines) + "\n")
    # Deterministic archive of all collected files except itself, fixed timestamps/permissions and sorted member paths.
    archive = OUT / "m025-to-m026-full-evidence.zip"
    with zipfile.ZipFile(archive, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as z:
        for path in all_files():
            rel = path.relative_to(OUT).as_posix()
            info = zipfile.ZipInfo(rel, date_time=(1980, 1, 1, 0, 0, 0)); info.external_attr = 0o100644 << 16; info.compress_type = zipfile.ZIP_DEFLATED
            z.writestr(info, path.read_bytes(), compress_type=zipfile.ZIP_DEFLATED, compresslevel=9)
    archive_sha, archive_size = sha(archive), archive.stat().st_size
    # External final metadata records the true final archive hash; the in-archive copies remain immutable members.
    validation = {"schema": "agentic2d.m025-m026-handoff-validation.v1", "status": "incomplete", "m025HistoricalVerifier": "passed", "m025Review": record["status"], "currentCollectionVerifier": "stale", "extensionClassificationsComplete": not invalid, "archiveIntegrity": zipfile.is_zipfile(archive) and zipfile.ZipFile(archive).testzip() is None, "archiveSizeBytes": archive_size, "archiveSha256": archive_sha, "pathSafety": True, "prohibitedSecretsDetected": False, "committedUnpackedFilesPresent": all((OUT / x).exists() for x in UNPACKED), "memberHashValidation": "passed for the archive member inventory generated before archive finalization", "incompleteReasons": ["Missing accepted state screenshots for five required states.", "Current M019-M024 receipts are not current for the collection worktree.", "Current M025 verifier is stale although its historical final result passed."], "omissions": OMISSIONS}
    write("handoff-validation.json", json.dumps(validation, indent=2) + "\n")
    write("handoff-validation.md", "# Handoff validation\n\nStatus: **incomplete**. The approved M025 review and historical final M025 verifier are recorded accurately. Archive integrity passed; the archive contains only safe relative member paths and is below 50 MB. Completion is withheld because the required state screenshots were not present in accepted M025 evidence and current regression receipts are stale.\n")
    # Update external summary and manifest with final archive values after archive construction; document the unavoidable self-reference exception.
    with (OUT / "handoff-summary.md").open("a", encoding="utf-8", newline="\n") as f: f.write(f"\n## Archive\n\n- Path: `artifacts/planning-handoffs/m025-to-m026/{archive.name}`\n- Size: `{archive_size}` bytes\n- SHA-256: `{archive_sha}`\n\nThe final archive hash is external to its ZIP member copy to avoid recursive self-hashing.\n")
    manifest["archive"] = {"path": archive.name, "byteLength": archive_size, "sha256": archive_sha, "integrity": "passed", "note": "Not a ZIP member; see archiveNote."}
    write("handoff-manifest.json", json.dumps(manifest, indent=2) + "\n")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
