# Combat C2 — persistent monster injury and reserve-backed recovery

Recommended Codex configuration: GPT-6.1 Sol, High

Task classification: Complex

Reason: Approved contracts require coordinated persistence, combat, economy, replay and Windows qualification changes.
Fallback if unavailable: GPT-6 Sol, High.

## Baseline and scope

Starting GitHub/local main: `0624bc0bbbd34a8ceff26d764d7f35c3b464d8bc`, merged Combat C1 PR #239. GitHub main was fetched before creating the single branch `combat-c2-monster-continuity`. The initial checkout was clean. No owner environment is used for automated execution.

The owner's C2 task is the approval for schema 15 and the 66/100 readiness, initial level 1, one mana per HP, living-first recovery and global reserve contracts. Earlier documentation that calls these choices unapproved is historical. C0 combat tuning, B1 executable/checkpoint bytes, C1 presentation patterns and B2's dormant production boundary remain in force.

Implementation and final qualification are recorded below. This packet requires independent ChatGPT review, resolution of blocking findings and proportionate owner Editor/Windows UAT before merge approval. Automated passes alone do not establish merge readiness.

## Exact schema and migration

Schema 15 appends a ninth primary canonical owner after the eight schema-14 owners:

```json
"monsterContinuity":{"FormatVersion":1,"Records":[{"AssignmentId":"existing.assignment.id","Level":1,"Health":8,"RevivalRequired":false}]}
```

Records are sorted by ordinal AssignmentId and must match exactly the owned monster identities in canonical room assignments and returned custody. Duplicate/missing/orphan records, aliases, unknown fields, fractional/invalid HP, unsupported levels, inconsistent revival and incompatible health configuration fail validation. Floor, room, position, option, sequence and custody stay exclusively with the existing authorities. Maximum HP and reserve are derived, never saved as independent writable balances or values.

The new explicit 14→15 step strictly validates its frozen source, rejects continuity-owner collisions, copies historical owners/extensions, changes the schema token and appends healthy level-one records for every owned monster, including custody. It does not infer injuries from history or spend mana. Native creation appends an empty owner. Existing 7→14 migration steps remain sequential; the coordinator then performs 14→15, validates and recoverably persists through the existing complete-save authority. Recovery preflight explicitly recognizes frozen schema 14 as well as current schema 15. Supported B2 state/checkpoint data is retained, including non-dormant qualification states; unsupported checkpoint data fails closed.

The schema-13 resource remains unchanged. A new schema-14 resource preserves the previous production profile byte for byte. Schema-specific budget selection uses the frozen profile for each earlier boundary.

| Budget | Schema 13 | Schema 14 | Schema 15 |
| --- | ---: | ---: | ---: |
| Complete serialized bytes | 262,144 | 655,360 | 1,376,256 |
| Parsed nodes | 8,192 | 9,216 | 10,240 |
| Collection records | 4,096 | 4,352 | 5,376 |
| Ordinary raw array elements | 128 | 128 | 128 |

Only schema 15's `primary.monsterContinuity.Records` array can use the existing configured spatial-record bound of 165. Other arrays, including returned custody, keep their 128-element bound; this does not expand the historical custody horizon.

The explicit schema-15 raw input/scan caps are 2,752,512 / 5,505,024 bytes of bounded scan work; serialized string characters are capped at 1,376,256. Copied-source values remain capped at 524,288 bytes, unknown members at 64 / 131,072 bytes, and materialized spatial tiles at 64. These unrelated limits are not expanded for continuity.

Schema 15 adds 720,896 bytes to the complete envelope. The independent worst-case continuity envelope uses the unchanged 165 spatial-record bound and 4,096-byte raw IDs. Its measured owner size is in the focused test output; geometry also consumes spatial records, so this is a conservative owner envelope, not a claim that 165 deployed monsters fit current production geometry. Nodes/collections have room for the owner object, record array and five nodes/four fields per record. Raw input/scan budgets remain proportional; string budgets cover the added IDs. Frozen checkpoint bytes and parser limits are not enlarged. B2's own base-save/checkpoint/replay quotas still apply independently, with the new continuity owner excluded from its historical base-save accounting.

