"""Windows process ownership primitives. Standard library only; no PID-only kills."""
import ctypes as C
from ctypes import wintypes as W
import json
import os
import subprocess
import time

if os.name != 'nt':
    raise OSError('Qualification supervisor requires Windows')

k = C.WinDLL('kernel32', use_last_error=True)


def api(name, args, result=W.BOOL):
    fn = getattr(k, name)
    fn.argtypes, fn.restype = args, result
    return fn


close = api('CloseHandle', [W.HANDLE])
open_process = api('OpenProcess', [W.DWORD, W.BOOL, W.DWORD], W.HANDLE)
get_times = api('GetProcessTimes', [W.HANDLE] + [C.POINTER(W.FILETIME)] * 4)
query_image = api('QueryFullProcessImageNameW', [W.HANDLE, W.DWORD, W.LPWSTR, C.POINTER(W.DWORD)])
wait_many = api('WaitForMultipleObjects', [W.DWORD, C.POINTER(W.HANDLE), W.BOOL, W.DWORD], W.DWORD)
create_job = api('CreateJobObjectW', [W.LPVOID, W.LPCWSTR], W.HANDLE)
open_job = api('OpenJobObjectW', [W.DWORD, W.BOOL, W.LPCWSTR], W.HANDLE)
assign_job = api('AssignProcessToJobObject', [W.HANDLE, W.HANDLE])
terminate_job = api('TerminateJobObject', [W.HANDLE, W.UINT])
query_job = api('QueryInformationJobObject', [W.HANDLE, C.c_int, W.LPVOID, W.DWORD, W.LPVOID])
set_job = api('SetInformationJobObject', [W.HANDLE, C.c_int, W.LPVOID, W.DWORD])
create_port = api('CreateIoCompletionPort', [W.HANDLE, W.HANDLE, C.c_size_t, W.DWORD], W.HANDLE)
get_completion = api('GetQueuedCompletionStatus',
                     [W.HANDLE, C.POINTER(W.DWORD), C.POINTER(C.c_size_t), C.POINTER(W.LPVOID), W.DWORD])
create_event = api('CreateEventW', [W.LPVOID, W.BOOL, W.BOOL, W.LPCWSTR], W.HANDLE)
open_event = api('OpenEventW', [W.DWORD, W.BOOL, W.LPCWSTR], W.HANDLE)
set_event = api('SetEvent', [W.HANDLE])


def checked(value):
    if not value:
        raise C.WinError(C.get_last_error())
    return value


def identity(pid):
    """FILETIME creation ticks and actual image, using one live process handle."""
    handle = open_process(0x1000 | 0x100000, False, pid)
    if not handle:
        error = C.get_last_error()
        if error == 87:  # no such PID; access denied is uncertainty, never absence
            return None
        raise C.WinError(error)
    try:
        handles = (W.HANDLE * 1)(handle)
        if wait_many(1, handles, False, 0) == 0:
            return None
        times = [W.FILETIME() for _ in range(4)]
        checked(get_times(handle, *[C.byref(t) for t in times]))
        image = C.create_unicode_buffer(32768)
        size = W.DWORD(len(image))
        if not query_image(handle, 0, image, C.byref(size)):
            if wait_many(1, handles, False, 0) == 0: return None
            raise C.WinError(C.get_last_error())
        # A signaled process is already dead, even if another handle retains it.
        if wait_many(1, handles, False, 0) == 0:
            return None
        return dict(pid=pid, created=times[0].dwHighDateTime << 32 | times[0].dwLowDateTime,
                    executable=os.path.normcase(os.path.realpath(image.value)))
    finally:
        close(handle)


def matches(record):
    return bool(record) and identity(record['pid']) == record


class Accounting(C.Structure):
    _fields_ = [('times', C.c_longlong * 4), ('page_faults', W.DWORD),
                ('total', W.DWORD), ('active', W.DWORD), ('terminated', W.DWORD)]


class Association(C.Structure):
    _fields_ = [('key', W.LPVOID), ('port', W.HANDLE)]


class Job:
    def __init__(self, name):
        C.set_last_error(0)
        self.handle = checked(create_job(None, name))
        if C.get_last_error() == 183:
            close(self.handle)
            raise ValueError('Owned job name already exists')
        self.port = checked(create_port(W.HANDLE(-1), None, 0, 1))
        association = Association(None, self.port)
        checked(set_job(self.handle, 7, C.byref(association), C.sizeof(association)))

    def assign(self, process):
        checked(assign_job(self.handle, int(process._handle)))

    def active(self):
        data = Accounting()
        checked(query_job(self.handle, 1, C.byref(data), C.sizeof(data), None))
        return data.active

    def stop(self):
        checked(terminate_job(self.handle, 125))

    def wait_empty(self, seconds=5):
        # Job notifications close the small gap between process signaling and job
        # accounting cleanup. Wait on the OS completion port; never sample in a loop.
        deadline = time.monotonic() + seconds
        while True:
            message, key, overlapped = W.DWORD(), C.c_size_t(), W.LPVOID()
            remaining = max(0, int((deadline - time.monotonic()) * 1000))
            if not get_completion(self.port, C.byref(message), C.byref(key), C.byref(overlapped), remaining):
                if C.get_last_error() == 258: return False
                raise C.WinError(C.get_last_error())
            if message.value == 4: return self.active() == 0

    def close(self):
        close(self.port)
        close(self.handle)


def wait(process, event, seconds):
    handles = (W.HANDLE * 2)(int(process._handle), event)
    code = wait_many(2, handles, False, int(seconds * 1000))
    if code == 0:
        return 'exit'
    if code == 1:
        return 'cancel'
    if code == 258:
        return 'timeout'
    raise C.WinError(C.get_last_error())


def signal(name):
    handle = checked(open_event(2, False, name))
    try:
        checked(set_event(handle))
    finally:
        close(handle)


def assert_no_unity():
    # Same broker exclusions as qualify_b3b1.run. Query failures fail closed.
    script = r"""$ErrorActionPreference='Stop'
Get-CimInstance Win32_Process | Where-Object {
($_.Name -eq 'Unity.exe' -or $_.Name -like 'Dungeon Lord*.exe') -and
$_.CommandLine -notin @('"C:\Program Files\Unity Hub\resources\unity.exe" serve',
'"C:\Users\gdg34\AppData\Local\Unity\bin\unity.exe" --internal-auth-broker-serve')
} | Select-Object ProcessId,CreationDate,ExecutablePath | ConvertTo-Json -Compress
"""
    result = subprocess.run(['powershell', '-NoProfile', '-NonInteractive', '-Command', script],
                            capture_output=True, text=True, check=True)
    if result.stderr.strip() or result.stdout.strip():
        raise ValueError('Conflicting Unity/player or process inspection failure: ' + result.stdout.strip())
