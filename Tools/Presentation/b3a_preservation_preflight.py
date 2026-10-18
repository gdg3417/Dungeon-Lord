"""Read-only launch gate for the frozen B3A qualification campaign.

This deliberately cannot bless new source, local overrides, preservation losses,
or a second use of an output namespace. A new campaign needs explicit review.
"""
from pathlib import Path
import hashlib
import io
import json
import re
import subprocess
import zipfile

import qualify_b3a as q

REVIEWED = '36da2f80ef31febbd01e63d8ee5c5ffa2fd38532'
QUALIFIED = 'b5502291e9da3d229bbcc3d152f8bd56954d84eb'
EVIDENCE = 'Docs/testing/evidence/living-dungeon-b3a/'
STAGES = {'focused', 'regression', 'full-edit', 'full-play', 'integration-play', 'build'}
FIXTURE_REPLACEMENTS = [('bootstrap_overlay_', 'bootstrap_overlay_b3a_20261017_'),
                        ('run_sim_test_', 'run_sim_test_b3a_20261017_'),
                        ('mvp_canonical_journey_test.json', 'mvp_canonical_journey_b3a_20261017.json')]


def digest(data):
    return hashlib.sha256(data).hexdigest()


def git(root, *args):
    return subprocess.check_output(['git', *args], cwd=root)


def anchors(root):
    # Trust reviewed committed evidence, not a caller-editable preparation stamp.
    snapshot = json.loads(git(root, 'show', REVIEWED+':'+EVIDENCE+'source-applicability.json'))
    if snapshot['finalCommitAtCollection'] != QUALIFIED:
        raise ValueError('Unexpected qualified source anchor')
    if git(root, 'diff', '--name-only', QUALIFIED, 'HEAD', '--', *q.inputs.INPUT_ROOTS).strip():
        raise ValueError('Unity inputs changed since the qualified commit; a new review is required')
    with zipfile.ZipFile(io.BytesIO(git(root, 'show', REVIEWED+':'+EVIDENCE+'qualification-reports.zip'))) as archive:
        baselines = {name: archive.read(name) for name in ['baseline.json', 'historical-evidence-before.json']}
    return {r['path']: r['sha256'] for r in snapshot['unityInputs']}, baselines


def unused(path):
    q.inputs.no_redirect(path)
    if path.exists():
        raise ValueError('Destination already used; preserve it and review a fresh campaign: '+str(path))


