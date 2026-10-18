# Living Dungeon B3A qualification

Status: **controlled B3A checks passed; overall qualification incomplete.** Two graphical assertions reproduce with baseline production source, and five historical screenshot originals remain unrecovered. This record does not waive those failures or claim completion of the owner's preservation requirement. The PR must remain a draft and unmerged.

Branch: `codex/living-dungeon-b3a-terminal-settlement`. Fetched baseline: `e5abe082a8a51a00845dfff5c735fbb3eb9edb8d` (#241). GitHub metadata confirmed #233–#241 merged. Initial feature commit: `2fa5a30c117cbcfdeea8121e1a8396642e8e65e8`. Corrected runtime/test commit: `b5502291e9da3d229bbcc3d152f8bd56954d84eb`. Actual turn metadata: GPT-6.1 Sol, High; Complex, well-defined cross-system implementation. No model fallback or delegation was used.

## Implemented qualification boundary

The [implementation contract](../../../planning/living-dungeon-phase-b3a-implementation-contract.md) records the reviewed transition. The Editor-only synthetic foreground scope observes admission and every committed progression transaction, pins complete predecessor bytes and each party's complete checkpoint identity, and never adopts an existing party. A new scope after restart has no provenance. A live scope can continue across byte-identical session reopen. An unobserved write invalidates its binding.

Selection reconstructs all terminal records before checking eligibility. It verifies the actual last Terminal boundary, checked ordinal arithmetic and RunId, then orders by checked absolute completion time, numeric sequence and terminal ordinal. The ineligible earliest record blocks later retirement. An issued request is a scope-owned nonce; old, fabricated or consumed requests cannot select another party.

One detached candidate consumes the selected B1 outcome, applies the resolved run Heat delta to current canonical Heat, then the existing separate loot cooling/clamp stage. Material/configuration compatibility and current knowledge preservation guard survivor learning. Only the individually verified configured first-session objective can be completed; a changed objective configuration skips it. No research, new reward/mana source or C2 injury is inferred.

Current configured bounded history appends the already allocated RunId, retains Latest/final Recent alias validity, and changes NextRunSequence zero times. The exact transition authorization preserves retained outcome nodes and history extensions/nulls. The selected party is removed and its local semantic revision increments once. Scheduler fields and other checkpoints are preserved. Derived storage/replay limits release the retired record's obligations.

The existing complete-save preparation, atomic writer, durable readback and recovery-required contract remain authoritative. Failed proven rollback can retry the same request; uncertain rollback blocks the scope. Schema 15, frozen readers/migrations, B1 format 1 and `run.executable.living-dungeon-b1.v1` remain unchanged. Production has no capability/settlement entry point or GameRoot/SaveService/UI caller. B2 advancement still retains terminal parties.

## Executed tests and corrections

Unity 6000.3.2f1; Unity CLI 1.0.0-beta.12. The existing root project/Library is used. No additional Unity project copy or Editor installation was created. Commands and exact arguments are recorded under `TestResults/living-dungeon-b3a-20261017/*-command.json` and will be included with final report evidence.

| Attempt | Check | Passed | Failed | Skipped | CLI exit |
| --- | --- | ---: | ---: | ---: | ---: |
| 01 | Initial B3A EditMode | 28 | 1 | 0 | 8 |
| 02 | Expanded B3A EditMode | 32 | 3 | 0 | 8 |
| 03 | Corrected B3A EditMode | 38 | 0 | 0 | 0 |
| 04 | Committed B3A EditMode | 39 | 0 | 0 | 0 |
| 05 | Full EditMode, initial implementation | 2,173 | 2 | 1 | 8 |
| 06 | Final B3A EditMode, `b550229` | 41 | 0 | 0 | 0 |
| 07 | Final full EditMode, `b550229` | 2,175 | 2 | 1 | 8 |
| 08 | Relevant PlayMode integration, `b550229` | 7 | 0 | 0 | 0 |
| 10 | Baseline-source reproduction of graphical failures | 0 | 2 | 0 | 8 |

Attempt 09 Windows Development compilation/package validation succeeded, CLI exit 0, 0 build errors and 1 warning: Unity services could not upload native symbols. The player contains all 52 required artwork assets and the isolated save namespace; `SyntheticForegroundQualification`, `SettleSyntheticTerminal` and `LivingDungeonTerminalSettlement` are absent from its managed assembly. The preserving output is `Builds/Development/Windows-living-dungeon-b3a-20261017/Dungeon Lord.exe`. Its 303 files total 182,194,196 bytes including reports/debug information; the Unity build report's player size is 181,721,101 bytes. The player was not launched for owner gameplay UAT. No production B3A behavior is enabled.

The final full run includes 37 B1, 82 B2, 42 Phase A, 42 C2, 20 Heat, 33 knowledge, 14 manual branch-integration and 65 production build-gate cases, all passed. Migration-related cases record 83 passed and the existing platform-specific skip `CurrentNonWindowsRuntimeFailsClosed` on Windows. Category counts overlap and must not be summed as unique tests. All other full-suite cases passed except the two named graphical assertions. The PlayMode run covers Phase A/B1/B2/B3A/C2 integration, including actual frame isolation and explicit settlement/reopen; it is not a full PlayMode suite claim.

Exact final commands (absolute executable/arguments, UTC start/end times, exit codes and test verdicts are in `commands.json`):

```powershell
& Tools/Presentation/run_b3a_qualification.ps1 -Stage focused -Attempt 06
& Tools/Presentation/run_b3a_qualification.ps1 -Stage full-edit -Attempt 07
& Tools/Presentation/run_b3a_qualification.ps1 -Stage integration-play -Attempt 08
& Tools/Presentation/run_b3a_qualification.ps1 -Stage build -Attempt 09
& Tools/Presentation/run_b3a_qualification.ps1 -Stage baseline-ui -Attempt 10
```

The baseline command ran between `qualify_b3a.py begin-baseline-ui` and `end-baseline-ui`. The actual chronology is recorded in command timestamps. `baseline-failure-comparison.json` compares the two exact failure messages. `qualification-reports.zip` retains all nine NUnit reports, earlier failures, source/preservation manifests and command records. Raw Editor logs remain local with hashes and compiler diagnostics indexed in the archive. Python helper compilation and code/authored-document whitespace checks passed. The unrestricted whole-diff whitespace check reports nine trailing-space lines in the verbatim engine-generated `build-messages.txt` and `packed-source-assets.txt`; those raw evidence copies are deliberately retained. Final runtime/tests were qualified at `b5502291e9da3d229bbcc3d152f8bd56954d84eb`; subsequent changes are evidence/tooling only. Refreshed `origin/main` still matched the baseline before publication.

Initial fixture corrections retained the original reports: reset filesystem operation history before injecting the second rollback failure; use valid B1 party-size bounds and actual reached encounters for wipe evidence; target the fourth active-file read for post-replacement durable readback failure instead of the third preflight read. The runtime-extension assertion was corrected because B2 progression had already normalized that unrelated nested field before B3A; history-level and retained-outcome extensions remain explicitly tested. The settlement preparation now surgically preserves unrelated recognized state and only patches Heat, history and verified objective fields.

The two full-suite failures were reproduced with baseline production source and all B3A additions excluded, using the same preserved environment. `baseline-ui-context.json` records the exact input substitution; feature inputs were restored byte-for-byte and the 1,340-input applicability check passed afterward. These are unresolved baseline/environment failures, not waived assertions:

* `RoomContentMouseTouchPreviewAndResumeKeepCanonicalIsolated`: expected valid content preview at `ProductionDungeonRoomContentSceneTests.cs:879`, received false.
* `TransactionalRoomConstructionLayoutsLocalizationAndResume`: expected viewport height greater than 150, received 122 at `PhaseSevenA4ProductionSceneTests.cs:341`.

No historical assertion was removed or weakened. The first full report and baseline reproduction remain retained. An initial temporary-source substitution doubled CR whitespace in four test files on Windows; those exact inputs were archived, the helper was corrected to write bytes, and normalized source comparison then passed. No verbatim literals or test assertions were changed by that correction.

## Workload evidence

Final focused sample: two retained parties, 50 total committed replay events; complete bytes decreased from **114,098** to **70,961** after one settlement. Selection took **84.7737 ms**, and the detached atomic transaction **165.3432 ms**, including complete validation/replay and test-harness costs on this Editor host. This is one bounded sample, not a device responsiveness or multi-month catch-up guarantee. The retained-limit fixture proves four records can release storage/replay obligations and admit another controlled party after one retirement. Existing independent parser/checkpoint/replay limits are not expanded.

The real Windows filesystem test interrupts after actual atomic replacement, proves rollback, retries once, validates durable reopen and rejects the duplicate request. The resulting one-party retired save is 18,668 bytes. Synthetic files use fresh GUID directories inside the B3A evidence directory; owner saves are not test fixtures.

## Preservation incident and remaining originals

At task start, the audit hashed **6,839** pre-existing save/player files and backed up **1,340** Unity inputs, including five pre-existing modified Unity settings/font assets. A separate baseline hashed **1,991** historical evidence files. SaveService/build configuration and static legacy test-save names are redirected to unique B3A names during execution. Scene-specific evidence paths were also redirected.

The first full suite exposed a mistake in that setup: the shared `ProductionDungeonScreenshots` helper still used its old output directory. Ten pre-existing screenshots were overwritten. The shared helper is now redirected, replacement captures were preserved, five exact originals were restored from independently retained hash-matching copies, and the current historical evidence set was archived before any further graphical test. **Five original screenshots remain unrecovered.** Archive and prior image-output searches did not find byte-identical originals. Regenerated or visually similar images are not accepted as restoration.

| Original file under `TestResults/phase7a4-screenshots` | Required original SHA-256 |
| --- | --- |
| `combat-c2-reserve-ui-1920-Default.png` | `0957c9196f7202c68bb3d69b69315256d6089892d1e5cbd2c702d0b77b91f1d3` |
| `combat-c2-reserve-ui-1920-Large.png` | `ffe861491f8d09c2599416334b4b7d3871997cf592b75cae432aee299e1ca69f` |
| `combat-c2-reserve-ui-720-Default.png` | `5f812a2668e34ebb3a72e6aeb208edb17ee6c1ef6c15fde87682a108ef774881` |
| `combat-c2-reserve-ui-720-Large.png` | `fcf284148832be7a8d858a65b83ab12d7babb2cb202ebf8f72aff85a15694a03` |
| `combat-c2-reserve-ui-placement-1920.png` | `c6f15c1d0b3c65140ad4b6d0419572a302e31928682201e2bb248f6b09b5434e` |

The owner has been asked for another backup location. The original hash manifest remains unchanged; the incident is not described as full evidence preservation. Final audit: **6,839/6,839** original save/player files match; **1,986/1,991** historical evidence files match, with exactly the five screenshots above outstanding. All temporary namespace/output/test-source substitutions were restored. The 1,340 Unity-input comparison passed against committed source with only the five original local settings/font asset overrides, which were restored to their exact starting bytes. Those local changes are excluded from the feature commits. No prior player/build output was overwritten, and no new Unity project copy or Editor installation was created. `qualification-status.json` correctly records `complete: false`.

## Scope limitations and B3B prerequisites

This implements controlled B1 qualification only. Authorization is volatile and cannot be reconstructed after process restart. Learning deliberately skips incompatible material/configuration and records changed since admission. Detailed presentation remains outside the persisted coarse history contract. No production arrivals, settlement loop, wall-clock mapping, offline/mixed attribution, C0/C2 durable execution, floor occupancy/waiting, additional content/rewards or balance changes are activated.

Before B3B production integration, separately approve and qualify trusted period provenance/restart rules; production durable executable support; configuration-owned time mapping and admission demand/structural capacity; one-party-per-floor admission and durable descent waiting; causal ordering among admissions, terminal effects and edits; individually eligible production economics/objectives; mixed-period Heat/extraction attribution and offline loss protection; exact resumable catch-up with measured storage/device bounds. Live presentation is a later dependency. The baseline UI failures and missing originals above remain unresolved and are not silently accepted as scope limitations.
