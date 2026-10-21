# Two-floor foreground technical qualification

Status: in progress. This document is not a completed qualification claim or an invitation to owner gameplay testing.

Baseline: `76f78c5c2d17d3aab916f297aecb5c25cf146d76` (`origin/main`, fetched again during continuation). Branch: `codex/living-dungeon-two-floor-foreground`. Approved implementation configuration: GPT-6.1 Sol High, Complex; no model substitution. The [implementation contract](../planning/living-dungeon-two-floor-foreground-contract.md) records the approved semantics.

## Source and persistence

The implementation registers a separate `living.foreground.c2.v2` resource. It owns transfer duration 1,000 logical units at 1,000 units/second. Existing v1 configuration and executable/checkpoint formats 1/2 remain historical authorities. Format 3 wraps the captured C2 start and authenticates event-prefix progression under distinct domains. Stable event numbers 0–12 remain explicit; Blocked and Released append 13/14.

Waiting and Released retain the captured source-floor identity. Only completed transfer changes occupancy. The private writer derives a vacancy from pinned terminal execution, verified settlement, and retirement in its current transaction. A predecessor/checkpoint-specific capability permits the released candidate; arbitrary complete-save preparation cannot manufacture a new release. The capability is never serialized. Release and completed transfer publish within one atomic timestamp; a standalone Released prefix remains reconstructible for checkpoint qualification but cannot be a partially published canonical boundary.

Writable schema remains 15, using the existing party checkpoint chunks. No new writable owner, field, sidecar journal, schema migration, monster-condition authority, or clock is introduced. Historical v1 drains under its original timing and inputs, suppresses replacement admissions, retains installed future opportunities, and upgrades atomically when empty and closed regardless of current layout eligibility.

## Campaign and tools

Raw evidence: `TestResults/living-dungeon-two-floor-20261020/`. Adapter: `Tools/Presentation/qualify_two_floor.py`; native launcher: `qualify_two_floor_native.py`. These extend the existing preservation adapters with fresh products, filenames, reports, and build output. Qualification substitutions are temporary and must be restored before final closeout.

The owner approved CLI beta.12 → beta.13. Installed `unity --version` reports `1.0.0-beta.13`; `unity editors --installed --format json` reports the unchanged Editor `6000.3.2f1`, revision `a9779f353c9b`. ProjectVersion and package files have no diff. The current PlayerSettings change is only the pre-existing isolated qualification product name, pending final restoration. Evidence: `cli-beta13-verification.json`. No other tool upgrade was performed.

Preservation preparation recorded 11,688 protected save/build files and 8,604 historical evidence files. These are checked before each launch. Final byte-preservation/restoration audit remains pending.

## Completed attempts and corrections

Commands below use the bundled Python at `C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe` to invoke `Tools/Presentation/qualify_two_floor.py`. Each command JSON contains its exact Unity invocation, source commit, start/end, and exit code. Overlapping filters are not a unique test total.

| Attempt | Source | Actual result | Correction/disposition |
| --- | --- | --- | --- |
| 01 focused | `0eb35607` | Compile failed; no test verdict | Renamed conflicting detached-session local. Earlier sandbox process-inventory failure retained separately. |
| 02 focused | `598374b5` | 16/17 passed, one failed, zero skipped | Historical fixture required an explicit floor target. |
| 03 boundary-edit | `cfc1f358` | 190/191 passed, one failed, zero skipped | Historical fixture needed the configured floor-lifecycle writer; the feature safeguard/overlap cases passed. |
| 04 focused | `ab1f613a` | Compile failed; no test verdict | Added missing placement namespace to development driver. |
| 05 focused | `1b492471` plus uncommitted Editor fixture | 20/22 passed, two failed | Diagnostic only: fixture added after applicability capture. Both failures used raw JSON instead of the production Bootstrap parser that attaches combat rules. Stop request arrived after exit; actual disposition retained separately. |
| 06 focused | `04516affeb491c4e3b18068378603dc01e290129` | **21/21 passed, zero failures/skips** | Corrected production parser and historical fixture. |
| 07 full-edit | `6f9fc4a7` | Compile failed; no test verdict; PlayMode/build not started | Golden-fixture test referenced an instance-only migration-limits property. Corrected to load the authored production limits. |
| 08 full-edit | `300aa5fea22f14acf5c47d9d8cfad4dfba10ed89` | **2,313 passed, one failed, one expected platform skip; 2,315 total** | Only new matched-profile fixture failed: its captured manifest was lost across Enter Play Mode/domain reload. Move fixture creation after the reload. Skip: `CurrentNonWindowsRuntimeFailsClosed` (`gd66.test.windows_only_inverse`) on Windows. PlayMode/build did not start. |
| 09 boundary-edit | `bb667318e54bd4150d9d64e5fb23adb491f87f7f` | **194/194 passed, zero failures/skips** | Includes one-unit-before-vacancy and immediate-after-release reopen. |
| 09 profile-edit | `bb667318` | Failed: combined test exceeded its 1,800,000 ms timeout during two-floor measurement | Single-floor 68-run report completed; two-floor report incomplete and not passing. Split scenarios into separate cases with a larger measurement-only wall timeout. Same 68-run target, 40,000-frame cap, gameplay timing, and validation/work envelopes retained. PlayMode/build did not start. |
| 10 original-format1 capture | `1d78fe78` | Compile failed; attestation did not execute | New guard regression used a guessed helper name; corrected to the existing `AdvanceToNextEvent` API. |
| 11 original-format1 capture | `a8617a00` | Independent original assembly loaded; reader returned `run.checkpoint.invalid_inputs` | No format-1 golden or compatibility pass claimed. Diagnose the original reader's underlying exception before drawing a compatibility conclusion. |
| 12 diagnostic | `b87adbf2` | Stopped during preflight; Unity did not launch | Replaced diagnostic byte-offset parsing with a direct captured-start lookup. |
| 13 original-format1 diagnostic | `dfde5ce0` | Failed attestation; original DLL deserialized a null private room array | IL inspection located the original-reader exception in its room-array path. Side-by-side DLL execution is not used as a compatibility pass. The final golden instead pins the unchanged format-1 source baseline and is qualified with existing frozen structured execution regressions. Original DLL and failed evidence remain unchanged. |
| 14 frozen-format1 capture | `637c03ac4ad3fb7744a8bc9b2166ce176ad8b50a` | Passed; 34 prefixes round-tripped | Exact `main` source hashes match unchanged `RunCheckpoint.cs` and `RunStartCapture.cs`. Pinned checkpoint ordinal 7, identity `2a2c63400b766d158f8ca16b23c29fdfd0fa84455d1cc437cff7b1381f0bddc1`; start `34f4e506f49eca94b3febe468fde044a28c72feffc937ccce3da49a3e7b3e614`. This is a frozen-source golden, not a claimed successful old-DLL execution. |