## Writable authorities and combat

The strict complete-save contract owns continuity. Canonical spatial/custody mutation reconciles identities: retain individual state across moves, unassignment, deletion to custody and redeployment; initialize a fresh acquired identity healthy; remove only identities no longer owned. No parallel writable monster list or retry journal exists.

The existing validated C0 profiles remain the maximum-HP authority. `monster_recovery.json` owns initial level, exact readiness numerator/denominator and whole-HP price. The recovery snapshot combines these configured inputs; production passes it through save creation, migration, recovery validation and writes. Editor-only legacy fixtures can resolve the production rules without changing frozen B1 configuration serialization.

New eligible manual snapshots capture persistent starting HP/revival immutably and emit `run.executable.combat-c2.v1` / `run.combat.c2.persistent.v1`. Historical C0 remains explicitly executable for its fixtures; B1 checkpoint serialization/execution is unchanged. Living positive-HP monsters can fight below readiness. Revival-pending defenders are excluded from combat geometry/engagement, while assignments remain inspectable. A lethal final HP sets revival required. Initial and final states are distinct immutable transient evidence.

Encounter resolution never writes canonical injuries. Complete-run settlement stages final individual consequences alongside Heat, knowledge, history, existing sequence allocation and recognized state, then eligible recovery and its debit. Only successful durable validation/readback publishes the new runtime/session. Stale sessions and persistence failures retain the existing complete-save semantics. Uncertain rollback can leave a durable complete candidate and requires established recovery; unchanged primary bytes are not promised when the filesystem cannot prove rollback.

## Recovery and reserve accounting

Readiness is `ceil(MaxHP * 66 / 100)` using integer arithmetic: Goblin 6/8, Skeleton 7/10. Zero HP requires revival. Positive HP with revival still set remains inactive until readiness; reaching readiness clears the flag in the same transaction. A survivor below readiness remains eligible. No automatic healing exceeds readiness.

Only assigned, active, provably vacant floors are eligible for automatic recovery. Inactive floors and custody preserve state without recovery spending or obligations. This eligibility interpretation preserves inactive defenses until activation/redeployment. Living injured monsters are processed before revival-pending monsters, ordered by numeric canonical floor, floor identity, placement sequence and AssignmentId. Available whole-HP funding partially heals the current monster; the remainder is derived from canonical state. There is no persistent queue and no frame-driven healing.

Recovery retries at complete-run settlement, canonical funding publication, activation/deployment increasing obligations, and eligible closed B2 boundaries. Actual restored B2 checkpoint position gates vacancy. Qualification route checkpoints without canonical floor identity cannot prove vacancy and suppress recovery until terminal; no occupancy is invented or persisted. Incomplete B2 horizons do not run recovery. Production scheduling remains dormant.

`StructureRuntimeState.ManaReserve` remains total mana. Required reserve is the sum of readiness HP times configured recovery price for every active-floor monster, including defeated defenders and duplicate species. Usable mana is `max(0, total - required)`. Returned/inactive monsters contribute zero. Recovery spends actual total mana and does not lower required reserve. Releasing obligations creates no grant/refund.

Ordinary debits and net obligation increases must leave enough total mana for the resulting global requirement before authorized recovery. Construction, content acquisition, draft editing, redeployment and activation pass through that check; applicable previews reflect it. Free unassignment/deactivation, ordinary lifecycle saves and recovery remain valid in legitimate deficits. Existing grants, capacity, fractional balances, prices, investment/refunds, purchase verification and composition diagnostics retain their authorities. Crisis drain remains an existing mandatory effect, not discretionary construction. Neither `EffectiveManaReserve` nor `ManaReservePressureCost` is reinterpreted.

Online ticks retain their established grants; recovery retries when funding is canonically published. Offline rate, timestamp, capacity clamp and one-time grant are unchanged. The consumed timestamp, grant, recovery and debit share one write. Offline evidence retains the original grant amounts and separately reports recovery spending and published balance.

