"""Fail closed on missing, changed or unexpected disposable Unity/content inputs."""
from pathlib import Path
import json, subprocess, sys
from qualification_inputs import compare

if __name__ == '__main__':
    root = Path(__file__).resolve().parents[2]
    result = compare(root, sys.argv[1])
    result.update(branch=subprocess.check_output(['git','branch','--show-current'],cwd=root).decode().strip(),
                  source_head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=root).decode().strip())
    output = root / sys.argv[2]
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(len(result['files']), 'inputs;', len(result['failures']), 'failures;', len(result['unexpected']), 'target-only')
    for failure in result['failures']: print(failure)
    sys.exit(bool(result['failures']))
