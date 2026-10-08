"""Overlay tracked source into the disposable project; isolate every fallback boot."""
from pathlib import Path
import json, shutil, subprocess, sys

root = Path(__file__).resolve().parents[2]
target = root / 'Temp' / (sys.argv[1] if len(sys.argv)>1 else 'ui-composition-validation')
assert target.resolve().is_relative_to(root / 'Temp') and (target / 'Assets').is_dir()
files = subprocess.check_output(['git', 'ls-files', '-z', 'Assets', 'Packages', 'ProjectSettings', 'ContentAuthoring', 'Docs'], cwd=root).decode().split('\0')
untracked = subprocess.check_output(['git', 'ls-files', '--others', '--exclude-standard', '-z', 'Assets'], cwd=root).decode().split('\0')
for name in files + untracked:
    if not name or not (root / name).is_file(): continue
    dest = target / name
    dest.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(root / name, dest)
config_path = target / 'Assets/_Project/Data/Bootstrap/build_config.json'
config = json.loads(config_path.read_text(encoding='utf-8-sig'))
config['save']['fileName'] = 'phase7-ui-composition-validation-01a11d08.json'
config_path.write_text(json.dumps(config, indent=2), encoding='utf-8')
service = target / 'Assets/_Project/Scripts/Services/SaveService.cs'
service.write_text(service.read_text(encoding='utf-8-sig').replace('save_primary.json', config['save']['fileName']), encoding='utf-8')
settings = target / 'ProjectSettings/ProjectSettings.asset'
text = settings.read_text(encoding='utf-8-sig')
# Build utility requires canonical identity; filename isolation persists in the player.
assert 'companyName: gdg3417' in text and 'productName: Dungeon Lord' in text
print('Validation fallback isolated:', config['save']['fileName'])