def validate(root, out, save, stage, attempt):
    """Return proof metadata, with no writes and no Unity/process launch authority."""
    if stage not in STAGES:
        raise ValueError('Unsupported/closed stage '+stage+'; historical baseline reproduction is preserved, not rerunnable')
    if not re.fullmatch('[a-z0-9-]+', attempt):
        raise ValueError('Unsafe attempt identifier')
    for path in [root, out, save]:
        q.inputs.no_redirect(path)
    if out != root/'TestResults/living-dungeon-b3a-20261017':
        raise ValueError('Unexpected qualification output namespace')
    if (out/'substitutions-restored.json').exists():
        raise ValueError('Isolation was restored/inactive. Do not launch Unity. A separately reviewed fresh preparation is required; preserve this closed campaign.')
    required = ['baseline.json', 'historical-evidence-before.json', 'root-inputs-before.zip',
                'test-fixtures-before.zip', 'fixture-substitutions.json']
    for name in required:
        path = out/name
        q.inputs.no_redirect(path)
        if not path.is_file():
            raise ValueError('Isolation preparation incomplete: missing '+str(path)+'; do not run Unity or install substitutions in preflight')
    source_hashes, baseline_bytes = anchors(root)
    for name, data in baseline_bytes.items():
        if (out/name).read_bytes() != data:
            raise ValueError('Preservation baseline differs from reviewed immutable evidence: '+name)
    baseline = json.loads(baseline_bytes['baseline.json'])
    history = json.loads(baseline_bytes['historical-evidence-before.json'])
    if not baseline['protected'] or not history or not source_hashes:
        raise ValueError('Empty preservation/source baseline')
    original_hashes = {r['path']: r['sha256'] for r in baseline['inputs']}
    expected = dict(source_hashes)
    transformed = [q.inputs.CONFIG, q.inputs.SERVICE]+q.OUTPUT_TESTS+q.FIXTURE_TESTS
    with zipfile.ZipFile(out/'root-inputs-before.zip') as archive:
        for name in transformed+q.MUTABLE[2:]:
            original = archive.read(name)
            if digest(original) != original_hashes[name] or digest(original) != source_hashes[name]:
                raise ValueError('Unreviewed source/override backup: '+name)
            if name in transformed:
                if name in [q.inputs.CONFIG, q.inputs.SERVICE]:
                    # Avoid changing the shared helper's process-global namespace.
                    if name == q.inputs.CONFIG:
                        config = json.loads(original.decode('utf-8-sig'))
                        if config['save']['fileName'] != 'save_primary.json':
                            raise ValueError('Unexpected original save namespace')
                        config['save']['fileName'] = q.NAMESPACE
                        prepared = json.dumps(config, indent=2).encode('utf-8')
                    else:
                        prepared = original.decode('utf-8-sig').replace('save_primary.json', q.NAMESPACE).encode('utf-8')
                else:
                    text = original.decode('utf-8-sig')
                    if name in q.OUTPUT_TESTS:
                        if 'TestResults/' not in text:
                            raise ValueError('Expected screenshot/evidence output helper changed: '+name)
                        text = text.replace('TestResults/', 'TestResults/living-dungeon-b3a-20261017/outputs/')
                    else:
                        for old, new in FIXTURE_REPLACEMENTS:
                            text = text.replace(old, new)
                    prepared = text.encode('utf-8')
                expected[name] = digest(prepared)
    # Includes ignored/untracked files and metadata: no normalized or partial match.
    actual = q.inputs.target_inventory(root)
    if actual != set(expected):
        raise ValueError('Unexpected/missing Unity inputs: '+str(sorted(actual.symmetric_difference(expected))))
    q.require_metadata(root)
    for name, expected_hash in sorted(expected.items()):
        if q.sha(root/name) != expected_hash:
            raise ValueError('Prepared-source identity mismatch: '+name+'; verify save, shared screenshot, static fixture substitutions and allowed overrides')
    fixtures = json.loads((out/'fixture-substitutions.json').read_bytes())
    if {r['path'] for r in fixtures} != set(q.FIXTURE_TESTS):
        raise ValueError('Static fixture preparation record is incomplete')
    with zipfile.ZipFile(out/'test-fixtures-before.zip') as archive:
        for row in baseline['protected']:
            if row['fixture'] and digest(archive.read(Path(row['path']).name)) != row['sha256']:
                raise ValueError('Static fixture preservation backup differs: '+row['path'])
    # No allowlist silently accepts the known incident: it blocks actual execution.
    for row in baseline['protected']+history:
        path = Path(row['path'])
        q.inputs.no_redirect(path)
        if not path.is_file() or q.sha(path) != row['sha256']:
            raise ValueError('Preservation mismatch; recover exact original or obtain explicit owner review before a new campaign: '+str(path))
    for suffix in ['.xml', '.log', '-command.json']:
        unused(out/(attempt+'-'+stage+suffix))
    # These helpers have fixed directories. Even an empty previous directory is
    # not an unused destination. Existing captures must never be cleaned here.
    for path in [out/'outputs', out/'windows']:
        unused(path)
    if stage == 'build':
        unused(root/'Builds/Development/Windows-living-dungeon-b3a-20261017')
    if save.exists():
        prefixes = [Path(q.NAMESPACE).stem, 'bootstrap_overlay_b3a_20261017_',
                    'run_sim_test_b3a_20261017_', 'mvp_canonical_journey_b3a_20261017']
        for path in save.iterdir():
            if any(path.name.startswith(prefix) for prefix in prefixes):
                raise ValueError('Isolated save/fixture namespace already used: '+str(path))
    return dict(approved=True, stage=stage, attempt=attempt, namespace=q.NAMESPACE,
                exactUnityInputs=len(expected), protected=len(baseline['protected']),
                historicalEvidence=len(history), readOnly=True, qualifiedCommit=QUALIFIED)
