# Combat C0 authoritative evidence and focused review correction — 2026-10-15

Existing draft PR #238, branch `codex/combat-c0-deterministic-combat`, reviewed starting head `404251f71150b1de317cd1707b9dc2fdda9e508f`; main baseline `eb80df1e3fd09c18aedb062afc108eb941deb8a3`. The owner explicitly stopped keyboard/scroll investigation and authorized developer event evidence for mechanical C0 acceptance. C1 owns player-visible attacks, HP changes and graphical playback. No new combat-log navigation UI or production exporter is added. Keep draft/unmerged.

## Evidence available for external review

[production-path-run-1.txt](production-path-run-1.txt) is the complete chronological export from the existing isolated Bootstrap production-scene fixture. It executes the normal `GameRoot.SimulateRunOnce()` detached manual transaction and reads `CombatC0Diagnostics.Events`, `.Monsters` and `.ChargedWork`. Export code is confined to the Editor test fixture; no runtime/save authority changes. The loaded production config and final roster supply initiative values; recorded ordinals supply the actual action order. No events are sorted, filtered, shortened or truncated.

The report contains every event property, full canonical actor/target/assignment/floor/room/encounter/rules/attack/targeting identity, source/target coordinates or explicit absence, eligibility reasons, round/action/run/event ordinals, HP before/after, damage, defeat/completion, final member/monster HP and the full aggregate outcome. It is deliberately a text inspection artifact, not a new durable C0 checkpoint or save format.

SHA-256: `bcfdb4c5adc8fc791f3faebdb227386613290beba50c8cfbfb0ea15f8811f9fe`. Only line endings are normalized to LF for repository review; original export bytes/hashes are preserved in the final support ZIP.

This is **automated fixture run-1**, not owner run-68/run-69/run-71/run-72 or their reconstruction. Earlier transient events do not survive closing Unity. The new run repeats the same fixture/configuration that produced the preserved 18-event reproduction; it does not claim to recover previously closed transient memory. No owner save is loaded.

| Actual production-path result | Recorded value |
| --- | --- |
| Records | 18: one start, ten eligibility, six attacks, one completion |
| Encounter | `run-1/combat/0`, one round, completion `DefendersDefeated` |
| Party classes | Ranger, Cleric, Mage, Cleric, Mage |
| Actual initiative/action order | Ranger 40, Skeleton 25, Mage/member 2 at 20, Mage/member 4 at 20, Cleric/member 1 at 10, Cleric/member 3 at 10 |
| Final party HP | 5/7, 8/8, 6/6, 8/8, 6/6; no casualties |
| Defender | Skeleton, 10 → 0 HP; defeated |
| Coordinates | party anchor `(1,0)`, defender `(0,0)`, RoomLocal |
| Result | success true, score 2096, `run.reason.success`, next sequence 2 |
| Mana / Heat after transaction | 1000 / 0; fixture funds 1000 before the run |
| Charged resolver work | 1,729 units |

Assertions verify identical latest/selected record identities and event sequence, all 18 ordinals ordered/unique, reciprocal damage, joined target HP, no dead actor attack, final HP equality and completion. Existing prior-qualified tests cover other mechanics including groups, two floors, LOS denials, workload failures, rollback and historical compatibility. This one trace is not comprehensive mechanic coverage and does not replace owner gameplay acceptance.

## Preserved input reproduction: findings and limits

[preserved-input-reproduction.zip](preserved-input-reproduction.zip) retains both focused attempt reports/logs, the exact diagnostic reproduction source, preparation helper and preservation snapshots. [Manifest](preserved-input-reproduction-manifest.json) records original bytes/hashes plus archived hashes; only log credentials/machine IDs are redacted. Original raw logs and archive remain locally in Temp/TestResults. No earlier owner UAT artifact is replaced.

Both latest and selected history records reported the same run-1 and 18 events. The selected-history versus LatestOutcome hypothesis was therefore **not supported** by this scenario. The first attempt did not enter focus via queued F2. The next attempt established the owner-confirmed initial focus through its existing method; five queued PageDown presses left the opening summary text unchanged. The printed scroll-offset property observes the normal full-page slot, so its zero value is not proof about the separately stored focused C0 slot. The wheel assertion was **not reached** after the PageDown failure. No root cause for missed input is asserted. Investigation ended by owner direction; no further input test, navigation UI or environment reconstruction followed.

## Two GitHub review findings corrected

`BootstrapOverlay.cs` is restored exactly to its pre-paging implementation at `164fc6cb97bf48a9731a4e797858ff0a1e418c9b`. This removes the C0-only focused paging branch/slot, LatestOutcome paging gate and global TMP masking/wrapping override. The existing historical focused text and full-diagnostics paging behavior are restored.

