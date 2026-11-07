# H1-F4 regression hardening — independent review milestone

Status: focused correction milestone ready for independent PR review. PR #259 remains draft. Full H1-F4 qualification, performance assessment, owner UAT and merge readiness are pending.

## Source and scope

- Repository: `gdg3417/Dungeon-Lord`; branch: `codex/h1-f4-recovery`.
- PR: <https://github.com/gdg3417/Dungeon-Lord/pull/259>.
- Main/base: `3eb6ccf93558b8145d64e9e63cbefcbd73484602`.
- Reviewed starting head: `16c564d0f78d50c2cbf8b033a028db47872fa133`.
- `534b4d1ff4f8bb8f38dac468ba27f2dffb2d60b7`: author complete B3B1 configuration before canonical ownership; add exact identity-guard regression and explicit five-case selector.
- `d8537eadeab96e363ffa88fae7c62712fab4f5b8`: instrumentation fault pumps use the existing canonical observation/transition helper; add explicit 28-case selector.
- `796a959bfe5f90eca9e5794faccdfd34b05ea79c`: distinguish historical mixed-profile reader compatibility from schema-17 writer authority; own B3B1 exports inside the campaign; add explicit 51-case selector.

Recommendation remains **GPT-6.1 Sol High, Complex**, for cross-system fixture, canonical ownership and qualification investigation; GPT-6 Sol High is the availability fallback. No Astra use or recommendation change was required.

This milestone changes two test files and the existing qualification preparation tool:

- `Assets/_Project/Tests/EditMode/LivingDungeonB3B1Tests.cs`
- `Assets/_Project/Tests/EditMode/ForegroundPerformanceInstrumentationTests.cs`
- `Tools/Presentation/qualify_h1_f4.py`

The accompanying three reports are documentation only. No production runtime, frozen serializer, migration, schema, policy resource, checkpoint format, localization or gameplay tuning changed in this milestone. No assertions were ignored or disabled.

## Confirmed causes and corrections

**Consumed-configuration identity.** B3B1's shared `Dungeon` factory replaced `f.Configuration` after `Fixture.Create` had constructed and bound the canonical owner. Schema 17 deliberately requires the actual consumed configuration instance. The first failed guard is `DetachedCanonicalWriteAuthority.recoveryBindingsValid == false`; its private `ValidateSession` returns null before candidate preparation or persistence. The outward reason is the existing `gd66.write.first_write_validation_failed`.

The new `SchemaSeventeenWriterRejectsReloadedConfigurationBeforeCandidatePreparation` test proves that even a later independently parsed configuration with an identical legacy projection fails this guard, preserves authoritative bytes, and succeeds when the actual consumed object is restored. It does not rebind an existing save. The corrected shared factory authors the full validated configuration bytes, including CombatC0 and the existing synthetic descent inputs, before constructing either the one-floor or two-floor owner. Original prefix, outcome, combat-work, immutable-capture, corruption and ceiling assertions remain.

**Instrumentation fault-path time capability.** The instrumentation helper supplied a raw wall timestamp to the detached foreground writer. It now delegates to the existing `PhaseFourTestSupport.PumpForeground`, retaining the consumed two-floor policy and using a source-bound canonical time cut. The failed persistence case reaches its intended protocol cut. Enabled/disabled exact canonical equality, rollback, readback, historical readers, per-party inspection and origin/reuse assertions pass. This extends the already independently reviewed Review-2 fixture correction; no production reason code or transition changes.

**Historical versus current profile authority.** The mixed B1/C0/C2 test previously installed an unbound synthetic historical profile into a current schema-17 validation context. That is incompatible with persistent context continuity and supported execution policy. The test retains its scheduler, mixed-executable, aggregate replay, malformed deadline, terminal-settlement refusal and exact checkpoint assertions. Its positive complete-save roundtrip now explicitly uses the frozen schema-16 reader on an authored test-only historical input. It separately proves that binding the unsupported profile to current recovery inputs throws `time.recovery.context_mismatch`, current preparation is refused without writing, and historical reader validation grants no writable session.

`SyntheticHistoricalMixedOwner` is a local test input author, not a historical serializer change, production migration, release mapping, producer proof or activation path. Its output uses the existing legacy time grammar, preserves the fixture's accounted seconds, and is checked by the unchanged frozen complete reader and canonical roundtrip. No historical input file is changed. This test does not qualify producer-bound activation of an actual historical campaign.

**Export ownership.** The B3B1 exporter no longer writes under the retained `living-dungeon-b3b1-20261018` directory. It requires the isolated product, batch mode, exact supervisor directory ancestry, fresh output directory, permitted stage and non-reparse paths. Its input is sealed in the new campaign's `fixture-inputs/b3b1-smoke-input/input.json`. No frozen adapter was changed.

No production gameplay/save defect was confirmed or corrected by these passing groups.

## Automated qualification