Recovery spending is also exposed as transient transaction-result evidence. Renovation undo subtracts it from the refundable renovation payment, so healing never becomes structural investment or refundable mana. Undo preserves the recovered individual's current HP.

## Presentation and deferred work

C1 replay initializes each individual from recorded starting HP, carries revival status, retains original damage/defeat chronology and never reads post-run recovery as a combat event. Duplicate species retain separate AssignmentIds. Replay remains transient and unavailable after restart. Existing monster defeat indicators also identify partially revived inactive defenders. Existing party casualty presentation is retained.

The existing mana display shows derived usable mana; its tooltip explains required reserve and shortage. Room/content inspection and returned-content choices show localized level, current/max HP, readiness and revival state. Replay tooltips include recorded starting HP. No generalized HUD, UI framework or C1 polish changes are introduced. All added player text uses string-table keys.

No arrivals, background/offline combat, occupancy scheduler, transition waiting, level purchases/scaling, replacement identities, spells/species/balance changes, extra mana sources, trap resets or reusable-loot replenishment are implemented.

## Qualification environment and preservation

Automated project: `C:/Dev/Dungeon-Lord/Temp/ui-composition-combat-c0-diagnostics-validation-r2`. Only its source inputs and two existing save-namespace substitutions are refreshed; caches and earlier players remain. Automated namespace: `combat-c2-automated-20261016.json`. Unity: installed `6000.3.2f1`; CLI: `C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe`.

The retained owner project `...-warm` is never opened/refreshed. Its save `combat-c0-diagnostics-validation-20261015.json` starts at SHA-256 `54c7744e7ee92b1597daa88df995ef07c120e34667398377be78591f7fb7aa13`. The audit records 15,654 preexisting save/recovery/player files and separately 1,312 owner input/log/settings files. Original r2 inputs are archived before refresh. Existing disposable fixture files have exact backups; only those explicitly scoped fixtures may be restored after suites. Owner saves are never reset, migrated or automatically restored. New Windows persistence tests use unique directories under the automated project, not owner save namespaces.

## Actual automated evidence

The first sandboxed Unity attempt stopped at licensing access. A subsequent zero-selected run exposed the repository's EditMode fixture bridge naming. Both are retained and are not counted as passes. Intermediate fixture ordering, frozen-14 dispatch and test-only assertion failures are retained as failed attempts. The 121-pass integration selection covered C2/C0/C1; its B2/lifecycle filename-derived selectors selected no cases, so B2/lifecycle evidence is provided by later actual class-name selections and full qualification.

- Focused C2/C0/C1 EditMode: `combat-c2-integration-editmode-07.xml`: 121 passed, 0 failed, 0 skipped; CLI exit 0.
- Focused C2/C1 PlayMode: `combat-c2-focused-playmode-02.xml`: 90 passed, 0 failed, 0 skipped; CLI exit 0.
- Before the usage interruption, `final-01` focused EditMode reported 1,180 passed / 1 failed / 0 skipped. Its only failure expected schema 15 to be unsupported; the missing-contract fixture now uses 16 and retains its original failure-code assertions. `final-02` verified that correction (1/0/0) and focused PlayMode (96/0/0), then stopped on full EditMode (2,080 passed / 11 failed / 1 ignored; CLI 8). Full PlayMode/build had not started; no runner remained active on resume.
- The 11 failures were stale fixture expectations: construction/movement exact balances omitted deployed reserve; new manual evidence expected the historical C0 executable; settlement expected no recovery debit; inspection required the previous text exactly. Six test files were corrected, with no gameplay-source change. `final-03` passed the affected EditMode selection (113/0/0) and focused C2/C1 PlayMode (96/0/0), CLI 0 for both. The schema correction remains confined to unsupported-future starter/contract lookups; historical schema checks are retained.

All commands, filters, XML counts, source applicability, final full suites, build and preservation outcomes are recorded in the qualification manifest and final verdict below. Failed/zero-selected attempts are not substituted for final evidence.

## Owner manual testing after independent review

