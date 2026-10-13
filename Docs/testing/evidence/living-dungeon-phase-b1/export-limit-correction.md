# Phase B1 checkpoint export limit correction

Correction qualification for PR #234, starting from `5faeb32e045521cad85b3dbae1dbc8b4b8e334ce` on `codex/living-dungeon-phase-b1`, based on `eeaf9d1516c2ffa5d6600238a3e46fcb3787ecc8`. The active configuration was GPT-6 Luna, Medium (Routine); no fallback was used.

`RunCheckpointCodec.Export` now rejects `NextOrdinal > MaximumReplayEvents` with `run.checkpoint.replay_limit` before encoding or returning a checkpoint. This uses the same caller-supplied limit and reason as restore. The check does not advance or poison the active execution. Byte limits, format 1, replay logic, and executable-rule identity are unchanged.

Added EditMode coverage proves export and restoration succeed at the exact limit; one event beyond rejects without changing inspection or preventing a later adequate export; a zero-event checkpoint exports and restores with a zero-event replay limit; and terminal checkpoints use the same exact/over-limit behavior. Existing byte-limit rejection and round-trip tests remain enabled.

## Qualification results

Unity 6000.3.2f1, existing disposable validation checkout, isolated output and save namespace:

| Run | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Focused EditMode, B1 and Phase A | 79 | 0 | 0 |
| Focused PlayMode, B1 and Phase A | 3 | 0 | 0 |
| Full EditMode | 1,783 | 0 | 1 |
| Full PlayMode | 3,208 | 0 | 10 |
| Production spatial build gates (within full EditMode) | 65 | 0 | 0 |
| Strict-input qualification | 12 | 0 | 0 |

The Phase A frozen structured equivalence test passed with all 38 scenarios. Its original fixture was not regenerated; `phase-a-baseline.txt` remains SHA-256 `53045ad58ab1dbebf8ea07f83480db02604eb79ae3b243f1169df2ae2e950198`. The existing byte-limit and valid checkpoint round-trip cases passed in both focused and full EditMode runs.

The Windows Standalone Development build succeeded for Bootstrap with zero errors and one Unity Cloud Diagnostics symbol-upload warning (`Invalid URI: The URI is empty`; upload credentials unavailable). It packed all 52 required art assets. The new output is `Temp/ui-composition-floorhud-validation/Builds/Development/Windows-living-dungeon-phase-b1-export-limit/Dungeon Lord.exe`; the build report records 181,441,696 bytes. The prior B1 player was preserved at its original path and verified byte-for-byte (303 files).

The post-build strict input comparison passed for all 1,238 inputs. The expanded preservation audit matched all 2,765 entries. The 22 preexisting disposable PlayMode fixture files were restored byte-for-byte from their retained backup; the primary owner save was neither reset nor restored. Existing caches and previous builds were retained.

No save service, canonical save format, schema, migration, gameplay rule, timing rule, replay algorithm or executable-rule identity changed. The review-thread listing was empty when checked on 2026-10-13. Independent review remains required; this correction does not imply merge approval.
