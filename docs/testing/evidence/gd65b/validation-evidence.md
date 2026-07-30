# GD65B validation evidence

> **Final verdict:** Pending owner Unity validation. GD65B is not yet complete. GD66 remains blocked.

This is the owner-completed closeout record for GD65B5. Every result below is **pending**; this implementation record does not claim that Unity tests or player builds passed.

## Commit and environment

| Item | Evidence |
|---|---|
| Implementation branch | Pending owner entry |
| Exact tested commit SHA | Pending owner entry |
| Unity version | `6000.3.2f1` |
| Operating system and build target | Pending owner entry |
| Date of validation | Pending owner entry |

## Automated Unity results

Record total, passed, failed, skipped, and inconclusive counts, XML result paths, and retained failure/correction history for each run.

| Responsibility / suite | Counts | XML path | Result and failure history |
|---|---|---|---|
| Focused `ProductionSpatialContentBuildGateTests` | Pending | Pending | Pending |
| `ProductionSpatialContentWorkloadLimitTests` | Pending | Pending | Pending |
| `ProductionSpatialContentExportTests` | Pending | Pending | Pending |
| `ProductionSpatialContentRecoveryTests` | Pending | Pending | Pending |
| `ProductionSpatialContentLoadingTests` | Pending | Pending | Pending |
| `ProductionSpatialContentScalabilityTests` | Pending | Pending | Pending |
| All six named GD65B responsibilities | Pending | Pending | Pending |
| Complete EditMode suite | Pending | Pending | Pending |
| Complete PlayMode suite | Pending | Pending | Pending |

## Deterministic export evidence

- First production export result: Pending.
- Second identical export result: Pending.
- Generated file hashes after each export: Pending.
- No generated diff on the second export: Pending confirmation.
- Authoring source and validation limits unchanged: Pending confirmation.
- No unexpected files generated: Pending confirmation.

## Recovery evidence

- Recovery scenario and result: Pending.
- Proof recovery occurred before validation: Pending.
- Proof no publication/regeneration occurred in the build gate: Pending.
- Proof the recovered installed set was subsequently validated: Pending.
- Failure and correction history for recovery defects: Pending; preserve all runs here.

## Build acceptance evidence

- Windows 64-bit development build result: Pending.
- Build report path and summary: Pending.
- Standalone startup: Pending.
- Existing dungeon placement and adventurer run: Pending.
- Save, close, reopen, and persistence: Pending.
- Player log path: Pending.
- Production spatial catalog remains inactive: Pending confirmation.

## Build rejection evidence

- Exact reversible local defect: Pending.
- Expected stable reason code: Pending.
- Actual stable reason code and build rejection result: Pending.
- Invalid state restored and branch returned clean: Pending confirmation.
- Successful build or preflight rerun after restoration: Pending.

## File integrity

| File | Before SHA-256 | After SHA-256 |
|---|---|---|
| `content_manifest.json` | Pending | Pending |
| `dungeon_spatial_content.json` | Pending | Pending |
| `string_table_en.json` | Pending | Pending |
| `validation_limits.json` | Pending | Pending |

- `ContentAuthoring/DungeonSpatial/` unchanged: Pending confirmation.
- `Bootstrap.unity` unchanged: Pending confirmation.
- Save schema remains 6: Pending confirmation.
- No scene or project setting dirtied: Pending confirmation.
- No invalid rejection-test state committed: Pending confirmation.

## Authority and scope confirmation

- Production spatial catalog remains inactive: Pending confirmation.
- No runtime graph consumer added: Pending confirmation.
- Existing runtime and save authority unchanged: Pending confirmation.
- No migration or GD66 implementation added: Pending confirmation.
- No new tuning values invented: Pending confirmation.
- No localization ownership moved into code: Pending confirmation.

## PR #184 placeholder reconciliation

The planning record retains two PR #184 gaps: the exact final PlayMode count was not retained, and two evidence placeholders remained unverified. Each relevant underlying placeholder must be listed by the owner and classified without deleting its history.

| PR #184 placeholder | Reconciliation status | Final evidence or stronger GD65B5 validation |
|---|---|---|
| Exact final PlayMode count / associated placeholder | Still unresolved | Pending |
| Remaining unverified evidence placeholder 1 | Still unresolved | Pending identification and evidence |
| Remaining unverified evidence placeholder 2 | Still unresolved | Pending identification and evidence |

Allowed final classifications are: directly satisfied by supplied final evidence at the final tested SHA; superseded by an explicitly identified stronger final GD65B5 validation; or still unresolved. GD65B cannot close while a required responsibility is unresolved.

## Final verdict

**Pending owner Unity validation. GD65B is not yet complete. GD66 remains blocked.**
