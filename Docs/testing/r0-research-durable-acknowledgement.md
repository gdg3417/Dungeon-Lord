# R0 research durability acknowledgement

Implementation base: `fd9917f5c4c22c37fc5d22c8617eb2c0ca8f7394` (merged #253).
Branch: `codex/r0-research-durable-acknowledgement`.
Execution recommendation: GPT-6.1 Sol, High; Complex but well defined. Research,
foreground preparation and canonical persistence have known authority boundaries;
the integration needs cross-system reasoning. No availability fallback was needed.

## Authority and scope

This implements only R0 from the [hybrid persistence proposal](../planning/living-dungeon-hybrid-persistence-proposal.md#r0--separate-research-persistence-acknowledgement-prerequisite).
Sources checked include AGENTS.md, the model policy and development rules, the
active post-GD60 roadmap, Spec 9, the invariant glossary, the M7 research boundary
plan, and the GD59 player research contract/runbook. Historical M7 production
restrictions and future Phase 8 offline-first/two-track decisions describe other
scopes; R0 preserves the merged local-MVP online-gated single-slot implementation.
The proposal's detailed H1/H2/O1 policies remain unimplemented.

No gameplay configuration, progress rate, project ID, verification rule, reward,
cost, production-claim permission, feature flag, save field or migration changes.
Writable schema remains **15**. The only new string-table entry is
`ui.player_research.save_failed`; recovery uses the existing localized
`gd66.transaction.recovery_failed` entry. Existing success keys remain unchanged.

## Transaction and publication

`PlayerResearchActionHandler.Start/Claim` submit a staging callback instead of
mutating their supplied runtime. `SaveService.CommitPlayerResearchTransition`:

1. Calls the existing foreground preparation barrier and takes its replacement
   current runtime before validation/staging. A failed barrier returns failure.
2. Captures recognized current values with `DetachedRecognizedSaveStateSnapshot`,
   prepares them against the owned canonical session, and obtains a detached
   validated runtime projection. Session-owned spatial state, other canonical
   owners and unknown compatible fields retain their existing preservation rules.
3. Runs unchanged research eligibility/resolvers and start or claim mutations on
   that private projection. `GameRoot` applies the existing objective completion
   applier to the same candidate. Unlocks remain derived from completed research;
   there is no separate unlock write. Rejections/no-ops perform no research write.
4. Calls `DetachedCanonicalWriteAuthority.SaveRecognizedState` directly. Calling
   ordinary `Save(candidate)` here would prepare foreground again and could discard
   the proposed transition. The existing writer uses atomic replacement, exact
   readback and fresh semantic reopening through `ExactCompleteSaveAtomicPersistence`.
5. Updates the canonical session and emits `CanonicalRuntimePublished` only on
   confirmed success. `GameRoot.PublishCanonicalRuntime` attaches the new save to
   time/UI consumers. Only then does the action return its existing success key.

Root-created handlers read the current published save through a provider, so a
retained handler cannot keep the prior research reference. The detached handler
inside `ApplyForegroundPassiveTick` still has no persistence callback: tick effects
remain inside the pump's single complete-save transaction. The existing standalone
progress callback is retained for its existing callers; start/claim cannot use it
as acknowledgement.

Proven rollback returns failure with `gd66.write.atomic_save_failed` and publishes
nothing. Uncertain authority returns `gd66.transaction.recovery_failed`, latches
the existing save-service recovery guard, faults foreground in GameRoot, blocks
research presentation and rejects subsequent actions without storage retry. A new
load coordinator must establish authority; the previous process never guesses that
the action failed. A surviving committed candidate is recognized on reopen and
duplicate start/claim remains rejected.

## Canonical versus legacy

Supported Bootstrap startup configures canonical persistence, loads/migrates old
saves, and subscribes validated runtime publication before player actions. The
unconfigured `SaveService` path is explicitly schema <=6 test/repair compatibility.
Its serialization and ordinary `Save/Load` behavior are unchanged. Research
start/claim fail closed without canonical authority; R0 does not add a new legacy
transaction protocol. A callback-free handler can still run detached progression
but cannot acknowledge start/claim.

The older research save integration fixture now uses the actual configured
canonical load path. The historical journey/diagnostics fixtures keep their legacy
presentation sandbox outside a scoped research segment; that segment uses a real
canonical writer and GameRoot publication. New scene tests independently exercise
Bootstrap, the overlay, foreground progression, localized feedback and recovery.

## Automated qualification

Results and commands are recorded below after execution. Raw reports/logs are in
`TestResults/r0-research-durable-ack/`. Unity CLI is 1.0.0-beta.14 (update check
confirmed current); Editor is 6000.3.2f1. Runs require normal Windows access for
Unity licensing; the initial sandboxed attempt could not access Windows system
information and produced no verdict. The first completed filter discovered zero
tests; forwarding fixtures corrected discovery. Neither attempt counts as a pass.

New deterministic tests cover complete start/claim, reopened state, exact-once
objective/unlock, preserved unrelated extension data, invalid/no-op actions,
private candidate failure, staging writes, replacement, directory flush,
post-replacement readback, verified rollback, unprovable rollback, fresh recovery,
blocked unsafe retry, authoritative foreground replacement and callback-free ticks.
Existing canonical semantic validation/recovery tests supplement this coverage.
Production-scene tests cover the public GameRoot/overlay path and recovery UI.

| Run | Mode | Passed / failed / skipped | Result |
| --- | --- | --- | --- |
| `edit-regression-06.xml` | EditMode | 308 / 1 / 0 | Sole failure: an existing view-size/screenshot check requires graphics and was run with `-nographics`. Re-executed successfully below. |
| `edit-final-10.xml` | EditMode | **78 / 0 / 0** | 47 research cases, 26 objective cases, 4 save-lifecycle cases, and the successful graphics rerun. |
| `play-final-11.xml` | PlayMode | **9 / 0 / 0** | Six Bootstrap/overlay research cases plus three foreground/admission/reopen regressions. |

The 253 canonical/foreground non-scene cases in run 06 all passed: complete-save
10, load/recovery 65, semantic validation 27, canonical writer 59, foreground B3B2
51, B3A 41. Ten additional nearby scene cases also passed there. Final runs use
the final source. Initial development failures were corrected and rerun; no test
is reported as passed based on compilation or zero-case discovery.

Exact qualification commands (PowerShell; repository root `C:\Dev\Dungeon-Lord`):

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' self-update --check --format json
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode EditMode --filter 'R0ResearchDurability;R0ResearchHandler;R0ResearchSaveIntegration;R0ResearchJourney;R0ResearchDiagnostics;PhaseFourWrites;PhaseFourCompleteSave;PhaseFourSaveSemantics;PhaseFourLoad;LivingDungeonPhaseB3A;LivingDungeonB3B2' --output 'C:/Dev/Dungeon-Lord/TestResults/r0-research-durable-ack/edit-regression-06.xml' --timeout 900 --no-color -- -nographics -logFile 'C:/Dev/Dungeon-Lord/TestResults/r0-research-durable-ack/edit-regression-06.log'
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode EditMode --filter 'R0ResearchDurability;R0ResearchHandler;R0ResearchSaveIntegration;R0ResearchJourney;R0ResearchDiagnostics;R0ResearchObjectives;PhaseFourOfflineSaveLifecycle;LivingDungeonB3B2LiveLabelsSessionReplayAndLargeLayoutsObserveOnlyCommittedState' --output 'C:/Dev/Dungeon-Lord/TestResults/r0-research-durable-ack/edit-final-10.xml' --timeout 600 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/TestResults/r0-research-durable-ack/edit-final-10.log'
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode PlayMode --filter 'PhaseSevenA4ProductionShellPlayModeTests.R0Research;LivingDungeonPhaseB3APlayModeTests;LivingDungeonB3B2PlayModeTests' --output 'C:/Dev/Dungeon-Lord/TestResults/r0-research-durable-ack/play-final-11.xml' --timeout 600 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/TestResults/r0-research-durable-ack/play-final-11.log'
git diff --check
```

The [evidence archive](evidence/r0-research-durable-ack/reports.zip) contains those
three XML reports and a source-hash/result manifest. Raw diagnostic attempts and
logs remain in the local TestResults directory. The initial wrong-slot fixtures,
unbound/inactive overlay lookup and absolute fault-count assumptions were test
setup errors; corrected cases pass in the final reports. No standalone build or
owner-operated UAT was executed. Those remain scheduled after independent review.

## Risk review

No schema/migration concern was found. Unknown compatible members remain owned by
the complete-save session; R0 does not reserialize a whole save with JsonUtility.
The staging projection is temporary transaction state, never another runtime
authority. Related objective effects cannot escape a failed candidate. No new
retry loop exists. An independently recovered candidate can be committed despite
the original caller receiving uncertainty; duplicate validation handles that case.

The boundary remains synchronous and uses the existing validation/write costs.
It is not a performance optimization. Backend verification remains unavailable;
`CanClaimProduction`, `WouldChargeCosts` and `WouldProcessOfflineProgress` retain
their approved prototype meanings. Unsupported storage/platform activation remains
subject to the existing canonical preflight. Owner-operated qualification is
pending independent PR review; automated evidence is not merge approval.

Pre-existing limitation found while qualifying the real scene: native creation's
JsonUtility clone can materialize absent research as empty object shells. The
existing resolver blocks those as an occupied slot. R0 intentionally preserves
malformed-state handling instead of repairing saves in an action handler. The
scene fixtures use the existing `ClearResearchPendingScaffold` development control
to establish explicit empty research before testing; the owner plan below does
the same. The production graphical shell initially hides Bootstrap's legacy action
panel; research remains in its existing **Development tools** view. Tests use that
view's existing overlay, not a newly implemented research screen. Neither issue is
introduced or repaired by R0.
The smallest separate follow-up is to preserve explicit nullable research fields
in native creation's existing serialization path, with fresh-save/reopen tests;
it should not repair malformed existing research during player actions.

## Owner qualification after independent review

Do not start these steps until independent code review finds the PR ready for
manual testing. Use the reviewed commit for both Editor and Windows verification.
No manual disk-failure injection is requested: deterministic fixtures own it.

1. Make a disposable local project copy on a nonredirected NTFS path. Before
   entering Play Mode, set Project Settings > Player > Product Name to
   `Dungeon Lord R0 Qualification`. This gives the copy a separate persistent-data
   directory. Keep the normal project's saves untouched. Record the reviewed
   commit, Editor version and isolated save path. Open
   `Assets/_Project/Scenes/Bootstrap.unity` in this copy.
2. Enter Play Mode with a fresh isolated save. Open **Development tools** in the
   production view to expose the existing Bootstrap action panel. Show diagnostics
   with F1 if needed and click **Clear Research Pending** once to establish explicit
   empty research (the pre-existing native empty-shell issue above). Hide diagnostics
   again. Before a valid run, research must be blocked. Build the starter dungeon
   and complete a valid run. Confirm Activity Analysis Start appears in that panel.
3. Start research once. Expect localized start success, one active slot and visible
   progress. Repeat the action if accessible: it must not create a second project
   or another success. Check that normal dungeon/visitor actions still work.
4. Stop Play Mode during partial progress and restart it. Expect the same project
   and saved progress, with no completed research or unlock yet. Preserve a copy of
   the isolated save and screenshot the partial state before/after reopen.
5. Remain in foreground play until completion-pending. Expect Claim under existing
   rules, with no new charge or reward. Through existing development controls,
   check offline and verification-pending each block start/claim; restore online
   and clear verification pending. These checks must not change research state.
6. Claim once. Expect localized claim success, cleared active research, one
   completed project and Basic Run Analysis unlocked. First Dungeon Contract
   completion should appear only when its other existing requirements are met.
   Repeat Claim if accessible: no repeated success, duplicate completion or effect.
7. Stop/restart Play Mode again. Expect completed research, unlock and any earned
   objective to persist. Compare unrelated dungeon layout, run history, mana and
   heat for coherent normal progression. Confirm no raw keys/IDs, missing strings,
   exceptions or missing references in the player flow.
8. From the same disposable copy, use File > Build Profiles, select Windows
   x86_64, include Bootstrap, enable Development Build, and build to a new isolated
   folder such as `Builds/R0OwnerQualification`. Retain the existing development
   diagnostics feature flag so **Development tools** and the setup control are
   available; do not change it in the normal project. Keep the qualification Product
   Name. With Editor stopped, preserve/rename its isolated save for evidence so the
   player starts fresh. Launch `Dungeon Lord.exe` and repeat steps 2–7, closing and
   relaunching the executable at both reopen checkpoints. Do not run Editor and
   player against the same qualification save simultaneously.
9. Report pass/fail for Editor and Windows separately. Supply commit/build
   identity, isolated save path, start/partial/ready/claimed/reopened screenshots,
   saved-state copies or hashes at reopen checkpoints, objective/unlock outcome,
   duplicate/blocked-action outcomes, and relevant Editor/Player logs.

Pass: both paths acknowledge each eligible transition once, reopen consistently,
retain coherent unrelated state and localized feedback, and show no exceptions.
Fail: false success, lost acknowledged research, duplicate effect, stale active
project after claim, unrelated-state loss or broken feedback. Report the first
failing step with its save/log evidence; do not modify the normal player save.