- [Historical B1 clipping finding](https://github.com/gdg3417/Dungeon-Lord/pull/238#discussion_r4287399055), thread `PRRT_kwDOQxUKE86s7zHI`: removing the new masking override restores historical overflow behavior instead of introducing new scrolling UI.
- [Stale C0 paging-slot finding](https://github.com/gdg3417/Dungeon-Lord/pull/238#discussion_r4287399073), thread `PRRT_kwDOQxUKE86s7zHX`: removing focused C0 paging removes the inconsistent slot entirely. Focus no longer depends on LatestOutcome's transient C0 tag and cannot reopen partway through a C0 page.

Numeric invariant `(x,y)` formatting, localized aliases/legends/reasons, actual HP/damage/defeat text and accurate C0-versus-historical replay messages remain byte-unchanged from `404251f`. Complete immutable evidence and replay eligibility remain unchanged. No combat/config/geometry, B1/B2 executable/checkpoint, save schema/migration, transaction, economy or settlement implementation is modified. The frozen B1 ZIP is byte-identical.

Accepted limitation: the restored development overlay may overflow/offscreen, overlap the dungeon, and be inconvenient for inspecting a long log. Keyboard/scroll accessibility is not qualified and is not a player-facing C0 requirement. Use the authoritative test report/APIs for mechanical review. C0 graphical replay remains unavailable; C1 is separate. This acceptance does not excuse any mechanical combat/save defect.

## Executed focused validation

Windows 11 x64, Unity **6000.3.2f1 (a9779f353c9b)**, Unity CLI **1.0.0-beta.12**. Same existing automated disposable project: `C:/Dev/Dungeon-Lord/Temp/ui-composition-combat-c0-access-validation`. Fallback namespace `combat-c0-access-validation-20261015.json`; scene fixture overrides before Boot to unique `phase7a4-scene-b9f8f92e4e3c4ead83751292de45416d.json` and removes only its own disposable save at teardown. The root is moved to DontDestroyOnLoad after Bootstrap startup, as shown in the report.

| Check | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Focused formatter and B1/C0 overlay regressions, EditMode | 12 | 0 | 0 |
| Production-path event/HP export, PlayMode | 1 | 0 | 0 |
| Exact disposable-source input comparison | 1,298 inputs | 0 mismatches | — |
| Protected existing saves/build artifacts | 14,442 files | 0 changed | — |

[Report summary and provenance](report-summary.json), [EditMode XML](combat-c0-authoritative-focused-editmode.xml), [final PlayMode XML](combat-c0-authoritative-production-path-final.xml), [initial focused logs/support](focused-validation-support.zip), [final logs and strict/preservation support](focused-validation-support-final.zip). No C# compiler warnings/errors were found. Existing licensing/debugger service noise is present in logs; both focused commands exited successfully. One known generated project-setting difference was archived before exact restoration.

`R` is `C:/Dev/Dungeon-Lord`, `P` is `R/Temp/ui-composition-combat-c0-access-validation`. Use the installed Unity CLI and bundled Python:

```text
python Tools/Presentation/qualify_combat_c0_access.py refresh
python Tools/Presentation/qualify_combat_c0_access.py check
unity test P --mode EditMode --filter "CombatC0Diagnostics|BootstrapOverlayPaging" --output R/TestResults/combat-c0-authoritative-focused-editmode.xml --timeout 300 --no-color -- -c0DiagnosticsSerialImports -logFile R/Temp/combat-c0-authoritative-focused-editmode.log
unity test P --mode PlayMode --filter CombatC0ProductionEvidenceExport --output R/TestResults/combat-c0-authoritative-production-path-final.xml --timeout 300 --no-color -- -c0DiagnosticsSerialImports -logFile R/Temp/combat-c0-authoritative-production-path-final.log
python Tools/Presentation/qualify_combat_c0_access.py checkpoint authoritative-final
python Tools/Presentation/qualify_combat_c0_access.py audit
git diff --exit-code 164fc6c -- Assets/_Project/Scripts/UI/BootstrapOverlay.cs
git diff --check
```

The earlier navigation-test source/metadata in this disposable project were moved to a preserved non-input directory before exact refresh; no project/cache was reconstructed. The test export reuses its unique fixture-save identity in the artifact filename and uses CreateNew, preserving earlier reports automatically. The first passing export and its XML remain separately retained; a final focused PlayMode rerun after this test-artifact naming correction also passed (1/0/0). All formatter/runtime inputs are unchanged between these focused runs. The guarded opt-in import profile is the previously qualified test-only profile; no default production setting changed.

## Saves, owner project and remaining qualification

All **14,442** pre-existing protected files remain unchanged. Primary SHA-256 remains `39224B12446EC30990D043385E77634E45CC7DA1DC5E50E75237B33F25C79DF2`. Owner integration/diagnostics namespaces, two-floor project, recovery/migration evidence, prior UAT records and Windows players are preserved. No owner project refresh, save transfer, gameplay UAT or standalone launch occurred.

The owner project remains `C:/Dev/Dungeon-Lord/Temp/ui-composition-combat-c0-diagnostics-validation-warm`, configured for `combat-c0-diagnostics-validation-20261015.json`. It and its player still contain their previous qualified inputs; they are not claimed to contain this reversion. The [updated owner acceptance plan](../integration/owner-uat.md) uses the current dungeon without another transfer.

**Exact remaining gate:** external review must accept the mechanical event evidence, the two narrow review corrections and the revised developer-diagnostics/C1 boundary, then explicitly determine the final qualification required for the resulting head. Full EditMode, full PlayMode, broader production gates, strict helper reruns and a Windows Development rebuild were **not executed** for this delta, per owner instruction. Earlier full-suite/build results remain historical and are not relabeled as results for this source. If review requires any of these, run each required check once on stable final inputs, preserve artifacts/saves, then refresh the existing owner project without altering its save. Owner Editor/standalone/lifecycle acceptance remains separate as applicable. No merge readiness is claimed; no C1/C2 work begins.