**Windows UAT save prerequisite:** A newly generated native canonical save contains **zero floors**. A qualified Windows player does not bundle or automatically select the owner's dungeon save. Before asking the owner to launch, verify the packed filename and the actual destination's expected floor count. Reuse existing Unity projects; creating additional Unity project copies requires explicit owner approval. Never use `save_primary.json` or migrate an original protected save for this test.

The existing reserve-UI Windows player has now been prepared with an exact copy of the verified two-floor C0 owner dungeon, migrated only in `combat-c2-reserve-ui-20261016.json`, and smoke checked through close/reopen. See [source/destination hashes, migration and launch instructions](windows-uat-save-preparation/owner-uat.md). This preparation is complete; do not reseed over subsequent meaningful C2 UAT progress.

1. Use the prepared isolated UAT save described above. Verify branch/head, configured namespace and expected **two floors** before launch. Do not open Editor and player against the same save. Do not overwrite or migrate the protected C0 owner save as a destructive test. For future preparation, copy only an explicitly authorized, hash-verified existing dungeon, preserve the disposable destination/sidecars, and migrate only the copy through the normal loader; stop if either source integrity or destination disposability is uncertain.
2. Acquire two duplicate monsters and verify distinct identities with healthy configured HP/level. Observe total, required reserve and usable mana, including Goblin 6 and Skeleton 7 obligations.
3. Run manually twice. Compare the second replay's starting HP/revival with the first run's post-settlement state. Replay must retain original combat final HP even when recovery has already changed canonical HP.
4. Exhaust recovery funding in the isolated UAT save. Verify living injured monsters recover first; a partially revived positive-HP monster remains inactive. Add passive mana and observe canonical retries, whole-HP costs and revival clearing exactly at readiness, with no healing above it.
5. Move, unassign, delete a room to returned custody and redeploy a monster. Check identity, HP and level persistence. Custody releases obligations without a refund; redeployment/activation must fund an increased requirement. Free relief remains available during shortage.
6. Inspect per-floor behavior on the supported two-floor layout: inactive floors retain state without obligations; active vacant floors share one global reserve. Confirm no automated arrivals or background parties appear.
7. Verify affordability during construction, content purchase and draft commit; purchases cannot consume protected funds. Inspect localized HP/readiness/revival text and the visible required-reserve/shortage information at desktop and portrait sizes.
8. Close/reopen Editor and Windows player separately. Continuity and wallet must survive; detailed replay must be unavailable after restart. Observe one-time offline grant plus separately reported recovery debit, including fractional mana.

Owner UAT and independent review are not claimed by this implementation packet.

## Final qualification verdict and applicability

| Stage | Passed | Failed | Ignored | Other skipped | CLI exit | XML |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| Full EditMode | 2091 | 0 | 1 | 0 | 0 | `TestResults/combat-c2-final-04-full-editmode.xml` |
| Full PlayMode | 3518 | 0 | 10 | 0 | 0 | `TestResults/combat-c2-final-06-full-playmode.xml` |
| Supplemental EditMode | 2 | 0 | 0 | 0 | 0 | `TestResults/combat-c2-final-05-focused-editmode.xml` |
| Supplemental PlayMode | 13 | 0 | 0 | 0 | 0 | `TestResults/combat-c2-final-05-focused-playmode.xml` |

NUnit's XML `skipped` aggregate includes tests labelled `Ignored`; the table separates those labels without counting them twice. Exact skip names/reasons and suite counts are in the manifest. All 65 production content build gates passed in full EditMode. No unresolved automated failure remains. Superseded failed/zero-selected attempts remain in the archive.

The resumed `final-02` runner had already stopped after failed full EditMode, so no concurrent Unity process was started. After targeted fixture corrections (`final-03`), `final-04` passed full EditMode, then stopped at full PlayMode: 3,514 passed, 4 failed and 10 ignored (CLI 8). Its four failures were stale schema-14 literals in `SaveSpatialMigrationLimitsTests`, including malformed-input replacements that no longer changed the production JSON. They were updated to schema 15, both frozen profiles remain asserted, and the failure helper now rejects no-op malformed fixtures. After targeted `final-05`, `final-06` reran only the failed full PlayMode stage and the previously unstarted Windows build. No successful unchanged full suite was rerun for reassurance.

