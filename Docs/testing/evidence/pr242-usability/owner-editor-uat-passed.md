# PR #242 — owner Editor UAT passed

Recorded 2026-10-18 from the owner's explicit report in the PR #242 task. This is owner usability evidence, separate from Codex's automated results. It supersedes the pending **Editor** UAT status in the earlier qualification/handoff records; those historical records remain unchanged.

## Owner-confirmed result

The owner passed `owner-usability-editor-01` against Unity inputs at `9de6ea135ea8bb4d6c68232fa58dcfdddcc64b5f`, identical to the automated qualification's tested Unity source `0997d553ada69d54c4f53891932edd8df9b4cee7`:

- Landscape room construction is usable.
- Right-side editing controls in landscape are approved.
- Bottom editing controls in portrait are the preferred design.
- Spike Trap placement passed.
- Saving, restarting Unity Play mode and confirming persistence passed.

These are the owner's reported results. This record does not expand them into physical-touch, native-mobile, every-resolution or Windows-player approval. No automated suites or owner gameplay interactions were performed by Codex for this closeout.

## Approved responsive-layout decision and Phase 7

[Phase 7A0 Decision 5](../../../planning/phase-7a0-graphical-dungeon-editor-design-lock.md) originally specifies a contextual bottom panel/sheet. The owner now explicitly approves a scoped responsive refinement for the corrected PR #242 interface: **active landscape construction/content editing uses the right-side working sheet; portrait retains the bottom editing surface and remains the preferred design.** This approval is recorded here rather than rewriting the historical design lock or implying that it previously specified a landscape side panel.

This retains Decision 79's portrait-first approach and functional landscape support, and Decision 96's single responsive information architecture. It uses the same contextual elements and actions, not a separate gameplay authority or navigation architecture. Safe-area and text-size obligations in Decisions 80–81 remain applicable. Normal Dungeon mode, ordinary inspection, floor management, room movement and replay retain their qualified layout behavior. The approval is not authorization for a broader UI redesign.

## Closed Editor attempt and preservation

Codex verified no Unity Editor or Dungeon Lord player was running before restoration. The remaining `Unity Hub/resources/unity.exe serve` process is the unrelated CLI server. The Editor log records normal shutdown.

The established command completed with exit code 0:

```powershell
python -B Tools/Presentation/prepare_pr242_usability_owner.py finish owner-usability-editor-01
```

The result records **1,340 exact original inputs restored**, **14,401 protected files unchanged**, and no additional generated local asset changes. All five pre-existing local Unity asset modifications were restored exactly and remain uncommitted. The isolated owner save, Editor log, input backups/restoration record and generated session files are retained in the closed attempt's **15-file artifact index**. The owner save is preserved in its isolated product folder; it is not copied into the Windows product or normal save.

Private restoration/index records are under `TestResults/pr242-usability-owner-20261018/owner-usability-editor-01/`. Normal saves, existing builds, previous reports/captures and earlier owner attempts remain protected. The prior campaign's 6,839 original save/player files and 1,986 available historical evidence files are included in preservation. Exactly five historical screenshot originals remain permanently missing under the already accepted exception (1,986/1,991); their original hashes and incident record remain unchanged. No restoration claim is made for those five originals.

## Separate Windows handoff

Windows usability approval remains pending. Codex must verify preservation, the indexed qualified build and the already-prepared separate starter save before launch, using a new unused launch attempt `owner-usability-windows-01`. No build or automated suite is rerun.

- Qualified Development player: `Builds/Development/pr242-usability-20261018-usability-build-01/Dungeon Lord.exe`.
- Product: `Dungeon Lord PR242 Graphics usability-build-01`.
- Separate save: `pr242-usability-20261018-usability-build-01.json` in that isolated product's LocalLow folder.
- Starter SHA-256: `f9004a81971e5c7e60009048eb2dd1c868c1cb639d8bf4a0d505f312a992b743` (existing authority-generated starter room/monster and 1,000 mana; unchanged exact copy).
- Fresh log: `TestResults/pr242-usability-owner-20261018/owner-usability-windows-01/player.log`.
- Initial window: 1280×720; owner chooses Display → Large and repeats the short [manual procedure](../pr242-graphics/manual-uat.md).

The separate Windows launch preflight subsequently passed with exit code 0. It reverified all 14,401 protected files, all 1,340 restored inputs, the closed Editor artifact index, the complete qualified build/starter artifact indexes, the packed product/save identity, the exact separate seed and unused launch destinations. Its private record is `TestResults/pr242-usability-owner-20261018/owner-usability-windows-01/preflight.json`. No existing save was rewritten or copied during this check.

The player must not be launched if a preservation or destination check fails. Codex stops after launch readiness; the owner performs the usability check and reports when a save/reopen cycle is needed. PR #242 remains **Draft**, with independent review and Windows owner UAT pending. B3A gameplay and all qualified Unity inputs are unchanged; no merge is authorized.
