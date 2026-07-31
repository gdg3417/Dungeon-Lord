# GD65B validation evidence

> **Final verdict:** PASS — GD65B5 implementation and required owner validation passed at c5eefae61e9bf3b7bf0a200e343f383f0122743b. PR #186 is ready for final review and merge. Merging PR #186 closes GD65B and unblocks GD66.

## Commit and environment

| Item | Evidence |
|---|---|
| Implementation PR | PR #186 |
| Exact tested commit SHA | `c5eefae61e9bf3b7bf0a200e343f383f0122743b` |
| Unity version | `6000.3.2f1` |
| Operating system and build target | Windows; StandaloneWindows64 development build |
| Date of validation | `2026-07-30` |

## Automated Unity results

The complete suites supersede separate execution of the six named GD65B test classes; the six classes were not claimed as separately run. Exact final passing XML filenames were not retained.

| Suite | Discovered | Passed | Failed | Skipped | Inconclusive | XML |
|---|---:|---:|---:|---:|---:|---|
| Complete EditMode | 202 | 202 | 0 | 0 | 0 | Exact final filename not retained |
| Complete PlayMode | 1307 | 1307 | 0 | 0 | 0 | Exact final filename not retained |

The intentional red log from `ExportCommandFailureThrowsWithStableOrderedDiagnostics` was expected through `LogAssert.Expect` and was not a test failure.

### Preserved EditMode correction history

1. `184/201` passed; 17 failures occurred because a fixture attempted additive scene creation while an unsaved untitled scene was open.
2. `184/201` passed; 17 failures occurred because `SceneManager.CreateScene` is unavailable for this EditMode use.
3. `186/202` passed; 16 failures occurred because synthetically created preview-scene `GameRoot` components were not discovered through the tested preview-scene root path.
4. The fixture was changed to mutate isolated previews of the canonical Bootstrap scene and use a narrow root-list validation seam.
5. Final result: `202/202` passed.

## Play Mode and editor gameplay

- The complete PlayMode suite passed: `1307/1307`.
- The game started normally in Editor Play Mode.
- Existing dungeon placement worked.
- Adventurer runs worked.
- No production spatial runtime activation occurred.

## Deterministic production export

The approved Editor export menu was run repeatedly. Repeated exports produced no byte changes, and these SHA-256 values matched across both retained export sets and the post-build set:

| File | SHA-256 |
|---|---|
| `content_manifest.json` | `FB48BBBC3975827A295188818C7140BD91447E6D401C3A8CA1554A5402089902` |
| `dungeon_spatial_content.json` | `C0ABFB31515B37047E8700AE888450A2329A9AEBC415692FFCD99AE06D37D17A` |
| `string_table_en.json` | `37ECE030181BB51FF887A75AA41A2293BDECE9EED8D292EF6EC40E48BEEF3262` |
| `validation_limits.json` | `9D279717C15B9A1397D86261B8FD95DF835CB2688FFA35B20E3C5D2773FA47C1` |

- No generated diff occurred after the repeated export.
- `ContentAuthoring/DungeonSpatial/` was unchanged.
- `validation_limits.json` was unchanged.
- No unexpected generated file remained.
- Post-build hashes matched the retained export hashes.

## Recovery evidence

The final complete EditMode suite covered the ordered recovery-before-validation and fail-closed recovery responsibilities. The build gate invoked recovery without regeneration, and the subsequently validated installed set remained byte-identical during successful validation. No recovery defect remained in the final suite.

## Build rejection evidence

The reversible invalid enabled-scene state was:

```text
Assets/_Project/Scenes/Bootstrap.unity
Assets/Scenes/SampleScene.unity
```

The exact rejection was:

```text
BuildFailedException: [ProductionSpatialBuildGate:InvalidBuildSceneComposition] Assets/_Project/Scenes/Bootstrap.unity,Assets/Scenes/SampleScene.unity
```

This demonstrates that the `BuildPlayerProcessor` callback validated the actual scene list supplied through `BuildPlayerContext.BuildPlayerOptions.scenes`. SampleScene was removed afterward, and the Bootstrap-only scene configuration was restored. No invalid rejection-test state was committed.

## Successful Windows development build

| Item | Evidence |
|---|---|
| Repository menu | `Dungeon Lord/Build/Windows 64-bit Development Build` |
| Output | `Builds/Development/Windows/Dungeon Lord.exe` |
| Report | `Builds/Development/Windows/build-report.json` |
| Result | Passed |
| Included scene | Only `Assets/_Project/Scenes/Bootstrap.unity` |

The production-spatial build gate accepted the valid build, and generated production hashes remained unchanged. No unsupplied warning totals or report fields are asserted here.

## Standalone lifecycle

The Windows development build passed startup, dungeon placement or modification, an adventurer run, player-facing result and explanation, save, full application close, reopen, placement and progression persistence, and an additional adventurer run after reopening. No save migration was required; save schema remained 6.

## Command-line export classification

**Superseded by stronger final GD65B5 validation and accepted as a documented non-blocking limitation.**

The owner explicitly declined an additional manual Unity batch-mode export because it was unnecessary for this PR's acceptance. This is not a missing GD65B requirement. The basis is:

- The public command-line method still exists.
- `ExportAdaptersExposeApprovedMenuAndCommandLineThroughSharedBoundary` passed in the final complete EditMode suite.
- The menu and command-line methods invoke the same `ExecuteProductionSpatialContent` boundary.
- Repeated real Editor-menu exports were byte-identical.
- The successful player build changed no production file.
- The owner explicitly accepted omission of another batch-mode invocation.

No manual batch-mode process or exit code 0 is claimed.

## File integrity, authority, and scope

- Production spatial catalog remains inactive.
- No runtime graph consumer was introduced.
- Existing ordered two-room placement/runtime/save authority remains unchanged.
- No migration or GD66 implementation was added.
- Bootstrap remained the canonical sole player-build scene.
- No tuning values were added.
- Localization ownership remains in data.
- Authoring source, validation limits, Bootstrap, scenes, project settings, and save data were unchanged by successful validation.
- Save schema remains 6.
- No generated rejection-test state was committed.

## PR #184 placeholder reconciliation

No PR #184 placeholder remains unresolved.

| Historical gap | Classification | Final reconciliation |
|---|---|---|
| Exact final PlayMode count | Superseded by stronger final GD65B5 validation | `1307/1307` at `c5eefae61e9bf3b7bf0a200e343f383f0122743b` |
| Missing `dungeon_spatial_content.json` hash | Directly satisfied | `C0ABFB31515B37047E8700AE888450A2329A9AEBC415692FFCD99AE06D37D17A` |
| Command-line `NoByteChangesNeeded` and exit-code placeholder | Superseded by stronger final GD65B5 validation and accepted owner waiver | Shared public boundary test passed; repeated Editor-menu exports and successful build preserved bytes; omission of another batch invocation was explicitly accepted. No batch process or exit code is claimed. |

## Final verdict

**PASS — GD65B5 implementation and required owner validation passed at c5eefae61e9bf3b7bf0a200e343f383f0122743b. PR #186 is ready for final review and merge. Merging PR #186 closes GD65B and unblocks GD66.**
