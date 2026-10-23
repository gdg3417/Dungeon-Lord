# PR #249 qualification preparation recovery

Current status: automated qualification and scripted native continuation pass. After manual dialog dismissal, visible native checks made progress but found a repeatable windowless player shutdown failure. Further native work and owner preparation stopped. See the [2026-10-23 native continuation and preservation closeout](native-ui-continuation.md). Owner UAT is not qualified or launched.

## Source and isolation

Source `c870c5dc92aee87e854af350aa0a73653d045ff2`, branch `feature/phase7-production-analysis`. Its original Unity inputs are unchanged from `d4071924993c67f2b22722a539be37eea781017e` / implementation `948a38dc6f47b1419164fdfe8f44a2b3c9fd31f5`. Current main remains `fbe4bdff0cba9fea72b42fb6a05714327f880df4`; original implementation baseline remains `7dc6f96b5c35f24923f6e8cbfa8a5e256c16c3ac`. No merge/rebase/cherry-pick or production/test-source branch edit occurred. Standard / GPT-6.1 Sol Medium recommendation; no fallback was requested.

The earlier final campaign remains failed at **2,345 passed / 7 failed / 1 skipped**. Its reports and source identities are unchanged. This recovery uses unused `TestResults/phase7-analysis-recovery-20261022-r2` outputs and `Temp/ui-composition-analysis-p249r2-{focused,edit,play,native,nativefs}` projects. Each project's 1,418 Unity inputs are checked against source plus declared temporary transformations. The fifth project is the separately documented Windows Player filesystem test route, added after both full suites passed.

Qualified product: `DL B3B2 Smoke Two Floor P249R2`. Company `gdg3417` and application identifiers `com.gdg3417.dungeonlord` remain unchanged. The product satisfies both existing smoke substrings. The longest checked canonical/draft sidecar path is 221 characters against the existing 240-character envelope. This is an isolated Development/test product, not an ordinary player distribution.

Six input paths receive qualification-only changes in each disposable copy:

- Bootstrap `build_config.json` and `SaveService.cs`: only the approved save filename substitutions, using `living-dungeon-b3b2-qualification-two-floor-p249r2-<stage>.json`.
- `ProjectSettings.asset`: exact product-name substitution.
- `DevelopmentBuildUtility.cs`: exact ProductName constant aligned with that product; validation logic unchanged.
- `BuildReadinessTests.cs`: only its exact positive identity argument aligned with that product; all negative tests unchanged.
- `PhaseSevenA4ProductionSceneTests.cs`: fixed starter filename replaced by `p249r2-starter-<fresh GUID>.json` per execution. `File.Copy(..., false)` remains unchanged, including the PlayMode reflection entry.

Per-stage transformation records retain original/qualified SHA-256, exact values, reason, scope and restoration procedure. The repository originals are untouched. External golden JSONs are copied to their equivalent tracked relative paths and inventoried separately. The B1 file is LF in both Git and checkout; the C2 checkout is CRLF while its Git blob is LF. Git-filtered identities match the source commit, and exact checkout/destination SHA-256 match. No Base64/checkpoint payload is regenerated. Two static preparation aborts (initial overly strict raw blob comparison, then a non-overwrite guard on the already copied first fixture) are retained; both occurred before Unity. Completion retained the verified existing campaign copy without overwriting it.

Fixed screenshot and two-floor paths remain unchanged in test code. Explicit process working directories and distinct fresh projects root those paths separately for every suite, so no old campaign or previous-suite manifest/capture is selected. Seed manifests remain products of the existing canonical fixture writer, with product/hash/prefix/path checks. All ten machine-readable static readiness checks passed before focused execution.

## Actual automated results

Unity 6000.3.2f1, CLI beta.14; source `c870c5dc92aee87e854af350aa0a73653d045ff2` with only the declared per-stage qualification transformations.

| Gate | Passed | Failed | Skipped | Total |
| --- | ---: | ---: | ---: | ---: |
| Focused historical goldens, owner starter, two-floor waiting/layout | 4 | 0 | 0 | 4 |
| Complete EditMode, no exclusions | 2,352 | 0 | 1 | 2,353 |
| Complete PlayMode, no exclusions | 3,741 | 0 | 10 | 3,751 |
| Native Windows filesystem fixture | 1 | 0 | 0 | 1 |

Reports `01-prerequisites.xml`, `02-full-edit.xml`, `03-full-play.xml` and `04-native-filesystem.xml` live under the fresh output root. Unique leaf IDs match complete root counts; no inconclusive cases. EditMode ran 2026-10-23 00:24:01.465–01:44:19.597 UTC (80m18s including import/shutdown). PlayMode ran 01:51:57.772–02:14:54.089 UTC (22m56s including import/shutdown). Domain-reload root duration is not substituted for CLI wall duration. Timeouts were 1,800 / 14,400 / 10,800 seconds respectively.

The single EditMode skip is the existing non-Windows inverse. PlayMode retains eight synchronous-Editor-fixture skips, that inverse, and the player-only filesystem fixture. Exact names/reasons remain in XML. The latter then executed in a real StandaloneWindows64 player through `Docs/planning/test-stage-matrix.md`'s documented raw Unity CLI route, with a 1,800-second process bound and a fresh test-player directory. It was not counted as a pass while skipped in Editor.