Focused qualification includes normal/blocked/released transfer replay, source occupancy, real retirement/release, rejection of self-consistent forged release and empty/stale projections, failed write/replace/flush with unchanged predecessor and exactly-once retry, recovery/admission exclusion during waiting, current deactivation preserving captured route, partition/reopen equivalence, overlapping snapshots with partial recovery, corrupt format-3 rejection, and v1 draining/automatic upgrade.

The production-input exporter uses unmodified production combat/decision/timing configuration and legal construction/content transactions. Its verified sample produced 29 genuine DESCEND choices in 32 runs; maximum captured Floor 2 execution duration was 44,000 logical units. This is fixture coverage, not evidence that native automatic waiting has already passed.

## Required gates still pending

Full EditMode/PlayMode, matched Editor measurements, Windows Development build, real multi-launch waiting/release/finish and old-v1 upgrade, native matched measurements, final preservation, final diff review, and PR publication remain pending. No owner gameplay qualification should begin until technical gates and independent review have resolved blocking findings.

The native runner installs a fresh seed only before player startup, records the exact build/source identity, closes normally between continuation phases, and reads/copies canonical files only after exit. Development-only injected-clock acceleration preserves production logical timing. Native batchmode exercises the real Bootstrap/controller/writer and does not count as a human visual assessment.

## Performance reporting rules

Report normal event, admission, settlement, recovery, blocked/released, passive tick, idle, and reopen costs separately. Tag-based measurements can overlap; instrumentation scopes are inclusive. Unavailable allocation counters must be reported as unavailable, never zero. Include mean, p95, maximum frame/update stalls, checkpoint/save sizes, restoration and atomic-write counts. Compare equivalent Floor 1 content with Floor 2 inactive versus active on the same machine/build, and identify differences from PR #245's original simpler scenario. Preserve full validation, readback, replay, and atomic writes. Windows results do not establish mobile performance.

PR #245 final reference: Editor event-update mean/p95 216.261/296.458 ms; Windows 235.995/325.777 ms, 68 runs, 755 writes. Its complete raw evidence and qualification remain historical, protected inputs. New measurements are not yet available.

The existing single-floor Editor profile completed 68 runs during attempt 08: event-update mean/p95/max 231.272/259.753/417.243 ms; maximum observed frame 707.767 ms. Recorded 890 atomic-write scopes, 8,160 canonical-party restoration scopes, 1,579 stages, and maximum published checkpoint 23,621 bytes. Frame allocations are available (mean event 96,058,086 bytes); thread allocation counter is unavailable. Raw `TestResults/foreground-profile-805be0219d4d42fa8c3ba025698c4e5b.json`; summary `editor-pr245-scenario-summary.json`. This profile ran within the full suite, rather than the earlier dedicated PR #245 campaign. These are material synchronous stalls, not a performance acceptance claim. The old restoration scope excludes private lifetime-probe replay; a separate appended CheckpointReplay counter will measure all supported-reader invocations in subsequent matched profiles. No checkpoint bytes, reader evidence, or atomic behavior is changed by that opt-in scope.

The matched Floor-2-inactive scenario completed 68 runs during attempt 09: event-update mean/p95/max 279.653/404.439/583.655 ms; maximum frame 1,602.192 ms; 1,091 writes; 10,608 canonical-party restorations and 11,832 supported checkpoint-reader invocations (including private probes); maximum published checkpoint 24,242 bytes. Mean event frame allocation 117,597,417 bytes, with unavailable thread counter reported as null. Source/raw/summary are retained in the campaign. Its completed report does not turn the parent timed-out test or unfinished two-floor scenario into a pass.

A separate code audit found that the public synchronous `DrainToCompletion` helper would make no progress on a waiting party. Foreground scheduling/reconstruction do not use this path while waiting. The correction rejects that call with a stable localized reason and preserves checkpoint bytes, ordinal, work, start identity and occupancy; authoritative release can still resume it. Historical formats never enter this versioned waiting state. This correction requires its focused regression and a newly built player before final qualification.
