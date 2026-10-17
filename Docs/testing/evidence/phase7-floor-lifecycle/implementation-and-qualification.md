# Phase 7 — graphical floor construction and lifecycle

Branch: `codex/phase7-production-floor-lifecycle`. Verified starting main: `7a664efc5e38e0d3522e1ddbe6cd3747b14e61ce`. GitHub reports #240 merged on 2026-10-17; #239 and #238 are the preceding combat integrations. Historical roadmap/PR-body statements about C2 still awaiting merge do not override the actual remote refs and merged metadata.

Classification: Complex but well-defined; repository recommendation GPT-6.1 Sol High, fallback GPT-6 Sol High, then GPT-6 Astra Low. No policy/governance changes.

## Player result and scope

The production Dungeon floor summary exposes **Manage floors**. Its bounded existing context scroll shows the selected floor's authoritative lifecycle blocker or readiness, the applicable Activate/Deactivate action, and the actual configured next shell opportunity, mana price and research-permission status. Blocked activation stays visible and disabled with its localized authority reason. Shell construction starts Inactive; existing Edit Dungeon controls build its route/content. Once the configured shell exists, its action disappears and no synthetic next floor is displayed.

Construction retains the currently selected valid floor; the new floor immediately appears in the selector. Activation/deactivation likewise retain selection and update state, selector, actions, feedback, wallet/reserve and passive-mana HUD through the existing canonical publication event. Floor 1's deactivation is visibly blocked by the existing authority. Lifecycle actions are outside Edit Mode and unresolved draft recovery. No Activate All production button is added.

The expanded area scrolls within the existing responsive bottom sheet. The normal landscape summary retains its horizontal composition and compact-height contract. New strings use nine entries in the existing Bootstrap string table; costs and permission identity come from the canonical construction preview/profile. No gameplay tuning, research mechanics, floor definitions, persistent UI state, or alternative wallet/selection authority is introduced.

## Selected-floor identity and transactions

Production selection remains the existing `ProductionDungeonController.selectedFloor`. Bootstrap/GameRoot selection is neither read for production lifecycle targeting nor secretly changed. The controller passes the production-selected canonical ID explicitly to the existing `SaveService.PreviewFloorLifecycle` and `CommitFloorLifecycle` boundary. GameRoot and Bootstrap remain unchanged.

`FloorLifecyclePreview` now carries its exact action/target, an internal copy of its session baseline bytes, and read-only, canonical-order changed-floor descriptors derived by the authority from the detached candidate versus its baseline. Confirmation is required only for Deactivate with more than one actual changed floor. The UI enumerates those descriptors; it does not infer a cascade from index arithmetic. Confirmation explains the shorter future Active prefix, passive mana, retained structures/content/investment/custody without refunds, and unchanged already-started run snapshots.

The optional expected-preview argument preserves existing diagnostic call sites. Production supplies it on every lifecycle commit. Before mutation the authority rejects a mismatched action/ID, noncommittable preview or changed session bytes with `floor.lifecycle.stale_preview`, then retains the existing disk/session check and repeats eligibility, reserve, complete-save validation and durable atomic publication. The controller also checks that the selected floor still equals the captured confirmation target. Null/empty/missing explicit selections fail closed. A repeated deactivation is a validated no-op without persistence/publication. Ordinary Activate still rejects an already-active target.

Construction directly reuses SaveService's existing floor-construction quote/commit, shell investment accounting, current affordability/protected-reserve validation, duplicate/stale checks and atomic complete-save path. Rejected or cancelled operations spend nothing. No schema or migration code changes: writable schema remains **15**, with the existing canonical owner and save compatibility chain.

## Meaningful changed files