| Campaign | Exact source | Selected cases | Passed / failed / skipped / inconclusive |
| --- | --- | --- | --- |
| `b2850722722e41c9b9e44121bde75059` | `534b4d1ff4f8bb8f38dac468ba27f2dffb2d60b7` | Identity guard + four one/two-floor exact-prefix variants | **5 / 0 / 0 / 0** |
| `b0a271b3daaf4003bc27c2a5e040783b` | `d8537eadeab96e363ffa88fae7c62712fab4f5b8` | All admission reconstruction 18 + instrumentation 10 | **28 / 0 / 0 / 0** |
| `3dfdc7f0ceb44b458c6aa97d7e7bf260` | `796a959bfe5f90eca9e5794faccdfd34b05ea79c` | Complete B3B1 cohort, including new guard case and exporter | **51 / 0 / 0 / 0** |

All five supervisor stages passed for each campaign: direct preflight, workload, restoration preflight, restoration and final preservation. Root/discovered/leaf counts agree. The five representative cases are included in the later 51; do not add overlapping counts or present them as a full-suite result. These are deterministic Unit/integration/SIT gates, not owner UAT or runtime performance measurements.

The previous independently reviewed campaign `b09a3fc9877b4b1ea6ca8fb435512052` remains **358 / 0 / 0**, pinned to `3b63ff49a12496b97d7ed2e6e5ecba6bac11350e`. It was not repeated without cause. Its 46 former failures and two provenance cases retain their previously published evidence.

[Machine-readable qualification](h1-f4-hardening-qualification-20261028.json) includes every NUnit name, source, command, stage result, process identity, Job Object, input-map/archive/tool hash and independent verification receipt. Raw logs remain local because Unity logs may contain authentication arguments.

### Preservation and process completion

Independent checks completed after each successful campaign, before the next preparation/source correction:

- All **1,494** original Unity inputs, including all three temporary ProjectSettings substitutions, restored exactly; font unchanged from original at final verification.
- All **nine** pinned executables/helpers reverified against that campaign's plan.
- Actual recorded host/runner exit codes zero and owned Job completion evidence sealed. Later process-identity queries confirm no recorded process remains; each named Job Object query returns absent/error 2.
- Reservation read independently as `campaign=null`, `previous=<that exact campaign>`. The latest owner is released from `3dfdc7f0ceb44b458c6aa97d7e7bf260`.
- Protected owner proposal/checkout head, other saves, historical fixtures/evidence, stopped campaigns and qualified build checks match the preparation maps.
- Original full-suite XML SHA-256 remains `aac1bab11bf2ba8b942c5c9828250f02cec636f29456b70921c87a2abc0a1f53`.

Restored hashes:

| File | SHA-256 |
| --- | --- |
| `ProjectSettings/ProjectSettings.asset` | `34da6d701e4c4629ca7b1cecb638f801d9c5ea4f40d33b09feca46777073b993` |
| `ProjectSettings/EditorBuildSettings.asset` | `53b28254e635e69536453567ccf17ced2aa18b42f73c896ed7251fbdbfd84fd9` |
| `ProjectSettings/ShaderGraphSettings.asset` | `6d98840f808336cd3b599d4ae82ef1582cc4bcb74b1f27bb4ad64c6c1d8a2f72` |
| `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset` | `6cd2faa131f346f601036104691f0223744c5806a51ceec454e6d4ff3bb6831d` |

Raw independent receipts are under each `TestResults/h1-f4-20261027-hardening-{repro01,instrument01,b3b1-01}` preparation directory, named `independent-final-verification01.json`. The review artifact inventory is `TestResults/h1-f4-20261028-hardening-report01/artifact-inventory.json`; it records exact owned paths, purpose and retention. Preserve these campaigns, archives, receipts, isolated saves and exported inputs through review. No cleanup, restoration exception, process termination, force release or uncertain recovery occurred. An initial sandbox CIM denial happened before the first preparation; the authorized process inspection then succeeded.

Historical FAILED and INCOMPLETE/UNCERTAIN verdicts remain unchanged. Later process absence is never substituted for missing historical descendant completion.

## Historical failure reconciliation and remaining correctness work

[Exact-name reconciliation](h1-f4-hardening-reconciliation-20261028.json) contains all **805** historical failed names, their original first messages and subsequent exact-name passing evidence. It compares literal NUnit full names, without rename aliases. **249** historical names have later passing evidence; **556** lack such evidence. These are coverage counts only. No current full-suite failure total is inferred. The changed historical/current scope of the mixed-profile test is explicitly marked.

The last complete raw XML remains **1,966 passed / 805 failed / 1 skipped**, source `16955152fcdd5a5f8af006dc38887a75f1f83266`, campaign `61c2a2cd452e4a5c837496834f680e55`, permanently INCOMPLETE/UNCERTAIN. The current passing cohorts establish no new full-suite verdict.

Remaining source-backed investigation:

