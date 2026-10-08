"""Exact qualification inputs. No deletion; stale files require a new disposable.

Docs/Tools are evidence/tooling, not Unity inputs. Library, Logs, Builds,
UserSettings and TestResults are outputs outside the four input roots. Every
file inside an input root counts, including generated metadata: no exceptions.
"""
from pathlib import Path, PurePosixPath
import hashlib, json, os, re, stat, subprocess

INPUT_ROOTS = ('Assets', 'Packages', 'ProjectSettings', 'ContentAuthoring')
NAMESPACE = 'phase7-ui-composition-validation-01a11d08.json'
CONFIG = 'Assets/_Project/Data/Bootstrap/build_config.json'
SERVICE = 'Assets/_Project/Scripts/Services/SaveService.cs'

def no_redirect(path):
    for node in (path, *path.parents):
        if node.is_symlink(): raise ValueError('Symlink forbidden: ' + str(node))
        try: info = node.lstat()
        except FileNotFoundError: continue
        if getattr(info, 'st_file_attributes', 0) & stat.FILE_ATTRIBUTE_REPARSE_POINT:
            raise ValueError('Reparse point forbidden: ' + str(node))

def target_path(root, name):
    if not re.fullmatch(r'ui-composition-[A-Za-z0-9][A-Za-z0-9_-]*', name):
        raise ValueError('Expected one disposable ui-composition-* directory under repository Temp')
    root = Path(root).absolute()
    target = root / 'Temp' / name
    no_redirect(target)
    if target.resolve().parent != (root / 'Temp').resolve(): raise ValueError('Unsafe disposable path')
    if target.exists() and not target.is_dir(): raise ValueError('Target is not a directory')
    return target

def source_inventory(root):
    names = set()
    for flags in ([], ['--others', '--exclude-standard']):
        data = subprocess.check_output(['git', 'ls-files', '-z', *flags, '--', *INPUT_ROOTS], cwd=root)
        names.update(filter(None, data.decode('utf-8').split('\0')))
    for name in names:
        parts = PurePosixPath(name).parts
        if not parts or parts[0] not in INPUT_ROOTS or '..' in parts or '\\' in name or ':' in name:
            raise ValueError('Unsafe source input: ' + name)
        path = root / name
        no_redirect(path)
        if not path.is_file(): raise ValueError('Missing required source input: ' + name)
    if not names: raise ValueError('Empty source inventory')
    return sorted(names)

def target_inventory(target):
    def unreadable(error): raise error
    names = set()
    for name in INPUT_ROOTS:
        base = target / name
        no_redirect(base)
        if not base.exists(): continue
        if not base.is_dir(): raise ValueError('Input root is not directory: ' + str(base))
        for folder, dirs, files in os.walk(base, followlinks=False, onerror=unreadable):
            for child in dirs + files: no_redirect(Path(folder) / child)
            for child in files:
                path = Path(folder) / child
                if not path.is_file(): raise ValueError('Nonregular input: ' + str(path))
                names.add(path.relative_to(target).as_posix())
    return names

def transformed(name, data):
    if name == CONFIG:
        config = json.loads(data.decode('utf-8-sig'))
        if config['save']['fileName'] != 'save_primary.json': raise ValueError('Unexpected canonical save filename')
        config['save']['fileName'] = NAMESPACE
        return json.dumps(config, indent=2).encode('utf-8')
    if name == SERVICE:
        text = data.decode('utf-8-sig')
        if 'save_primary.json' not in text or NAMESPACE in text: raise ValueError('Unexpected SaveService fallback')
        return text.replace('save_primary.json', NAMESPACE).encode('utf-8')
    return data

def normalized(name, data):
    if name == CONFIG: return json.dumps(json.loads(data.decode('utf-8-sig')), sort_keys=True).encode()
    if b'\x00' not in data: return data.removeprefix(b'\xef\xbb\xbf').replace(b'\r\n', b'\n')
    return data

def compare(root, name):
    root = Path(root).absolute()
    target = target_path(root, name)
    expected = source_inventory(root)
    actual = target_inventory(target)
    unexpected = sorted(actual - set(expected))
    failures = [p + ': unexpected target-only input' for p in unexpected]
    rows = []
    for path in expected:
        source = (root/path).read_bytes()
        left = normalized(path, transformed(path, source))
        if path not in actual:
            failures.append(path + ': missing')
            rows.append(dict(path=path, equal=False, source_sha256=hashlib.sha256(source).hexdigest()))
            continue
        right = normalized(path, (target/path).read_bytes())
        equal = left == right
        if not equal: failures.append(path + ': mismatch')
        rows.append(dict(path=path, source_sha256=hashlib.sha256(source).hexdigest(),
                         expected_qualified_sha256=hashlib.sha256(left).hexdigest(),
                         qualified_sha256=hashlib.sha256(right).hexdigest(), equal=equal))
    return dict(validation_directory=str(target), input_roots=INPUT_ROOTS, files=rows,
                expected_count=len(expected), actual_count=len(actual), unexpected=unexpected, failures=failures,
                permitted_transformations=['exact config and SaveService save filename substitution: '+NAMESPACE,
                                           'JSON config formatting; text CRLF/LF and UTF-8 BOM normalization'])

def prepare(root, name):
    root = Path(root).absolute()
    target = target_path(root, name)
    names = source_inventory(root)
    extras = target_inventory(target) - set(names)
    if extras: raise ValueError('Stale target inputs; use a new disposable directory: ' + ', '.join(sorted(extras)))
    # Preflight every source transformation before writing. No cleanup of any kind.
    inputs = {name: transformed(name, (root/name).read_bytes()) for name in names}
    settings = inputs['ProjectSettings/ProjectSettings.asset'].decode('utf-8-sig')
    if 'companyName: gdg3417' not in settings or 'productName: Dungeon Lord' not in settings:
        raise ValueError('Unexpected application identity')
    for path, data in inputs.items():
        dest = target/path
        no_redirect(dest)
        dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_bytes(data)
    result = compare(root, name)
    if result['failures']: raise ValueError('Prepared input comparison failed: '+str(result['failures']))
    return result
