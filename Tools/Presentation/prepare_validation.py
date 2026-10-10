"""Prepare exact disposable inputs; reject stale files without deleting anything."""
from pathlib import Path
import sys
from qualification_inputs import prepare

if __name__ == '__main__':
    result = prepare(Path(__file__).resolve().parents[2], sys.argv[1])
    print(len(result['files']), 'exact inputs prepared; namespace isolated; no target-only files')
