"""Co21-only proposal. Requires separately authorized, authenticated launcher."""
from contextlib import contextmanager
import ctypes
from ctypes import wintypes
import hashlib
import json
import msvcrt
import os
from pathlib import Path
import stat
import subprocess
import sys
import types
import zipfile

ROOT = Path("C:/Users/gdg34/.codex/worktrees/h1-f4-recovery/Dungeon-Lord")
PREP = ROOT / "TestResults/h1-f4-20261027-co21"
CAMPAIGN = ROOT / "TestResults/qualification-supervisor/25da41d400e343f3b53c560cbe1cfd52"
BASE = ROOT / "TestResults/qualification-supervisor-reservation"
MARKER = BASE / "active.json"
LOCK = BASE / "execution.lock"
RECEIPT = ROOT / "TestResults/h1-f4-co21-attempt03.jsonl"
HEAD = "ffc46ff1c9105cea71fe18599caeae22e26d0b10"
BRANCH = "codex/h1-f4-recovery"
APPROVAL = "owner-approved-co21-25da41d400e343f3b53c560cbe1cfd52-attempt03"
GIT = Path("C:/Program Files/Git/cmd/git.EXE")
PS = Path("C:/Windows/System32/WindowsPowerShell/v1.0/powershell.EXE")
PYTHON = Path("C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe")
REVIEW = PREP / "preservation-review01/review.json"
ORIGINAL = PREP / "original-inputs.zip"
BEFORE = REVIEW.with_name("changed-inputs.zip")
TARGETS = (
    "Assets/Settings/UniversalRP.asset",
    "Assets/UniversalRenderPipelineGlobalSettings.asset",
    "Assets/_Project/Data/Bootstrap/build_config.json",
    "Assets/_Project/Scripts/BuildTools/DevelopmentBuildUtility.cs",
    "ProjectSettings/EditorBuildSettings.asset",
    "ProjectSettings/GraphicsSettings.asset",
    "ProjectSettings/ProjectSettings.asset",
    "ProjectSettings/ShaderGraphSettings.asset",
    "ProjectSettings/UnityConnectSettings.asset",
)
HELPERS = {
    "qualification_inputs": "34d3ff45cf7512e21f242bc0e4ede876e5c8b68eddba853ad9b1e69192270c92",
    "qualification_windows": "234bce5f0ca8c7087bb999c603cfbb3d87f0eb4499501a1644dcbbabf3104db7",
    "qualification_supervisor": "957b83f2b60be9790e4ec952c457372d93516ee9ee34b6c56990c9ed713e996a",
    "qualify_h1_f4": "cafa31627594654b4bc0d34a88c5a4bc3ad4db813a7451e7aa25a4756f86fbc0",
}
EXECUTABLES = {
    GIT: "81ef35ae005ca9318018d18e3327578ce939fb99feaad6b2d7c8ab15f3de8db5",
    PS: "d8f182560210d43baaf767d482ad1dc1207372522e91413b9d5e515a875e552d",
    PYTHON: "f8b45d63047da9d9adf09892e4cc4317b859e86a93a478cc9616bc69d3b176e5",
}
AUTHORITY = {
    REVIEW: "1c1709f6081b33b06af5a782f216b01ba75f2498524ae0c924bb804d57d58442",
    ORIGINAL: "7d0f74b6f6ae54d9708283b9c1e558ddb03d823cef01f247cd0d58f1ee3542b7",
    BEFORE: "616ec93a5063ddc5f52bbb25d9a24fe0c56d68fa43329655db3fffba1aa5ad31",
    PREP / "original-inputs.json": "f7a065f05e5631c8144abbcb3ded59e16e272d466f62f2c3af54d08e8be6812f",
    PREP / "qualified-inputs.json": "bd3b43b4660eea0c21d68566baa636fcc8952c48d07fa57fcb494c5f08e3c7e2",
    PREP / "protected.json": "5964841d0f5ccad1e286871e7aed5f9ad32351d4e46d6ad82dfe183a2950c8da",
    PREP / "prior-evidence-before.json": "39c3312f60fd3a8c2737261442406f956beaefba48b7b13970b57357f2ed370f",
    PREP / "plan-native01.json": "d2a2d1b85f9e7da24359cbf7539f001feba628c4248b6037ac84ba4dbefd0855",
    PREP / "plan.json": "899e403ca17f36d5662ec28aba1d68f858f1f35251573d64b759de36464643c9",
    PREP / "inventory.json": "9dac241c4dcc91e356a8c22a1227ddefdedea3dc0573ae4e5bca60f0bf860c30",
    CAMPAIGN / "campaign.json": "4f6d363033b81789ce1ace7599f5c0aac0e2adf0b59aa839727377c057149c03",
    ROOT / "TestResults/h1-f4-20261027-co20/owner-acceptance01.json": "85380aa267064600425fc6920b8324c3de2dc69bb069f469aab4dbf27c84caf4",
    ROOT / "TestResults/h1-f4-20261027-co20/independent-completion01.json": "9b6087f0865592610894a8b7d7103284583acabdc82a21f372d69db658ce5fdd",
}
MARKER_SHA = "88c2b5b4f09f93b3c46cfd1bb3c53632d7788fc35baf7fffa20544656cdadc78"
LOCK_ID = (10958016848787023586, 7318349395614852, 1793102930750678200, 0)


