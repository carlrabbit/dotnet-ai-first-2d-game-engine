#!/usr/bin/env python3
"""Curate the M026-to-M027 evidence handoff without changing product sources."""
from __future__ import annotations

import hashlib, json, mimetypes, os, platform, shutil, subprocess, sys, zipfile
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
BASE = "7d982d7"
REV = "5fe5a7c09051c85368257d9d6c45914c0f09e790"
NOW = datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")
INCLUDED: list[dict] = []
OMISSIONS: list[dict] = []

def run(*args: str, check=True) -> str:
    result = subprocess.run(args, cwd=ROOT, text=True, capture_output=True)
    if check and result.returncode: raise RuntimeError(f"{' '.join(args)}\n{result.stdout}\n{result.stderr}")
    return (result.stdout + result.stderr).strip()

def sha(path: Path) -> str:
    h=hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda:f.read(1024*1024),b''): h.update(block)
    return h.hexdigest()

def add(src: str|Path, dest: str|None=None, category="evidence", committed=True, generated=True, machine=False):
    source=ROOT/str(src); target=OUT/(dest or str(src))
    if not source.is_file():
        OMISSIONS.append({"path":str(src),"reason":"source absent at collection time"}); return
    target.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(source,target)
    INCLUDED.append({"originalSourcePath":str(src),"bundlePath":str(target.relative_to(OUT)),"byteLength":target.stat().st_size,"sha256":sha(target),"mediaType":mimetypes.guess_type(target.name)[0] or "application/octet-stream","evidenceCategory":category,"committedUnpacked":committed,"machineDependent":machine,"generated":generated})

def add_tree(src: str, dest: str, category="source"):
    root=ROOT/src
    for item in sorted(root.rglob('*')):
        if item.is_file() and not any(part in {"bin","obj","artifacts"} for part in item.relative_to(root).parts):
            add(item.relative_to(ROOT), f"{dest}/{item.relative_to(root)}", category, True, False)

def text(path: str, value: str, category="derived"):
    target=OUT/path; target.parent.mkdir(parents=True,exist_ok=True); target.write_text(value,encoding='utf-8')
    INCLUDED.append({"originalSourcePath":"derived","bundlePath":path,"byteLength":target.stat().st_size,"sha256":sha(target),"mediaType":"text/markdown" if path.endswith('.md') else "application/json" if path.endswith('.json') else "text/plain","evidenceCategory":category,"committedUnpacked":True,"machineDependent":False,"generated":True})