Three sustained methods inside the required full EditMode suite each passed with 68 completed runs: bare production, matched one-active-floor, and matched two-active-floors. The two-floor capture has 6,256 concurrent-party frames. Their raw report hashes and summaries are retained. Thread allocation counters are unavailable; frame counters are available. Combined retained checkpoint size is an aggregate, not an individual-checkpoint size. Scope timings are inclusive/nested and tags overlap. No standalone sustained campaign, new instrumentation, optimization or numerical speedup claim is introduced. These are Editor workload results, not Windows/phone responsiveness acceptance. The existing bounded cache/idle/layout checks also pass; the Analysis cache still captures on retained-array publication rather than frame ticks.

## Build and native continuation

The unchanged `DungeonVisualBuildQualification.BuildWindows` authority produced `Temp/ui-composition-analysis-p249r2-native/Builds/Development/Windows/Dungeon Lord.exe`. Build result Succeeded, StandaloneWindows64, Development=true, Bootstrap-only, zero errors and one warning. The warning is an empty Unity-services URI preventing native-symbol upload; local output remains available. No service/security setting was changed to fix it.

Artifact identity: `P249R2-fe8daae0264348579db73d3402254215`, 303 sealed files. PE machine is AMD64 (`0x8664`), the exact isolated product string is packed, and ordinary-player NUnit/test-runner qualification assemblies are absent. Player SHA-256: `abc8179e345b70c9d7ca423c54e02739ed9b9adca668c2c745ffb81aadc05cc6`; gameplay assembly SHA-256: `bec017cef152733a88087d159c875b8f4ef4642a098840a62ca5e2328af6cd58`. The production packed-art validation completed.

Existing opt-in Development hooks ran in separate native processes against fresh canonical fixtures and save names, with sealed-build checks before each launch:

- `06-native-wait`: genuine two-party waiting boundary, visible-marker/floor switching checks, and active draft commit without rewriting captured checkpoints.
- `07-native-release`: exact closed-prefix reopen, one blocker settlement and causal release of the waiting party.
- `08-native-finish`: another exact reopen, runs 1 and 2 settled once, run 3 active, next sequence 4. This genuine save supplies retained history plus an active automatic party.
- `09-native-v1`: unchanged historical v1 prefix restored and settled once; history count 1, next sequence 2, current profile activated.

All four player reports say Passed and processes exit 0. Canonical files are read/copied only after exit. The first external closeout checker rejected the wrapper `primary` as a changed root member; a recursive audit proved the sole change was `primary.lastSavedUtcUnix` advancing by one second at shutdown. The original checker failure remains recorded, with a separate same-execution correction audit. Later checks compare exact recursive paths and allow only documented save/pause UTC metadata; they do not normalize gameplay or history. No save bytes or player code were changed to satisfy that check. Native shutdown ComputeBuffer cleanup warnings remain in raw logs; no memory-leak conclusion is inferred from shutdown allocation telemetry.

## Generated inputs and preservation

Before Unity, 350 original files across existing company-product data directories were backed up/hash-verified, and a frozen list of 26,181 prior build/report/evidence files was hashed, including prior disposable reports. Original owner starter files were never removed or overwritten. Each completed stage verifies all 350 original persistent hashes.

The suite-generated TMP fallback font and settings key order were archived and restored after Editor exit. The native filesystem test's stricter closeout checker additionally rejected build-generated URP prefilter/runtime lists, Standalone batching defaults and UnityConnect enabled state. The failure is retained. Exact diffs were inspected against the established Phase 4/Phase 7 build-serialization precedent, archived, then restored after exit. The production build generated the same four cache/default updates and received the same controlled closeout. No unlisted difference is accepted at the next launch; all affected projects return to their 1,418-file declared qualified input inventory. Repository originals remain unchanged. Default persistent `TestResults.xml` side effects occur only in the new product and are snapshotted per stage before later runs regenerate them.

The follow-up hash comparison completed: all 26,181 historical files and all 350 original persistent files are byte-identical, with zero exceptions. The focused, EditMode, PlayMode and native-filesystem projects have closed their input transformations: each now matches all 1,418 original source files byte-for-byte, retaining archived qualified versions. The native build project still has its six declared qualification transformations while visible UI work remains open. Its generated build caches have already been restored. No live canonical file from the still-running GUI player was read for this audit.

## Visible UI checkpoint before manual dismissal (superseded)

The ordinary, unmodified-clock native player was launched with the genuine `08` fixture at requested 1280×720 for UI verification. It is isolated from owner progress. Computer Use app capture initially timed out; the refreshed retry succeeded but showed a Windows Security firewall dialog for the already completed `PlayerWithTests.exe` over the desktop. No UI action was sent. The skill forbids acting on security permission requests; manual Cancel was requested. Networking permission is not needed for the completed test.

Visible native Analysis navigation, actual native viewport/readability captures, draft-choice/replay interactions, the final GUI save/reopen comparison, and dedicated owner fixture/launcher preparation remain pending. No physical phone/Moonlight acceptance is claimed. No owner UAT handoff is ready, and the PR remains draft/unmerged. This status must be updated after the blocking desktop dialog is dismissed and native UI work continues.

The ordinary isolated GUI player remains open under recorded PID 38808. The latest native-window capture still shows the security dialog; no click/keypress was sent. Codex requested manual Cancel, not firewall authorization. Technical build/canonical fixtures are available, but the incomplete visible native checks prevent an owner-UAT readiness claim. Recommendation: **still blocked at the native UI gate**, pending that manual desktop action; no demonstrated production defect or additional code correction is identified.

See [machine-readable applicability and preservation](preparation-recovery-applicability.json) and [reports, declared input transformations and scoped preparation programs](preparation-recovery-reports.zip). Raw Unity logs, private original-file backups, full profile frames and canonical fixture snapshots remain local under the fresh output root. Native continuation uses the existing injected technical clock and does not establish physical phone acceptance or ordinary-clock Analysis navigation.