def require(condition, reason):
    if not condition:
        raise ValueError(reason)


def digest(data):
    return hashlib.sha256(data).hexdigest()


def checked_bytes(path, expected):
    data = path.read_bytes()
    require(digest(data) == expected, "Hash mismatch: " + str(path))
    return data


def load_helpers():
    sources = {}
    for name, expected in HELPERS.items():
        path = ROOT / "Tools/Presentation" / (name + ".py")
        sources[name] = (path, checked_bytes(path, expected))
    require(not set(sources).intersection(sys.modules), "Helper already loaded")
    for name, (path, data) in sources.items():
        module = types.ModuleType(name)
        module.__file__ = str(path)
        sys.modules[name] = module
        exec(compile(data, str(path), "exec"), module.__dict__)
    return tuple(sys.modules[name] for name in HELPERS)


def git(root, *args):
    return subprocess.check_output([str(GIT), "--no-optional-locks", *args],
                                   cwd=root, text=True).strip()


def assert_no_workload(review, windows):
    probe = r"""
$ErrorActionPreference='Stop'
$ids = @(25008,16076,23096,35624,2464)
Get-CimInstance Win32_Process | Where-Object {
    $_.ParentProcessId -in $ids -or (
        ($_.Name -eq 'Unity.exe' -or $_.Name -like 'Dungeon Lord*.exe') -and
        $_.CommandLine -notin @('"C:\Program Files\Unity Hub\resources\unity.exe" serve',
        '"C:\Users\gdg34\AppData\Local\Unity\bin\unity.exe" --internal-auth-broker-serve')
    )
} | Select-Object ProcessId,ParentProcessId,CreationDate,ExecutablePath | ConvertTo-Json -Compress
"""
    result = subprocess.run([str(PS), "-NoProfile", "-NonInteractive", "-Command", probe],
                            capture_output=True, text=True, check=False)
    require(result.returncode == 0 and not result.stderr.strip() and not result.stdout.strip(),
            "Process query failed or found a workload: " + repr(result))
    for row in review["absent_recorded_processes"]:
        require(windows.identity(row["pid"]) != row, "Owned process remains live")
    for name in review["absent_named_jobs"]:
        handle = windows.open_job(4, False, name)
        if handle:
            windows.close(handle)
            raise ValueError("Owned Job remains live: " + name)
        require(ctypes.get_last_error() == 2, "Job absence not established: " + name)


def lock_id(info):
    return (info.st_dev, info.st_ino, info.st_ctime_ns, info.st_size)