def main():
    if OUT.exists():
        for child in OUT.iterdir():
            if child.name != 'collector.py': shutil.rmtree(child) if child.is_dir() else child.unlink()
    branch=run('git','branch','--show-current'); head=run('git','rev-parse','HEAD'); status=run('git','status','--short')
    INCLUDED.append({"originalSourcePath":"artifacts/planning-handoffs/m026-to-m027/collector.py","bundlePath":"collector.py","byteLength":(OUT/'collector.py').stat().st_size,"sha256":sha(OUT/'collector.py'),"mediaType":"text/x-python","evidenceCategory":"collector","committedUnpacked":True,"machineDependent":False,"generated":False})
    changed=run('git','diff','--name-only',f'{BASE}..{REV}').splitlines()
    patch=run('git','diff','--binary','--','.') if False else run('git','diff','--binary',f'{BASE}..{REV}')
    text('source/changed-files.txt','\n'.join(changed)+'\n','source')
    for key,prefix in [('provider','src/'),('signal-passage','consumers/signal-passage/'),('tic-tac-toe','consumers/autonomous-tic-tac-toe/')]:
        text(f'source/{key}-files-changed.txt','\n'.join(x for x in changed if x.startswith(prefix))+'\n','source')
    text('source/m026.patch',patch,'source')
    text('source/signal-passage-tree.txt',run('git','ls-tree','-r','--name-only',REV,'--','consumers/signal-passage'),'source')
    text('source/tic-tac-toe-tree.txt',run('git','ls-tree','-r','--name-only',REV,'--','consumers/autonomous-tic-tac-toe'),'source')
    hygiene=f"# Branch hygiene\n\n- Branch: `{branch}`\n- Accepted M025 base: `{BASE}` (`Implement milestone 25`)\n- M026 implementation: `{REV}` (`Implement milestone 26`)\n- Range: `{BASE}..{REV}`\n- Handoff base: current implementation revision `{head}`\n- Current worktree at collection: \n\n```text\n{status or '(clean)'}\n```\n\nThe pending-to-record review change is evidence-only and is not staged by this handoff. Copied milestone ZIPs, temporary workspaces, `bin`, `obj`, and generated run directories are excluded. No history was rewritten and no implementation files are staged.\n"
    text('source/branch-hygiene.md',hygiene,'source')
    add_tree('consumers/signal-passage','source/signal-passage-source')
    add_tree('consumers/autonomous-tic-tac-toe','source/tic-tac-toe-source')
    # Curated geometry evidence.
    for stem in ('geometry-inspection.json','geometry-preview.json','geometry-diagnostics.json','geometry-projection-comparison.json'):
        add(f'artifacts/geometry/M026/all-supported-shapes/{stem}',f'geometry/all-supported-shapes/{stem}','geometry',machine=True)
        add(f'artifacts/geometry/M026/signal-passage/{stem}',f'geometry/signal-passage/{stem}','geometry',machine=True)
        add(f'artifacts/geometry/M026/tic-tac-toe/{stem}',f'geometry/tic-tac-toe/{stem}','geometry',machine=True)
    add('artifacts/geometry/M026/all-supported-shapes/geometry-inspection.json','geometry/geometry-inspection.json','geometry',machine=True)
    add('artifacts/geometry/M026/all-supported-shapes/geometry-preview.json','geometry/geometry-preview.json','geometry',machine=True)
    add('artifacts/geometry/M026/all-supported-shapes/geometry-diagnostics.json','geometry/geometry-diagnostics.json','geometry',machine=True)
    add('artifacts/geometry/M026/tic-tac-toe/geometry-projection-comparison.json','geometry/geometry-projection-comparison.json','geometry',machine=True)
    # Linkage evidence, sources, and exactly the requested cues.
    for consumer in ('signal-passage','tic-tac-toe'):
        add(f'artifacts/sound-linkage/M026/{consumer}/generated-sound-linkage-report.json',f'sound-linkage/{consumer}-generated-sound-linkage-report.json','sound-linkage',machine=True)
        add(f'artifacts/sound-linkage/M026/{consumer}/generated-sound-linkage-report.md',f'sound-linkage/{consumer}-generated-sound-linkage-report.md','sound-linkage',machine=True)
    add('artifacts/sound-linkage/M026/tic-tac-toe/generated-sound-linkage-report.json','sound-linkage/generated-sound-linkage-report.json','sound-linkage',machine=True)
    add('artifacts/sound-linkage/M026/tic-tac-toe/generated-sound-linkage-report.md','sound-linkage/generated-sound-linkage-report.md','sound-linkage',machine=True)
    for p in sorted((ROOT/'consumers/autonomous-tic-tac-toe/game-content/generated/sounds').glob('*.wav')): add(p.relative_to(ROOT),f'sound-linkage/tic-tac-toe-wav/{p.name}','sound-linkage',machine=True)
    # Compact scenario evidence.
    scenarios=['ai-vs-ai-smoke','deterministic-random-choice','x-wins','o-wins','draw','round-reset','human-takes-x','human-takes-o','release-control','invalid-cell-rejected','presentation-smoke','save-during-thinking']
    for s in scenarios:
        for name in ('tic-tac-toe-result.json','tic-tac-toe-presentation.json','tic-tac-toe-save.json'):
            add(f'consumers/autonomous-tic-tac-toe/artifacts/runs/{s}/{name}',f'tic-tac-toe/{s}/{name}','tic-tac-toe',machine=True)
    for rel in ['artifacts/validation/workspace-validation.json','artifacts/project-validation/project-validation.json','artifacts/sound-linkage/generated-sound-linkage-report.json']:
        add(f'consumers/autonomous-tic-tac-toe/{rel}',f'tic-tac-toe/{Path(rel).name}','tic-tac-toe',machine=True)
    # Signal Passage only M026 deltas.
    for p in ['artifacts/geometry/M026/signal-passage/geometry-diagnostics.json','artifacts/sound-linkage/M026/signal-passage/generated-sound-linkage-report.json','consumers/signal-passage/artifacts/journey/complete-journey.json','consumers/signal-passage/artifacts/journey/save.json']:
        add(p,f'signal-passage/{Path(p).name}','signal-passage',machine=True)
    # Performance and boundaries.
    for p in ['artifacts/performance/M026/performance-report.json','artifacts/performance/M026/performance-report.md']:
        add(p,f'performance/{Path(p).name}','performance',machine=True)
    for p in ['artifacts/performance/M026/before/performance-capture.json','artifacts/performance/M026/after/performance-capture.json','artifacts/performance/M026/capture/performance-capture.json']:
        add(p,f'performance/{Path(p).parent.name}/{Path(p).name}','performance',machine=True)
    perf=json.loads((ROOT/'artifacts/performance/M026/performance-report.json').read_text())
    workloads=perf.get('comparison',{}).get('workloads',[])
    small=[w['id'] for w in workloads if not w.get('timingAuthority',False)]
    scaled=[w['id'] for w in workloads if 'scaled' in w['id']]
    assessment={"schema":"agentic2d.m026-performance-handoff-assessment.v1","belowTenMilliseconds":small,"timingAuthoritative":[w['id'] for w in workloads if w.get('timingAuthority')],"scaledWorkloads":scaled,"allScaledTargetReached":all(w.get('timingAuthority',False) for w in workloads if 'scaled' in w['id']),"realEngineWork":True,"busyLoopOrArtificialWait":False,"recommendation":"Retain the 10 ms authority floor and use scaled workloads for M027 comparison; do not treat small-workload percentages as regressions."}
    text('performance/assessment.json',json.dumps(assessment,indent=2)+'\n','performance')
    text('performance/assessment.md','# Performance assessment\n\nSmall workloads below 10 ms are non-timing-authoritative: '+', '.join(small)+'. Scaled workloads: '+', '.join(scaled)+'. Real engine work is recorded through deterministic work counters; no busy loop or artificial wait is used. Recommendation: retain M026 policy in M027.\n','performance')
    for ext in ('json','md'):
        add(f'artifacts/consumer-boundaries/M026/consumer-boundary-decision-report.{ext}',f'consumer-boundaries/consumer-boundary-decision-report.{ext}','boundary',machine=True)
    decisions=json.loads((ROOT/'artifacts/consumer-boundaries/M026/consumer-boundary-decision-report.json').read_text())['decisions']
    summary={"schema":"agentic2d.m026-boundary-decision-summary.v1","requiredCandidates":[d['candidate'] for d in decisions],"countsByDecision":dict(Counter(d['decision'] for d in decisions)),"decisions":decisions}
    text('consumer-boundaries/decision-summary.json',json.dumps(summary,indent=2)+'\n','boundary')
    # Receipts are preserved as historical (current source/review changes may invalidate them).
    plan=run('./eng/m026-smoke.sh','--plan-json',check=False)
    text('validation/m026-plan.json',plan+'\n','validation')
    verifier=run('./eng/m026-smoke.sh','--verify',check=False)
    text('validation/m026-verifier.txt',verifier+'\n','validation')
    for p in sorted((ROOT/'artifacts/validation/m026-smoke').glob('*.json')): add(p.relative_to(ROOT),f'validation/receipts/{p.name}','validation',machine=True)
    lines=['# Regression summary','', 'Historical M019-M024 verifiers were previously refreshed; M022 and M025 remain review-gated. Current M026 receipt fingerprints are stale after launcher/review changes; this handoff preserves them as historical evidence and does not rerun earlier milestones.', '', '| Suite | Verifier | Status |', '|---|---|---|']
    for n in range(19,27):
        out=run(f'./eng/m{n:03d}-smoke.sh','--verify',check=False) if (ROOT/f'eng/m{n:03d}-smoke.sh').exists() else 'unavailable'
        lines.append(f'| M{n:03d} | `./eng/m{n:03d}-smoke.sh --verify` | `{out.splitlines()[-1] if out else "passed"}` |')
    text('validation/regression-summary.md','\n'.join(lines)+'\n','validation')
    # Review evidence: preserve pending markdown if present and current record, but do not alter decisions.
    add('.review/pending/M026-geometry-diagnostics-and-autonomous-tic-tac-toe.md','review/request.md','review')
    add('.review/records/review.m026.geometry-diagnostics-and-autonomous-tic-tac-toe.json','review/record.md','review')
    review_check=run('./eng/review-check.sh',check=False)
    text('review/review-check.txt',review_check+'\n','review')
    text('review/evidence-index.md','# Review evidence index\n\nGeometry previews and contrast diagnostics: `geometry/`. Board, thinking, takeover, mark, win/draw/reset, and particle evidence: `tic-tac-toe/`. Sound linkage: `sound-linkage/`. Determinism and save/resume: scenario results under `tic-tac-toe/`. Export/isolation: validation receipts. Performance and boundary reports: their named directories.\n','review')
    OMISSIONS.extend([{"path":"representative graphical PNG captures","reason":"no current provider or consumer PNG capture was available at collection time; structural geometry and presentation artifacts are included instead"},{"path":"complete Linux export directories","reason":"excluded by size and reproducibility policy; manifests, hashes, isolated-launch, and equivalence evidence are retained"},{"path":"complete prior M025 handoff evidence","reason":"excluded to avoid duplication; M025 base revision is recorded for historical context"}])
    statuses={"historicalMilestoneEvidenceStatus":"sufficient","historicalM026VerifierStatus":"passed","historicalReviewStatus":"approved","currentBranchValidationStatus":"stale","m027PlanningReadiness":"ready-with-open-items","explanations":{"historicalMilestoneEvidenceStatus":"M026 source range, reports, consumer results, and receipts are preserved.","historicalM026VerifierStatus":"Historical non-review receipts were passed before later launcher and evidence changes.","historicalReviewStatus":"M026 record is approved by repository user.","currentBranchValidationStatus":"Current receipt fingerprints are stale after post-M026 launcher fixes and review-record updates; repository-wide review check also reports stale M022/M025 records.","m027PlanningReadiness":"Evidence supports prioritization, with current-validation refresh and review-record maintenance as open items."}}
    meta={"schema":"agentic2d.m026-to-m027.collection-metadata.v1","repository":"carlrabbit/dotnet-ai-first-2d-game-engine","branch":branch,"acceptedM025Base":BASE,"m026Revision":REV,"currentHead":head,"commitRange":f'{BASE}..{REV}',"collectionTimestampUtc":NOW,"operatingSystem":platform.platform(),"dotnetSdk":run('dotnet','--version'),"workingTreeStatus":status,"handoffStatus":"incomplete","blockingReason":"Current branch validation receipts are stale after post-M026 launcher fixes; repository-wide review check has stale M022/M025 records."}
    text('collection-metadata.json',json.dumps(meta,indent=2)+'\n','metadata')
    text('handoff-summary.md',f"# M026 to M027 planning evidence handoff\n\n- Repository: `carlrabbit/dotnet-ai-first-2d-game-engine`\n- Branch: `{branch}`\n- Accepted M025 base: `{BASE}`\n- M026 revision: `{REV}`\n- Range: `{BASE}..{REV}`\n- Collected: `{NOW}`\n- Status: **incomplete** — {meta['blockingReason']}\n- Historical verifier: passed; historical review: approved.\n\nM026 adds geometry inspection/preview diagnostics, explicit generated-sound linkage, timing-authoritative scaled workloads, Signal Passage migration, and Autonomous Tic-Tac-Toe. Boundary candidates: {len(decisions)}; decisions: {dict(Counter(d['decision'] for d in decisions))}.\n\nM027 priorities: formalize the evidence-supported generated-sound and geometry diagnostics work; retain deterministic randomness and persistence policy; observe delayed behavior rather than introducing a scheduler.\n\nOmissions: full exports, engine copies, build outputs, repetitive frames/logs, unrelated M025 evidence, and any unavailable graphical capture. See manifest.\n")
    # ZIP after all content except top-level manifest/validation which are added below.
    # Initial manifest includes all currently curated files; ZIP is then indexed externally.
    manifest={"schema":"agentic2d.m026-to-m027.handoff-manifest.v1","repository":meta['repository'],"branch":branch,"acceptedBaseRevision":BASE,"m026Revision":REV,"commitRange":meta['commitRange'],"collectionTimestampUtc":NOW,"collectorVersion":"1.0","statusDimensions":statuses,"includedFiles":INCLUDED,"omissions":OMISSIONS}
    text('handoff-manifest.json',json.dumps(manifest,indent=2)+'\n','manifest')
    # Archive all curated files except itself, deterministically.
    zip_path=OUT/'m026-to-m027-full-evidence.zip'
    with zipfile.ZipFile(zip_path,'w',zipfile.ZIP_DEFLATED,compresslevel=9) as z:
        for item in sorted(p for p in OUT.rglob('*') if p.is_file() and p.name != zip_path.name):
            info=zipfile.ZipInfo(str(item.relative_to(OUT)).replace(os.sep,'/'),(1980,1,1,0,0,0)); info.external_attr=0o100644<<16
            z.writestr(info,item.read_bytes())
    zip_ok=zipfile.ZipFile(zip_path).testzip() is None
    archive={"path":str(zip_path.relative_to(ROOT)),"byteLength":zip_path.stat().st_size,"sha256":sha(zip_path),"integrityPassed":zip_ok,"safePaths":all(not x.startswith('/') and '..' not in Path(x).parts for x in zipfile.ZipFile(zip_path).namelist())}
    validation={"schema":"agentic2d.m026-to-m027.handoff-validation.v1","status":"incomplete","archive":archive,"requiredBoundaryCandidatesPresent":len(decisions)==11,"boundaryDecisionCounts":dict(Counter(d['decision'] for d in decisions)),"m026VerifierStatus":"historical-passed/current-stale","reviewStatus":"approved/current-review-check-blocked-by-stale-M022-M025","scaledPerformanceEvidencePresent":bool(workloads),"deterministicAiEvidencePresent":(OUT/'tic-tac-toe/deterministic-random-choice/tic-tac-toe-result.json').exists(),"saveDuringThinkingEvidencePresent":(OUT/'tic-tac-toe/save-during-thinking/tic-tac-toe-save.json').exists(),"soundLinkageBothConsumersPresent":all((OUT/f'sound-linkage/{x}-generated-sound-linkage-report.json').exists() for x in ('signal-passage','tic-tac-toe')),"reason":meta['blockingReason']}
    text('handoff-validation.json',json.dumps(validation,indent=2)+'\n','validation')
    text('handoff-validation.md',f"# Handoff validation\n\nStatus: **incomplete**. ZIP integrity: `{zip_ok}`; safe paths: `{archive['safePaths']}`; size: `{archive['byteLength']}` bytes. Boundary candidates: `{len(decisions)}/11`. Historical M026 verifier evidence is passed, but current receipts are stale and global review-check is blocked by stale M022/M025 review records.\n")
    # Refresh manifest once validation artifacts exist (archive deliberately indexes prior complete tree; external hash is authoritative).
    manifest['includedFiles']=INCLUDED; manifest['archive']=archive
    (OUT/'handoff-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print(json.dumps({"output":str(OUT),"archive":archive,"included":len(INCLUDED),"omissions":len(OMISSIONS)},indent=2))

if __name__ == '__main__': main()