The final review added explicit injury/level assertions to two existing test methods: `CombatC2Tests.AcquisitionRequiresDirectPriceAndAddedReserveAndReliefNeverDeadlocks` now covers movement, zero-funded unassignment, successful reserve-funded redeployment and identity-preserving revival; `DetachedCanonicalWriteAuthorityTests.StructuralDeletionReturnedCustodySurvivesCompleteSaveReopen` now carries injured HP/level through deletion, custody and successful redeployment. `final-05` reran these two methods in EditMode and both methods plus the budget fixture in PlayMode. Only these three Editor-guarded test files changed after successful full EditMode; the original full result plus the affected-method passes cover final source. The budget fixture lives in the runtime test assembly and is discovered by PlayMode, not the EditMode bridges. Full PlayMode/build then ran on the unchanged final inputs. The manifest records the exact differences; no gameplay/configuration source changed after successful full EditMode, and no Unity input changed after final full PlayMode/build.

Files corrected or added during the resume:

- Editor scene fixtures: `CombatC0ProductionEvidenceSceneTests.cs`, `ProductionDungeonReplaySceneTests.cs`, `ProductionDungeonRoomContentSceneTests.cs`, `PhaseSevenA4ProductionSceneTests.cs`.
- Core test fixtures: `TransactionalRoomConstructionTests.cs`, `PhaseSevenA5TransactionalRoomMovementTests.cs`, `SaveSpatialMigrationLimitsTests.cs`, `CombatC2Tests.cs`, `DetachedCanonicalWriteAuthorityTests.cs`.
- Qualification tooling: `run_combat_c2_qualification.ps1` (resume stages/precise filters), `collect_combat_c2_evidence.py` (actual verdicts, applicability, package and preservation receipts).
- Documentation: the active roadmap, invariant glossary and this C2 packet's implementation report, complete changed-file list, manifest and XML/audit archive. No gameplay implementation file was changed during the resume. Full paths and reasons are in [changed-files.md](changed-files.md).
- Checkout framing: `.gitattributes` pins the new frozen schema-14 resource to LF, as already required for frozen schema 13. Current qualified asset bytes are unchanged.

The independent owner envelope measured **686,596 bytes** for 165 records with 4,096-byte IDs. Maximum valid production recovery used **165 spatial records**, **six deployed monsters** and an exact **33-mana debit**. Real Windows NTFS migration/native/reopen evidence from the full EditMode test lives under the isolated project's `Temp/combat-c2-windows/20484b7e171b41e4b1727bfea9c30ffa`; no owner namespace was used.

### Exact final commands