@contextmanager
def protected_lease(lock, expected_identity, lease_factory, windows):
    create_file = windows.api("CreateFileW", [
        wintypes.LPCWSTR, wintypes.DWORD, wintypes.DWORD, wintypes.LPVOID,
        wintypes.DWORD, wintypes.DWORD, wintypes.HANDLE,
    ], wintypes.HANDLE)
    # OPEN_EXISTING, read access, read/write sharing only, open reparse point itself.
    handle = create_file(str(lock), 0x80000000, 3, None, 3, 0x00200000, None)
    if handle == ctypes.c_void_p(-1).value:
        raise ctypes.WinError(ctypes.get_last_error())
    try:
        fd = msvcrt.open_osfhandle(handle, os.O_RDONLY | os.O_BINARY)
    except BaseException:
        windows.close(handle)
        raise
    try:
        opened = os.fstat(fd)
        require(stat.S_ISREG(opened.st_mode) and lock_id(opened) == expected_identity,
                "Protected execution-lock identity changed")
        require(lock_id(lock.stat()) == expected_identity, "Execution-lock path changed")
        lease = lease_factory()
        try:
            with lease as acquired:
                require(lock_id(os.fstat(acquired.stream.fileno())) == expected_identity,
                        "Supervisor Lease opened a different execution lock")
                yield acquired
        finally:
            # The supervisor normally closes this stream. Cover exceptional seek/unlock paths.
            stream = getattr(lease, "stream", None)
            if stream is not None and not stream.closed:
                stream.close()
    finally:
        os.close(fd)


def run_restoration(receipt, targets, marker, expected_release, lease_factory, preflight, postflight):
    """Ordered write boundary. Callbacks are fixed by main for the live proposal."""
    with receipt.open("x", encoding="utf-8", newline="\n") as stream:
        def record(event, **data):
            stream.write(json.dumps(dict(event=event, **data), sort_keys=True) + "\n")
            stream.flush()
            os.fsync(stream.fileno())

        phase = "preflight"
        record("attempt_opened", campaign=str(CAMPAIGN), source=HEAD)
        try:
            with lease_factory() as lease:
                originals = preflight(lease)
                record("restoration_started")
                phase = "restoration"
                for name, target, temporary, expected_current in targets:
                    require(digest(target.read_bytes()) == expected_current,
                            "Current target changed: " + name)
                    record("replacement_attempted", target=name, temporary=str(temporary))
                    payload = originals[name]
                    with temporary.open("xb") as output:
                        output.write(payload)
                        output.flush()
                        os.fsync(output.fileno())
                    os.replace(temporary, target)
                    require(target.read_bytes() == payload, "Replacement readback mismatch: " + name)
                    record("replacement_verified", target=name, sha256=digest(payload))
                record("all_replacements_completed", count=len(targets))
                phase = "postflight"
                postflight(lease)
                record("complete_original_map_verified")
                phase = "release"
                record("release_attempted")
                lease.release(CAMPAIGN)
                require(json.loads(marker.read_bytes()) == expected_release, "Unexpected release marker")
                record("release_verified", marker_sha256=digest(marker.read_bytes()))
            phase = "receipt"
            record("final_receipt_completed")
        except BaseException as error:
            try:
                marker_bytes = marker.read_bytes()
                observed = {"sha256": digest(marker_bytes), "bytes_hex": marker_bytes.hex()}
            except OSError as probe_error:
                observed = {"unavailable": str(probe_error)}
            record("preflight_rejected" if phase == "preflight" else
                   "partial_restoration_failure" if phase == "restoration" else
                   "final_completion_uncertain",
                   phase=phase, marker=observed, error=repr(error))
            raise