| File | Purpose |
| --- | --- |
| `Assets/_Project/Scripts/UI/ProductionDungeonFloors.cs` | Production floor-management presentation, explicit-target commits and consequence confirmation. |
| `Assets/_Project/Scripts/UI/ProductionDungeonController.cs` | Initialize/present controls; refresh eligibility with wallet/publication changes. |
| `Assets/_Project/UI/ProductionDungeon/Dungeon.uxml` | Context-area management toggle, authoritative text and action bindings. |
| `Assets/_Project/UI/ProductionDungeon/Dungeon.uss` | Bounded expanded panel and compact landscape header. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/FloorLifecycleAuthority.cs` | Read-only preview consequences, target/baseline binding, invalid-target rejection and repeated-deactivation no-op. |
| `Assets/_Project/Scripts/Services/SaveService.cs` | Forward optional expected preview through the existing transaction owner. |
| `Assets/_Project/Data/Bootstrap/string_table_en.json` | Nine localized labels, explanations and stale-preview message. |
| `Assets/_Project/Editor/DungeonSpatial/Tests/PhaseSevenFloorLifecycleTests.cs` | Eleven target, stale/missing-selection, no-op, ordering, suffix-consequence and localization/UXML checks. |
| `Assets/_Project/Editor/DungeonSpatial/Tests/ProductionDungeonFloorSceneTests.cs` | Four actual production scene scenarios, reopen, identity mismatch, cancellation, resolution/text matrix, sample saves and captures. |
| `Assets/_Project/Tests/PlayMode/PhaseSevenA4ProductionShellPlayModeTests.cs` | Expose those scenarios through the established PlayMode reflection bridge. |
| Adjacent three new `.meta` files | Stable Unity GUIDs. |
| This evidence directory | Commands, source hashes, reports, audits, screenshots and owner instructions. |

Inspection included AGENTS/model policy; the current execution roadmap and Phase 7A0 lock (especially Decisions 104–107); Phase 6 floor-foundation lock; spatial-capacity/route, UI-trust and cross-spec invariants; existing construction/activation/lifecycle authorities; C2 reserve semantics; localization/config ownership; GameRoot/Bootstrap/production UI; and existing A2/A3/A4 and production scene tests. Current source/schema and merged C2 behavior supersede dated historical schema statements.

## Executed validation

Unity **6000.3.2f1**, primary `C:/Dev/Dungeon-Lord`, sequential processes. Exact invocations, including unsuccessful attempts: [commands.md](commands.md). Machine verdicts/paths: [qualification-manifest.json](qualification-manifest.json). Actual NUnit XML: [qualification-reports.zip](qualification-reports.zip).

| Stage | Passed | Failed | Skipped | CLI exit |
| --- | ---: | ---: | ---: | ---: |
| EditMode 03 | 230 | 0 | 0 | 0 |
| PlayMode 04 | 7 | 0 | 0 | 0 |
| Final EditMode 04 | 11 | 0 | 0 | 0 |
| Final PlayMode 05 | 4 | 0 | 0 | 0 |

EditMode 03 comprises 65 production content/build gates, 63 A2 construction checks, 23 A3 eligibility checks, 68 A4 lifecycle/run/atomicity checks and 11 new preview/identity tests. Final EditMode 04 additionally checks every UXML tooltip binding against the string table after that assertion was added. PlayMode 04 includes all four floor scenarios plus the existing content-height/action-safety matrix, overlay/input layout and C2 visible-reserve scenario. Final PlayMode 05 repeats only the four affected scene scenarios after adding sample-save exports/captures. Counts overlap and must not be summed as distinct tests.

Failures are retained and explained:

- EditMode 01: sandbox denied Unity licensing hardware access; cancelled, no tests. Reran with approved escalation.
- EditMode 02: compiler rejected a test-only `SaveData.schemaVersion` assertion. Corrected to inspect the serialized complete-save envelope; no production correction was needed.
- PlayMode 01: 5 passed, 2 failed, no skips; CLI 1 / Unity 2. The real large-text landscape summary-height regression was corrected by retaining horizontal summary composition. The deepest-deactivation assertion was corrected to read resolved visibility instead of an unset inline style. Neither assertion was weakened.
- PlayMode 02/03: no tests. Unity's batch scene-recovery dialog blocked startup. The backup file **and then its empty folder** created by PlayMode 01 were preserved under `Logs/phase7-floor-lifecycle`; Unity prompts even on the empty folder. Timestamp/path guards restricted these moves to this run's own artifacts. No historical backup/workspace was moved or removed.

Initial logs used root `Temp` and were subsequently cleared by Unity startup housekeeping; their verdict/error summaries remain recorded here, and the available XML is archived. Later complete logs remain at the manifest's `Logs/phase7-floor-lifecycle/...` paths. Raw Unity logs contain authentication arguments and are deliberately not published. The XML archive was checked for access-token arguments (zero matches). An existing empty `Tests 1.asmdef` warning and the nonfatal LicensingClient diagnostic appeared; no UXML/USS parse warning was found. No Windows standalone build or execution is claimed.

## Protection and evidence applicability

No new Unity project copy was created, and no existing validation project was used or edited. Tests ran only in the primary project. Scene fixtures substitute a GUID-named disposable save before GameRoot startup. As an extra guard, qualification temporarily substituted `phase7-floor-lifecycle-qualification-20261017.json` for the default build-config filename; the original `save_primary.json` value is restored with no final diff. The sole Unity-generated ProjectSettings dictionary-order change is also reverted; its patch is retained as evidence.

[tested-inputs.json](tested-inputs.json) records the final tested code/UI/localization/test hashes, including the temporary isolated build config. Restoring only that filename changes no gameplay configuration. No further source/UI/localization changes occurred after the passing applicable checks.

[save-baseline.json](save-baseline.json) and [save-final-audit.json](save-final-audit.json) prove all **45 preexisting saves/sidecars** are byte-identical. Unity refreshed the preexisting generic app-data `TestResults.xml`; it is a runner report, not a save. No saved-game restoration was necessary. Protected `combat-c0-diagnostics-validation-20261015.json` remains SHA-256 `54C7744E7EE92B1597DAA88DF995EF07C120E34667398377BE78591F7FB7AA13`; it was not opened, migrated, reset, renamed or overwritten. Existing historical validation directories and prior players were not targets of this work.

Large-text captures of the actual scroll-to-construction action are in [screenshots](screenshots): 720×1280, 1080×1920, 1920×1080 and 1536×2048. The test covers all four sizes at Small/Default/Large, action reachability, physical hit-target size, safe bounds and a nonempty map viewport. A scrolled context can intentionally clip preceding rows; scrolling restores them. Desktop and phone captures were visually inspected.

## Remaining qualification and non-goals

Independent review precedes owner Editor/Windows qualification and merge approval. [Owner steps and isolated synthetic saves](owner-uat.md) concentrate on interaction/readability. No owner UAT or physical-mobile/Windows-player acceptance is claimed.

With only Floor 1 and Floor 2, Floor 1 mandatory Active and Floor 2 deepest, a real committable cascade is unreachable. Generic suffix consequences and confirmation/cancellation are tested with explicitly synthetic test-only floors/previews, never authored production content. No claim of a real three-floor production transaction is made.

Deferred/non-goals: Floors 3–5/new content; bulk management; research, analysis or More screens; onboarding; arrivals/occupancy/scheduling/offline combat; new combat/recovery mechanics or tuning; corridor segment editing; Bootstrap retirement; broad redesign/refactoring; schema/migration changes; governance changes; storage cleanup. A newly built Windows player is deferred to the existing owner qualification workflow with an approved output location and isolated save namespace. No implementation blocker requires owner input.
