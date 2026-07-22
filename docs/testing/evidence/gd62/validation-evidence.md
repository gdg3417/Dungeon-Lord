# GD62 validation evidence

## Final validation baseline

| Item | Result |
|---|---|
| Final tested code-and-test baseline | `398567cfec4344ddfaad092eb5db9366b4c48c1d` |
| Environment | macOS, Unity 6000.3.2f1 |
| Evidence update | Documentation-only; the evidence commit is identifiable through Git history and is intentionally not self-referenced here |

## Automated validation

| Check | Result |
|---|---|
| `DungeonSpatialContractTests` | Passed |
| `FloorLayoutValidatorTests` | Passed |
| Complete Unity EditMode suite | Passed; no exact passing test count is recorded |
| Test scripts | All passed |

The initial test run had two failures: a Unity JSON `null`/`string.Empty` mismatch and an incorrect positional connection-limit test argument. Both failures were corrected through PR #163, after which the final focused and complete suites passed.

## Manual and regression validation

| Check | Result |
|---|---|
| Clean Bootstrap reset | Passed |
| One-room gameplay regression | Passed |
| Two-room gameplay regression | Passed; Room 2 remained ordered and the route reported `Full route cleared` and `Depth reached: Room 2` |
| Diagnostic pages | Passed |
| Game-view resolution checks | Passed |
| Save Now | Passed |
| Save-close-reopen-load-rerun | Passed; room order and assignments persisted |
| Save lifecycle integrity | No migration prompt, schema error, duplicate state, missing assignment, or inactive spatial-system activation occurred |
| Save schema | Remains version 6 |

The First Dungeon Contract output `Path built: incomplete` versus the guided output `Path complete: Yes` is accepted as a pre-existing, non-GD62 observation.

## Validation classification and scope

| Area | Result |
|---|---|
| Unit | Passed |
| SIT | Passed for the inactive standalone domain and the existing Bootstrap regression |
| UAT | Not applicable; GD62 introduced no player-facing behavior |
| Save lifecycle | Passed on macOS |
| Fun validation | Not applicable |
| Runtime and player-facing scope | No runtime spatial authority or player-facing GD62 behavior was introduced |
| Windows standalone | Not required because the PR contains no active runtime integration, platform-specific code, save-schema change, scene change, prefab change, build change, or player-facing spatial behavior |

Automation for GD62 establishes contract correctness only. It does not prove that future spatial editing is understandable or fun.
