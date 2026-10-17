# PR #241 focused review corrections

Reviewed baseline: `fe284ec91773720f852e45be570f0a253a5596ca`; branch `codex/phase7-production-floor-lifecycle`; main verified at `7a664efc5e38e0d3522e1ddbe6cd3747b14e61ce`. Working tree was clean before investigation. Model-policy classification: Complex but well-defined; recommendation GPT-6.1 Sol High; availability fallback GPT-6 Sol High, then GPT-6 Astra Low. No governance changes.

This evidence qualifies the focused corrections. The parent directory's reports, captures, samples and source hashes remain unchanged historical evidence for `fe284ec`; they must not be treated as executions of these corrected sources.

## Finding 1: avoidable refresh work

`content_bootstrap.json` configures 10-second foreground ticks and 30-second periodic saves. `TimeService.Update` increments `totalTicks` on each consumed tick while unpaused, not every rendered frame. `GameRoot.HandleSimulationTick` applies research progress and passive mana in memory; periodic saving/publication occurs separately. Opening floor management does not pause either path. Thus a tick alone previously caused both full previews every ten seconds, even at a capped wallet. Opening/presentation and canonical publications could cause additional previews.

Lifecycle preview validates the complete owned save, clones detached spatial state, prepares a replacement, round-trips complete-save validation and materializes the Active run snapshot. Construction preview also reparses the owned complete save and validates a detached construction candidate. These create avoidable strings, byte arrays, graphs and snapshot objects when eligibility has not changed.

The test-only probe opens an eligible Floor 2, then invokes the actual controller Update path for 12 simulated ticks with Heat and small mana increments, without session publications. It counts replacement preview objects, rather than adding production counters/logs. The initial measurement executes the reviewed implementation with only the test probe added. The corrected probe asserts both counts are zero.

| Same 12-Update probe | Lifecycle previews | Construction previews | Total Update time |
| --- | ---: | ---: | ---: |
| Reviewed implementation | 12 | 12 | 338.828 ms |
| Corrected implementation | 0 | 0 | 3.298 ms |

Timing is one local deterministic probe per version, includes HUD/input/reflection work, and is not a hardware-independent frame-time guarantee. It simulates two minutes of configured ticks without waiting two minutes. Real foreground operation also invalidates on each periodic publication. Mono's thread-allocation counter returned zero in both measurements; this is not reliable allocation evidence and no zero-allocation claim is made.

The correction separates existing HUD/resource updates, cheap cache invalidation, authoritative recalculation and cached presentation:

- Tick and ordinary Heat changes update the HUD without invalidating floor previews.
- Mana is checked independently of ticks. The existing SaveService authority compares its cached quote with the current price/reserve affordability **and blocker reason**. Recalculation occurs when either changes, including insufficient direct price becoming insufficient recovery reserve while still disabled. Mana movement inside one result/reason range reuses previews.
- Required reserve derives from Active-floor monster assignments and immutable configured readiness, not current HP/revival status. Valid ownership/layout/activation changes publish canonical state; those publications invalidate. Mutable mana can change without publication and is polled. A scoped HP-change test verifies that HP alone does not change the obligation.
- Completed floor-permit research is checked every open-panel Update, independently of mana/ticks. Other pending/progress changes do not grant permission. Construction/activation consume the same existing permission resolver.
- Canonical publication, changed canonical session/state reference, selected floor, opening/reopening and returning from editing invalidate. Publication remains a conservative full-revalidation boundary even when its changes were only recognized save metadata.
- Existing ConfigureCanonical, ConfigureFloorConstruction and ConfigureRunLoot entry points advance a transient configuration version. Reconfiguration, including replacement recovery rules/context, invalidates without requiring a publication. There is no supported live in-place configuration-editing workflow; arbitrary mutation of exposed config DTOs is nevertheless detected by the commit binding and covered by tests. A direct lifecycle click also always recalculates against current inputs.
- Closed panels and Edit Mode perform no floor-management previews. Returning from editing/discard and reloading cannot reuse the old panel baseline. No worker, delay, background task, persistent state or new writable owner is added.

The cache does not authorize a commit. Construction still uses its existing atomic revalidation. A direct lifecycle click takes a fresh preview before committing or opening confirmation, avoiding needless rejection after harmless mana gains. The modal retains that exact preview even if the underlying panel recalculates while it remains open.

## Finding 2: session bytes did not bind unpublished inputs

Mana, Heat, tick metadata and research can change in memory while canonical session bytes remain unchanged. The former binding only checked action, floor ID and those session bytes. A repeated preview could therefore prepare a different recognized-state candidate. Runtime configuration DTOs are also mutable references.

The corrected transient binding captures `JsonUtility` representations of the recognized runtime input, run configuration, **separate nonserialized CombatC0 configuration**, and loot configuration. Immutable production/profile/research/context identities are bound as well; the context owns the immutable recovery rules. Serialized runtime capture is deliberately conservative, covering all serialized recognized inputs rather than only today's permission/mana fields. Canonical spatial/content/continuity owners remain bound by complete session bytes and existing canonical equality/validation, not a second UI owner.

