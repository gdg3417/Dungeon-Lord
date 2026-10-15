# Owner Combat C0 acceptance — updated 2026-10-15

Status: owner-authorized scope correction on existing draft PR #238. Keep draft and unmerged. External review must determine remaining final qualification before more full Unity suites, a Windows rebuild, a project refresh or further owner UAT. Earlier integration/readability reports remain historical; they do not qualify the later overlay reversion.

## Approved acceptance boundary

Raw combat play-by-play is **developer evidence**, not a player-facing C0 feature. Mechanical combat correctness may be established by complete chronological authoritative event records and existing automated tests. Player-visible attacks, HP changes and graphical playback belong to **C1**. No combat-log navigation UI, animation, production export service or persistent combat history is required in C0.

The owner reported successful two-floor loading and reciprocal manual runs, including run-68 and run-71. The developer reproduction retained 18 events in both latest and selected history records, but its keyboard navigation did not change the summary. That input cause was not established. The investigation is stopped by owner direction; raw diagnostic navigation is not an acceptance blocker. The records and precise reproduction limitations are [preserved](../authoritative-20261015/qualification.md).

## Mechanical acceptance through automated evidence

- Review [production-path-run-1.txt](../authoritative-20261015/production-path-run-1.txt): all ordered events, full canonical identities, loaded configuration/initiative inputs, action order, legality reasons, numeric room-local positions, actual HP/damage/defeat, completion, final party/monster HP and aggregate outcome. This is an automated disposable run, not reconstructed owner run-71 evidence.
- Review focused production-path assertions and the previously qualified kernel/integration cases for reciprocal/group combat, initiative/target ties, illegal engagement/bypass, ranged/LOS, retained HP across encounters/floors, wipe/limits and complete atomic rollback. The exported single encounter is an illustrative production-path trace; it does not independently cover every mechanic.
- Review frozen B1 checkpoint/outcome/evidence identity, B2 compatibility, parser/work limits, trap/loot ordering, singular Heat/mana/loot settlement, run allocator/history and save/reopen tests. No fixture regeneration is authorized.
- Review chronological records directly through existing `CombatC0Diagnostics.Events(outcome)`, `.Monsters(outcome)` and `.ChargedWork(outcome)`. Export is test-only; no new runtime authority or save field exists. Detailed records remain transient and must not be fabricated after reopening.
- Earlier full-suite/build reports remain available. The owner specifically defers decisions about final full suites, gates and a Windows rebuild to external review of this correction. Do not describe focused results as full qualification or gameplay acceptance.

## Existing owner environment and preservation

Repository branch: `codex/combat-c0-deterministic-combat`. Reviewed starting head for this correction: `404251f71150b1de317cd1707b9dc2fdda9e508f`; record the eventual accepted source commit separately. Unity 6000.3.2f1.

The owner's two-floor project remains `C:/Dev/Dungeon-Lord/Temp/ui-composition-combat-c0-diagnostics-validation-warm`, Bootstrap scene `Assets/_Project/Scenes/Bootstrap.unity`, configured for `combat-c0-diagnostics-validation-20261015.json` in `C:/Users/gdg34/AppData/LocalLow/gdg3417/Dungeon Lord/`. Preserve this project's current dungeon/save, run history and recovery evidence. No new manual save transfer or pristine reset is required.

That existing project and its Windows player still contain the earlier qualified inputs until a separately reviewed refresh/rebuild. This correction does not overwrite either. Its automated tests use `ui-composition-combat-c0-access-validation` and the established unique `phase7a4-scene-<guid>.json` fixture; the project's fallback namespace is `combat-c0-access-validation-20261015.json`.

The primary `save_primary.json`, integration namespace `combat-c0-integration-validation-20261014.json`, run-68/run-69/run-71 evidence, prior migration backups and all previous builds remain protected. Never start an owner scene against the primary filename.

## Subsequent owner Editor checks, after external review

1. Confirm approved branch/commit, refreshed project inputs, scene and exact disposable filename. Back up current owner disposable save and its precisely owned recovery files while Editor/player are closed. Preserve the existing two-floor state rather than resetting it.
2. Open Bootstrap and verify both existing floors, placements, research, progression, resources and run history load. Record screenshots and startup warnings. Normal boot-save timestamps and configured foreground/offline passive mana changes are expected; identify them separately when comparing saves.
3. Use the existing manual Run control. Check the player-visible completed summary, one history/sequence allocation, continuation/depth result, wallet, Heat and existing loot/extraction behavior. Compare mechanical questions with reviewed authoritative automated evidence; raw diagnostic pages are optional developer aids.
4. Exercise normal dungeon interaction and floor selection. An absent C0 attack animation or unavailable graphical replay is expected. New C0 replay explains the C1 deferral; historical saved outcomes without transient evidence explain that absence. Do not enable C0 replay.
5. Save, close and reopen. Verify canonical layout, progression and aggregate history persist; detailed combat HP/events are transient and need not survive reopening. Never reconstruct them from current configuration.
6. Record observable failures, warnings, balance feedback and unexercised cases. Do not change tuning to force a wipe, bypass, injected ranged defender, solid interior wall or technical limit; those mechanics are automated qualification cases.

## Standalone, lifecycle and experience

Standalone parity remains separate and pending the external review's build decision. After a corrected build is qualified, run Editor/player sequentially against the approved disposable namespace and check the normal manual-run/summary/save/reopen flow. Preserve player logs, exact artifact hashes and save snapshots. Raw combat-log navigation is not a standalone C0 acceptance requirement.

Lifecycle acceptance requires canonical persistence, atomic failure behavior, unchanged owner saves and no invented transient combat state. Gameplay-experience feedback can address initial results and balance, but visible attacks/HP/action playback await C1. C2 persistent injuries/recovery/reserved mana and automatic Living Dungeon activation remain excluded.

Pass/fail is based on reviewed authoritative mechanical evidence, required automated qualification selected by external review, and the applicable player-observable/manual lifecycle checks. An illegal attack, inconsistent HP ledger, repeated defender, partial publication, new death reward or save/checkpoint regression remains a blocker. Accepted developer-diagnostics clipping/overflow or keyboard limitations alone do not require additional C0 UI work. No owner gameplay acceptance or merge readiness is claimed by this document.
