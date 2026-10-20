"""Summarize raw event-frame evidence; never read a running player's save."""
import json, statistics, sys
from pathlib import Path

source, destination = map(Path, sys.argv[1:3])
if destination.exists():
    raise ValueError('Preserve existing summary')
data = json.loads(source.read_text(encoding='utf-8-sig'))
groups = {}
for frame in data['Frames']:
    category = frame['Category']
    if category == 'between-events' and frame['Ordinal'] < 0:
        category = 'idle-eligible'
    groups.setdefault(category, []).append(frame)

def distribution(values):
    ordered = sorted(values)
    return dict(mean=statistics.mean(ordered), p50=ordered[(len(ordered)-1)//2],
                p95=ordered[int((len(ordered)-1)*.95)], p99=ordered[int((len(ordered)-1)*.99)], maximum=ordered[-1])

result = dict(source=str(source), passed=data['Passed'], completedRuns=data['CompletedRuns'],
              failure=data.get('Failure'), categories={})
for category, frames in groups.items():
    result['categories'][category] = dict(count=len(frames),
        updateMs=distribution([f['UpdateMs'] for f in frames]), frameMs=distribution([f['FrameMs'] for f in frames]),
        mainThreadMs=distribution([f['MainThreadNanoseconds']/1e6 for f in frames]),
        updateAllocatedBytes=distribution([f['Allocated'] for f in frames]),
        areas={name: dict(calls=sum(f['Calls'][i] for f in frames),
            inclusiveMs=sum(f['Ticks'][i] for f in frames)*1000/data['StopwatchFrequency'],
            inclusiveAllocatedBytes=sum(f['Bytes'][i] for f in frames)) for i, name in enumerate(data['Areas'])})
frames = data['Frames']
result['memory'] = {key: dict(first=frames[0][key], last=frames[-1][key], minimum=min(f[key] for f in frames), maximum=max(f[key] for f in frames))
    for key in ('ManagedBytes', 'NativeBytes', 'Collections')}
result['limitations'] = ['Opt-in development instrumentation; scope times/allocations are inclusive and nested.',
    'The retained raw sample list contributes bounded qualification-only memory growth.',
    'Clock acceleration changes qualification wall time, not configured gameplay intervals or work limits.',
    'Main Thread recorder availability must be checked; -1 means unavailable.']
destination.write_text(json.dumps(result, indent=2), encoding='utf-8')
print(json.dumps(result, indent=2))
