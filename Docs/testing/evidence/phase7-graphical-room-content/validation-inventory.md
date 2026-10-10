# Validation project inventory before reuse

Baseline: `5932a9fe0b7cec87c81f09e8e7d619df62c53101`, merged PR #230. Read-only inventory on 2026-10-10. No Unity.exe process was present in the unrestricted process inventory. Sizes are approximate complete directory sizes, including caches and outputs.

| Absolute project path | GiB | Library | UnityLockfile | Reuse assessment |
|---|---:|---|---|---|
| C:/Dev/Dungeon-Lord/Temp/room-construction-validation | 2.33 | Yes | Yes | Stale or unresolved lock; do not use |
| C:/Dev/Dungeon-Lord/Temp/room-construction-validation-bounds-186ba37 | 2.29 | Yes | No | Older input trees; not selected |
| C:/Dev/Dungeon-Lord/Temp/ui-composition-final-validation | 2.47 | Yes | No | Older presentation copy; not selected |
| C:/Dev/Dungeon-Lord/Temp/ui-composition-floorhud-validation | 2.51 | Yes | No | Selected after strict preflight: zero target-only inputs |
| C:/Dev/Dungeon-Lord/Temp/ui-composition-overlay-validation | 2.49 | Yes | No | Older presentation copy; not selected |
| C:/Dev/Dungeon-Lord/Temp/ui-composition-review-validation | 2.44 | Yes | No | Older presentation copy; not selected |
| C:/Dev/Dungeon-Lord/Temp/ui-composition-uat-validation | 2.45 | Yes | No | Older UAT copy; not selected |
| C:/Dev/Dungeon-Lord/Temp/ui-composition-validation | 2.03 | Yes | No | Older presentation copy; not selected |

Selected copy retains its imported Library. The existing strict helper restores the four source roots with only its reviewed save filename substitutions. It neither deletes files nor ignores unexpected metadata. Preflight report: `TestResults/room-content-reuse-preflight.json`; expected source changes/missing new files are distinct from target-only stale inputs. Only one Unity invocation may use this checkout at a time.

Disposable namespace remains `phase7-ui-composition-validation-01a11d08.json`; scene fixtures override it with their unique test namespaces before GameRoot.Start. The owner project is never a validation execution target. Primary save, primary-prefix sidecars and owner source settings are audited by read-only hashes before/after qualification.

Cleanup candidates for owner review: the seven unselected project copies above (approximately 16.5 GiB combined), outputs/logs/test XML and historical duplicate builds under their output directories, and redundant historical build directories under C:/Dev/Dungeon-Lord/Builds. Library caches provide reuse value. Historical evidence may refer to these outputs. No deletion or quarantine is authorized or performed by this packet.