1. Shared PhaseFiveB/C1/C2 factories still replace/mutate consumed configuration or use unsafe JsonUtility save clones. Fix the producer/factory before editing many individual tests; retain deliberately synthetic execution-only configuration edits where no canonical publication is attempted.
2. B2/B3A historical synthetic profiles need an explicit historical-reader/current-writer distinction. Do not silently label them supported production profiles, fabricate producer evidence or weaken schema-17 binding. Preserve gameplay/transaction assertions when correcting obsolete current expectations.
3. The unresolved scene null reference is **`controller.Draft.ReadModel` at `ProductionDungeonRoomContentSceneTests.cs:304`**, after `ConstructReassignmentDestination` yielded. It is not established as a ContentPreview null or dismissed by isolated passes. Add scene-scoped state diagnostics and reproduce the first lost-draft transition before correcting it.
4. `ProductionDungeonForegroundSceneTests.InstallForegroundBoundaryTestClock` replaces GameRoot's TimeService after SaveService has bound its observation owner. Several scene tests also replace policies after consumption. A precise, controlled clock and any synthetic policy must be established before binding; ordinary lifecycle behavior still needs actual scene qualification.
5. Remaining historical/current schema assertions, time eligibility/stalls, Research acknowledgement, authority/protocol cuts, repeated crashes and save/reopen remain gates. Three other historical null-reference names have later exact-name passing evidence; the fourth scene case does not.

Full EditMode is **not ready to launch** against these known shared problems. Refresh the actual NUnit inventory after corrections; the existing full-plan count is not a fresh inventory after the added guard test. Then run full EditMode, applicable PlayMode, interruption/repeated-crash and isolated Windows Player qualification with actual startup/gameplay/reopen/exit evidence. The intermittent native shutdown problem remains open.

## Performance baseline and benchmark readiness

All **eight** historical summary hashes referenced by `performance-correction/comparison.json` were independently rechecked. PRs #247, #251 and #252 and their measurement contracts were reviewed. These retained Windows workloads completed 68 runs at 1280×720/Default; timings below are milliseconds, mean / p95 / maximum:

| Historical Windows measure | Pre-optimization `3040ddc4` | Corrected historical `c3a21ba5` |
| --- | --- | --- |
| One-floor event | 307.957 / 445.714 / 481.043 | 267.554 / 372.134 / 404.420 |
| Two-floor event | 538.089 / 732.171 / 1168.427 | 468.954 / 618.602 / 1043.496 |
| Two-floor admission | 2138.105 / 2241.040 / 2282.163 | 1919.193 / 2011.483 / 2029.486 |
| Two-floor release-tagged work | 1761.764 / 1870.769 / 1893.522 | 1422.660 / 1537.420 / 1569.740 |
| Maximum two-floor complete frame | 2282.511 | 2030.011 |

These are evidence of that historical bounded optimization and inherited responsiveness costs. The approximately 268/469/1919/1423/2030 figures are **corrected historical results, not measurements of `3eb6ccf` or H1-F4**. No H1-F4 improvement, regression or mobile readiness is claimed.

Benchmark readiness findings:

- `GameRoot.ForegroundProfile.ProfileForeground` constructs a replacement TimeService after recovery binding, like the scene fixture. Qualify controlled-clock setup through the existing owner before relying on the schema-17 profiling path. The normal boot path does not perform this replacement.
- The admission microbenchmark uses one admission plus post-publication inspection and an in-memory filesystem, with one warmup per mode and five alternating enabled/disabled samples. Its Editor timings and reconstruction counts do not measure Player frame/disk latency. Passing its related correctness cases does not execute the benchmark.
- Preserve frozen #245/#247 adapters and original seeds. Prepare fresh reviewed supervisor commands with unused isolated checkout/build/product/save identities for `3eb6ccf93558b8145d64e9e63cbefcbd73484602` and the final qualified H1-F4 commit. Never run historical code against modern saves.
- Match workload/content, hardware, Editor/CLI, build/presentation settings and scope boundaries for the two fresh sources. Retained historical runs used an earlier CLI; disclose unavoidable mismatches and treat those comparisons as directional. Keep Editor and Windows results separate.
- Require warmups/repeats, event/admission/settlement/transfer/block/release and complete-frame distributions, main-thread costs, candidate/validation/reconstruction/reader/prefix/publication counts, atomic persistence/readback, allocation/GC/available memory and H1-F4 drain/original/residual/reopen overhead. Unsupported counters remain unavailable, never zero. Compare deterministic gameplay/replay outputs and explicitly identify schema-17 differences.
- Inclusive preparation/replay and persistence/readback scopes overlap and cannot be summed. Call-count reductions are not player-visible latency proof. NUnit duration is not performance evidence.

The four completion questions remain bounded by the available evidence: the historical correction measured a reduction; final pre-H1-F4 main versus H1-F4 speed/memory is **not yet measured**; repeated validation/reconstruction/prefix replay, admission assurance and durable readback are the largest measured historical costs; H2 retained execution/batching is intended to address repeated execution/reconstruction work, with no promised gain or automatic scope expansion. Material H1-F4 regressions remain review blockers until measured and resolved or explicitly assessed.

## Next review boundary

Review this fixture/historical compatibility milestone and its exact source-pinned results. The smallest next work is shared factory correction plus a narrow reproduction of the scene draft-loss/clock ownership paths, followed by their affected cohorts. Full EditMode, PlayMode, Windows and final matched performance work follow in dependency order. No owner UAT, broad Windows campaign, final benchmark or merge is authorized by this milestone's passing counts.

**READY FOR INDEPENDENT PR REVIEW — focused correction milestone only.**