def main():
    require(sys.argv[1:] == [APPROVAL] and sys.flags.isolated and sys.flags.no_site
            and not sys.flags.optimize and Path.cwd().resolve() == ROOT,
            "Wrong invocation, worktree or Python flags")
    require(Path(sys.executable).resolve() == PYTHON, "Unexpected Python executable")
    for path, expected in EXECUTABLES.items():
        checked_bytes(path, expected)
    inputs, windows, supervisor, q = load_helpers()
    # Use the pinned absolute executable for the existing protected-owner HEAD probe.
    q.subprocess = types.SimpleNamespace(check_output=lambda args, **kwargs:
        subprocess.check_output([str(GIT), "--no-optional-locks", *args[1:]], **kwargs))
    _, _, save = q.identities("co21")

    def authorities():
        result = {}
        for path, expected in AUTHORITY.items():
            inputs.no_redirect(path)
            result[path] = checked_bytes(path, expected)
        return result

    blobs = authorities()
    review = json.loads(blobs[REVIEW])
    original = json.loads(blobs[PREP / "original-inputs.json"])
    expected = dict(original)
    require(len(original) == 1494 and set(review["changed_inputs"]) == set(TARGETS)
            and review["campaign"] == str(CAMPAIGN) and review["source"] == HEAD,
            "Invalid pinned archive scope")
    expected.update({name: review["changed_inputs"][name]["current"] for name in TARGETS})
    targets = [(name, ROOT / name, (ROOT / name).with_name(Path(name).name + ".co21-restore03-tmp"),
                expected[name]) for name in TARGETS]
    original_status = {f" M {name}" for name in TARGETS}

    def verify_common(lease=None):
        require(git(ROOT, "rev-parse", "--show-toplevel").casefold() == ROOT.as_posix().casefold()
                and git(ROOT, "symbolic-ref", "--short", "HEAD") == BRANCH
                and git(ROOT, "rev-parse", "HEAD") == HEAD, "Unexpected Git source")
        for path in (MARKER, LOCK, RECEIPT):
            inputs.no_redirect(path)
        require(lock_id(LOCK.stat()) == LOCK_ID and
                (lease is None or lock_id(os.fstat(lease.stream.fileno())) == LOCK_ID),
                "Execution-lock identity changed")
        require(json.loads(checked_bytes(MARKER, MARKER_SHA)) == {"campaign": str(CAMPAIGN)},
                "Unexpected reservation")
        legacy = ROOT / "Temp/qualification-supervisor/active.json"
        inputs.no_redirect(legacy)
        require(not legacy.exists() or supervisor.read(legacy).get("campaign") is None,
                "Legacy reservation present")
        assert_no_workload(review, windows)

    def verify_preserved():
        require(q.tree(CAMPAIGN) == review["campaign_files"] and
                q.tree(PREP / "windows-development") == review["build_files"], "Co21 evidence changed")
        require(q.protected(save) == json.loads(blobs[PREP / "protected.json"]),
                "Protected owner, saves or historical evidence changed")
        require(q.tree(save) == {}, "Co21 save namespace changed")
        prior = json.loads(blobs[PREP / "prior-evidence-before.json"])
        require(len(prior) == 401, "Prior inventory count changed")
        for name, files in prior.items():
            require(q.tree(ROOT / name) == files, "Prior evidence changed: " + name)

    def preflight(lease):
        verify_common(lease)
        require(authorities() == blobs, "Recovery authorities changed")
        status = subprocess.check_output([str(GIT), "--no-optional-locks", "status", "--porcelain",
                                          "--untracked-files=all"], cwd=ROOT, text=True).splitlines()
        require(set(status) == original_status and q.inventory(ROOT) == expected, "Input map changed")
        verify_preserved()
        for _, target, temporary, _ in targets:
            inputs.no_redirect(target)
            inputs.no_redirect(temporary)
            require(not temporary.exists(), "Temporary path already exists")
        from io import BytesIO
        with zipfile.ZipFile(BytesIO(blobs[BEFORE])) as archive:
            require(sorted(archive.namelist()) == sorted(TARGETS), "Unexpected preserved-copy scope")
            for name in TARGETS:
                require(digest(archive.read(name)) == expected[name], "Preserved copy changed: " + name)
        with zipfile.ZipFile(BytesIO(blobs[ORIGINAL])) as archive:
            require(sorted(archive.namelist()) == sorted(original), "Unexpected original archive scope")
            files = {}
            for name, expected_hash in original.items():
                data = archive.read(name)
                require(digest(data) == expected_hash, "Archived input changed: " + name)
                if name in TARGETS:
                    files[name] = data
        verify_common(lease)
        return files

    def postflight(lease):
        require(q.inventory(ROOT) == original and not git(ROOT, "status", "--porcelain"),
                "Original input map or tracked source not restored")
        require(authorities() == blobs, "Recovery authorities changed")
        verify_preserved()
        verify_common(lease)

    verify_common()
    run_restoration(RECEIPT, targets, MARKER, {"campaign": None, "previous": str(CAMPAIGN)},
                    lambda: protected_lease(LOCK, LOCK_ID, lambda: supervisor.Lease(ROOT), windows),
                    preflight, postflight)


if __name__ == "__main__":
    main()
