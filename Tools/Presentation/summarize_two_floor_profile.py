"""Summarize finished matched workloads, including overlapping control phases."""
from pathlib import Path
import json
import statistics
import sys

source, output = map(Path, sys.argv[1:3])
if output.exists():
    raise ValueError('Preserve previous summaries')
data = json.loads(source.read_text(encoding='utf-8-sig'))
frames = data['Frames']
areas = data['Areas']


def distribution(values):
    if not values:
        return None
    values = sorted(values)
    return dict(count=len(values), mean=statistics.mean(values),
                p95=values[int((len(values) - 1) * .95)], maximum=values[-1])


def summarize(selected):
    return dict(updateMs=distribution([f['UpdateMs'] for f in selected]),
                frameMs=distribution([f['FrameMs'] for f in selected]),
                mainThreadMs=distribution([f['MainThreadNanoseconds'] / 1e6 for f in selected if f['MainThreadNanoseconds'] >= 0]),
                threadAllocatedBytes=distribution([f['Allocated'] for f in selected]) if data['ThreadAllocationCounterAvailable'] else None,
                frameAllocatedBytes=distribution([f['FrameAllocatedBytes'] for f in selected if f['FrameAllocatedBytes'] >= 0]) if data['FrameAllocationCounterAvailable'] else None)


result = dict(source=str(source), passed=data['Passed'], completedRuns=data['CompletedRuns'],
              display={key: data.get(key) for key in ('ScreenWidth', 'ScreenHeight', 'TextSize')},
              failure=data.get('Failure'), categories={}, taggedFrames={}, areas={},
              threadAllocationCounterAvailable=data['ThreadAllocationCounterAvailable'],
              frameAllocationCounterAvailable=data['FrameAllocationCounterAvailable'])
for category in sorted({f['Category'] for f in frames}):
    result['categories'][category] = summarize([f for f in frames if f['Category'] == category])
for index, area in enumerate(areas):
    selected = [f for f in frames if f['Calls'][index] > 0]
    result['areas'][area] = dict(calls=sum(f['Calls'][index] for f in frames),
        inclusiveMs=sum(f['Ticks'][index] for f in frames) * 1000 / data['StopwatchFrequency'],
        inclusiveAllocatedBytes=sum(f['Bytes'][index] for f in frames) if data['ThreadAllocationCounterAvailable'] else None)
    if area in ('TransferBlocked', 'TransferReleased', 'RecoveryApplied', 'Settlement', 'LifetimeProbe'):
        result['taggedFrames'][area] = summarize(selected)
result['workload'] = dict(frames=len(frames), concurrentFrames=sum(f.get('PartyCount', 0) == 2 for f in frames),
    maximumRetainedCheckpointBytes=max((f.get('CheckpointBytes', 0) for f in frames), default=None),
    maximumObservedFrameMs=max((f['FrameMs'] for f in frames), default=None))
result['limitations'] = ['Scope measurements are inclusive and nested; tagged phases may overlap.',
    'Null allocations mean unavailable, never zero.',
    'Opt-in injected clock preserves configured logical timing; Windows results do not establish mobile performance.',
    'Complete-save sizes and reopen durations are recorded separately from post-exit files and launch records.']
with output.open('x', encoding='utf-8') as stream:
    json.dump(result, stream, indent=2)
print(json.dumps(result, indent=2))