The commands below are the actual recorded arguments (PowerShell quoting), using project `C:/Dev/Dungeon-Lord/Temp/ui-composition-combat-c0-diagnostics-validation-r2` and evidence root `C:/Dev/Dungeon-Lord`. All returned CLI exit 0. The manifest retains start/end UTC, exact arrays, XML counts/hashes and earlier failed staged attempts.

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' 'test' 'C:\Dev\Dungeon-Lord\Temp\ui-composition-combat-c0-diagnostics-validation-r2' '--mode' 'EditMode' '--output' 'C:\Dev\Dungeon-Lord\TestResults\combat-c2-final-04-full-editmode.xml' '--timeout' '1200' '--no-color' '--' '-logFile' 'C:\Dev\Dungeon-Lord\Temp\combat-c2-final-04-full-editmode.log'
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' 'test' 'C:\Dev\Dungeon-Lord\Temp\ui-composition-combat-c0-diagnostics-validation-r2' '--mode' 'EditMode' '--filter' 'AcquisitionRequiresDirectPriceAndAddedReserveAndReliefNeverDeadlocks;StructuralDeletionReturnedCustodySurvivesCompleteSaveReopen' '--output' 'C:\Dev\Dungeon-Lord\TestResults\combat-c2-final-05-focused-editmode.xml' '--timeout' '1200' '--no-color' '--' '-logFile' 'C:\Dev\Dungeon-Lord\Temp\combat-c2-final-05-focused-editmode.log'
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' 'test' 'C:\Dev\Dungeon-Lord\Temp\ui-composition-combat-c0-diagnostics-validation-r2' '--mode' 'PlayMode' '--filter' 'SaveSpatialMigrationLimitsTests;AcquisitionRequiresDirectPriceAndAddedReserveAndReliefNeverDeadlocks;StructuralDeletionReturnedCustodySurvivesCompleteSaveReopen' '--output' 'C:\Dev\Dungeon-Lord\TestResults\combat-c2-final-05-focused-playmode.xml' '--timeout' '1200' '--no-color' '--' '-logFile' 'C:\Dev\Dungeon-Lord\Temp\combat-c2-final-05-focused-playmode.log'
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' 'test' 'C:\Dev\Dungeon-Lord\Temp\ui-composition-combat-c0-diagnostics-validation-r2' '--mode' 'PlayMode' '--output' 'C:\Dev\Dungeon-Lord\TestResults\combat-c2-final-06-full-playmode.xml' '--timeout' '1200' '--no-color' '--' '-logFile' 'C:\Dev\Dungeon-Lord\Temp\combat-c2-final-06-full-playmode.log'
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' 'build' 'C:\Dev\Dungeon-Lord\Temp\ui-composition-combat-c0-diagnostics-validation-r2' '--target' 'StandaloneWindows64' '--execute-method' 'DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildCombatC2Windows' '--allow-dirty-build' '--timeout' '1800' '--no-tail' '--log-file' 'C:\Dev\Dungeon-Lord\Temp\combat-c2-final-06-windows-build.log' '--no-color'
```

Logs are retained at the `-logFile` / `--log-file` paths in these commands. They remain local and are not included in the PR archive because raw Unity startup logs may contain licensing process arguments. XML/audit evidence is archived in `qualification-reports.zip`; the full per-file review inventory is [changed-files.md](changed-files.md).

### Windows build

One successful Windows x64 Development Build (`Succeeded`, errors **0**, warnings **1**, CLI **0**), Unity **6000.3.2f1**, target **StandaloneWindows64**. The one warning is optional Unity native-symbol upload: its service URI is empty, so native symbols were not uploaded. No gameplay/compiler correction is indicated, and no unrelated Unity Services settings were changed. The CLI also reports a nonfatal LicensingClient validation diagnostic; its actual build exit is 0 and the BuildReport result is Succeeded. Player: `C:/Dev/Dungeon-Lord/Temp/ui-composition-combat-c0-diagnostics-validation-r2/Builds/Development/Windows-combat-c2-20261016/Dungeon Lord.exe`. Build report, packed asset lists, build messages and every new player-file hash are recorded in the manifest. Packed dependencies include recovery config, frozen schema-13/schema-14 limits, localization and the isolated build config; packed player assets contain `combat-c2-automated-20261016.json`. This is build/package evidence, not owner standalone gameplay acceptance.

### Preservation verdict

All 15,654 preexisting save/recovery/player files match their baseline hashes after restoring only explicitly backed-up disposable test fixtures. All 1,312 retained warm-owner input/log/settings files match. The protected owner save remains SHA-256 `54c7744e7ee92b1597daa88df995ef07c120e34667398377be78591f7fb7aa13`. The owner project was never opened/refreshed, and that save was never reset, overwritten, migrated or restored. Frozen schema-13 limits, baseline schema-14 limit bytes and the C0/B1 golden checkpoint archive match their original hashes. Prior comparison players remain retained.

### Remaining review and owner work

Independent ChatGPT review, resolution of blocking findings and the eight owner Editor/Windows checks above remain required. No owner UAT or merge approval is claimed. Current implementation remains level one only; future HP-table reductions or progression need an explicit compatible content/migration policy. Unsupported or unprovable checkpoint information fails closed; production Living Dungeon scheduling remains dormant. There is no unresolved known automated failure or newly required separate implementation PR.
