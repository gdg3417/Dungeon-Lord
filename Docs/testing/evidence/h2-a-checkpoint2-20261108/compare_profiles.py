"""Offline reduction of unchanged production profile artifacts; no new counters."""
import collections
import hashlib
import json
from pathlib import Path
import statistics
import sys

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def dist(values):
    values = sorted(values)
    return {'mean': statistics.mean(values), 'p95': values[int((len(values)-1)*.95)], 'max': values[-1]}

def summarize(path):
    data = read(path)
    assert data['Passed'] and data['CompletedRuns'] == 68, path
    frames = data['Frames']
    thread = data['ThreadAllocationCounterAvailable']
    allocation = data['FrameAllocationCounterAvailable'] and any(f['FrameAllocatedBytes'] > 0 for f in frames)
    main = all(f['MainThreadNanoseconds'] >= 0 for f in frames)
    groups = {'all': frames, 'work': [f for f in frames if f['Category'] in ('admission', 'event', 'settlement', 'passive')]}
    for category in sorted(set(f['Category'] for f in frames)):
        groups[category] = [f for f in frames if f['Category'] == category]
    result = {'source': str(path), 'sha256': digest(path), 'passed': data['Passed'], 'completed': data['CompletedRuns'],
        'dimensions': [data['ScreenWidth'], data['ScreenHeight'], data['TextSize']], 'frames': len(frames),
        'thread_allocations_supported': thread, 'frame_allocations_responsive': allocation, 'main_thread_supported': main,
        'categories': {}, 'areas': {name: {'calls': sum(f['Calls'][i] for f in frames),
            'inclusive_ms': sum(f['Ticks'][i] for f in frames)*1000/data['StopwatchFrequency'],
            'allocated_bytes': sum(f['Bytes'][i] for f in frames) if thread else None}
            for i, name in enumerate(data['Areas'])},
        'memory': {key: {'first': frames[0][key], 'last': frames[-1][key], 'min': min(f[key] for f in frames), 'max': max(f[key] for f in frames)}
            for key in ('ManagedBytes', 'NativeBytes', 'Collections')},
        'gc_increments': sum(max(0, b['Collections'] - a['Collections']) for a, b in zip(frames, frames[1:])),
        'max_party_count': max(f['PartyCount'] for f in frames), 'max_checkpoint_bytes': max(f['CheckpointBytes'] for f in frames)}
    for name, rows in groups.items():
        if not rows: continue
        result['categories'][name] = {'count': len(rows), 'update_ms': dist([f['UpdateMs'] for f in rows]),
            'frame_ms': dist([f['FrameMs'] for f in rows]),
            'main_thread_ms': dist([f['MainThreadNanoseconds']/1e6 for f in rows]) if main else None,
            'frame_allocated_bytes': dist([f['FrameAllocatedBytes'] for f in rows]) if allocation else None,
            'update_allocated_bytes': dist([f['Allocated'] for f in rows]) if thread else None}
    return data, result

def compare(main, pr):
    a, left = summarize(main)
    b, right = summarize(pr)
    keys = ('Frame', 'Sequence', 'Ordinal', 'Category', 'PartyCount', 'CheckpointBytes')
    sig = lambda d: [tuple(f[k] for k in keys) for f in d['Frames']]
    sa, sb = sig(a), sig(b)
    differences = [{'index': i, 'main': x, 'pr': y} for i, (x, y) in enumerate(zip(sa, sb)) if x != y]
    result = {'main': left, 'pr': right, 'comparability': {'signature_keys': keys,
        'exact_frame_workload_equal': sa == sb, 'mismatches': len(differences), 'first_mismatches': differences[:5],
        'same_dimensions': left['dimensions'] == right['dimensions'], 'same_area_order': a['Areas'] == b['Areas']},
        'limitations': ['One ordered Editor campaign per source; no statistically established causal speedup.',
            'Full frame includes real UI/render and Editor scheduling; inclusive scopes overlap.',
            'Frame allocation recorder responds but has no isolated live-allocation calibration in this existing harness.',
            'Global heap includes Editor, report sample arrays and unrelated activity; not a retained byte budget.',
            'Thread allocation counter must respond to the existing live allocation probe; false is unsupported, not zero allocation.']}
    return result

if __name__ == '__main__':
    main, pr, destination = map(Path, sys.argv[1:4])
    assert not destination.exists(), 'Preserve prior analyses'
    result = compare(main, pr)
    destination.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'output': str(destination), 'comparability': result['comparability']}))