Commit first checks the captured action/target/session/input binding. It then repeats the full current authoritative validation and compares the recomputed canonical-order changed-floor IDs/indices and no-op state with the presented preview. Mismatch returns existing `floor.lifecycle.stale_preview` before persistence. Selection is checked separately by the controller. The existing disk/session check and atomic transaction remain intact. No mutation or resource debit occurs on rejection; cancellation never calls commit.

This intentionally rejects benign unpersisted tick/Heat changes during a confirmation as well as material changes. The player must review again. Ordinary direct actions refresh synchronously before commit, so they do not require repeated clicks after normal ticks. Current two-floor content cannot produce a real cascade; the modal regression forces the existing presenter with a real deactivation preview to test the captured delegate, while the existing explicitly synthetic cascade tests cover affected-floor presentation/cancellation. No third production floor is added.

Existing Bootstrap calls omit the optional expected preview and continue to perform a fresh current-state validation. All 68 existing A4 tests, including Bootstrap integration, remain passing. Existing authorized recovery/atomic save semantics are unchanged; the same bound context/session/runtime inputs feed those deterministic authorities.

## Exact changes

| File | Focused change |
| --- | --- |
| `ProductionDungeonController.cs` | Poll meaningful invalidation independently of HUD tick updates; invalidate on publication. |
| `ProductionDungeonFloors.cs` | Cache presentation previews by session/state/selection/permit/config inputs, refresh on resource-result transitions, refresh direct lifecycle clicks, retain modal's original binding. |
| `SaveService.cs` | Read-only permission/resource-result checks and transient configuration version on existing configuration entry points. |
| `FloorLifecycleAuthority.cs` | Captured unpublished/config input binding, authoritative reserve quote metadata, recomputed-consequence equality. |
| `PhaseSevenFloorLifecycleTests.cs` | Eight unpublished-input cases, changed consequences, context replacement and Bootstrap compatibility. |
| `ProductionDungeonFloorSceneTests.cs` | Measured tick probe, meaningful invalidations, open-modal changes; existing cascade presenter test adapted to fresh-click semantics. UAT export/capture paths now use each fixture's unique namespace to preserve previous artifacts. |
| `PhaseSevenA4ProductionShellPlayModeTests.cs` | Three wrappers through the existing reflection bridge. |

No UXML, USS, localization, prices, tuning, content, gameplay systems, schema-15 owner or migration changes.

## Executions and applicability

Unity 6000.3.2f1, only `C:/Dev/Dungeon-Lord`, sequential processes. [Exact commands](commands.md), [actual NUnit XML](qualification-reports.zip), [all exact executed test names/counts and probe output](qualification-manifest.json), [final tested source hashes](tested-inputs.json).

| Run | Passed | Failed | Skipped | CLI exit |
| --- | ---: | ---: | ---: | ---: |
| Baseline measurement only | 1 | 0 | 0 | 0 |
| EditMode 01 | 154 | 0 | 0 | 0 |
| PlayMode 01 | 7 | 0 | 0 | 0 |
| PlayMode 02, affected final correction | 2 | 0 | 0 | 0 |

No failed/blocked attempt occurred in this focused review. EditMode comprises 21 PhaseSevenFloorLifecycleTests, 68 PhaseSixA4 checks and all 65 production content/build gates. It includes unchanged target/stale-session/localization/UXML checks plus new rejection cases for unpublished mana, research, Heat, ticks, recognized metadata, run/combat/loot configuration; configuration/context replacement; and differing presented consequences.

PlayMode includes the four original GraphicalFloors scenarios and the three new `GraphicalFloorReviewTickProbe`, `GraphicalFloorReviewMeaningfulInvalidations`, and `GraphicalFloorReviewOpenConfirmationRejectsUnpublishedChanges` scenarios. The probe asserts zero repeated previews. Invalidation coverage includes resource/reason thresholds, research without ticks/publication, HP-versus-reserve, deployment publication, config reconfiguration, session publication, selection, close/reopen, editing return, and successful activation/deactivation. Modal tests preserve state/bytes after wallet and research changes.

After 154/7 passed, a final three-line resource-helper extension refreshed construction's blocker reason even when affordability stayed false; its scene assertions were added. Only its two directly affected construction/resource scenarios were rerun (2/2). No lifecycle authority code changed after 154/7; other tested paths do not call the changed construction-reason branch. Counts overlap and must not be summed as distinct tests. Historical full-suite results are not claimed for this correction.

All 45 preexisting saves/sidecars remain byte-identical: [baseline](save-baseline.json) and [final audit](save-final-audit.json). Protected `combat-c0-diagnostics-validation-20261015.json` remains SHA-256 `54C7744E7EE92B1597DAA88DF995EF07C120E34667398377BE78591F7FB7AA13`. Unity refreshed its generic app-data TestResults.xml. Fixtures use new disposable save names; the additional qualification guard filename is restored from exact original build-config bytes. The incidental Unity settings dictionary reorder is also restored. No original evidence, historical save, validation workspace or project copy was reset, overwritten or removed. Raw logs remain locally under `Logs/phase7-floor-review`; they contain Unity authentication arguments and are not published. Report archive token-argument check found zero matches.

Ready for independent re-review of these two concerns. Owner Unity qualification, Windows standalone verification and merge approval remain pending. No owner testing or merge was started. Remaining limits are the current two-floor cascade restriction, conservative modal expiry on any recognized change, and local-probe timing rather than a general performance certification.
