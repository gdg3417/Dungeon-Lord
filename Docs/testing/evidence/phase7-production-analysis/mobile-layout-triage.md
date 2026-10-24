# PR #249: owner layout and responsiveness triage

2026-10-24. **Hold merge. Independent review must disposition the viewport contract, UI correction scope and remaining owner acceptance. No production fix is authorized by this investigation.**

## Evidence identity and owner clarification

Clean starting branch `feature/phase7-production-analysis`, local/remote reviewed HEAD `fb5ab9ddc583246b8342294f7e81e812114806f1`. GitHub main and local origin/main both resolve to `fbe4bdff0cba9fea72b42fb6a05714327f880df4`; PR #249 is open, draft and unmerged. Qualified source remains `c870c5dc92aee87e854af350aa0a73653d045ff2`. All 303 files of sealed artifact `P249R2-fe8daae0264348579db73d3402254215` match its manifest before and after observations. No runtime, UI, settings, test, fixture or launcher source was changed.

The owner explicitly clarified: **testing was directly on the PC, not on a phone or through Moonlight.** Accordingly the reported 1–3 second delays and narrow-window defects are desktop observations. Physical phone acceptance and stream latency have not been measured. The owner reports desktop Analysis advances and the launcher closes/archives without Task Manager; five existing owner-session archives are preserved. This is partial owner desktop feedback, not blanket acceptance of all scenarios. Earlier F1 statements about zero owner sessions describe that earlier preparation, not today's state.

## Approved contract versus selected test sizes

Phase 7A0 Decisions 79–81 require portrait-first UI, usable/non-broken landscape, safe-area protection and readable Small/Default/Large presentation. Decisions 85–87 require iOS 44-point / Android 48-dp hit regions, four persistent primary HUD metrics and a compact Analysis entry. Decisions 95–96 require the approved destination hierarchy and shared adaptive layout. Decisions 100–103 preserve transaction-safe navigation, secondary/advanced presentation and durable-versus-transient reporting boundaries. None of these cited decisions establishes a numeric minimum supported screen resolution, logical point size or panel size.

**360×640 and 640×360 were explicitly selected qualification/owner-instruction targets in this PR's earlier work.** They remain accountability targets for those checks; do not retroactively discard failures as irrelevant stress cases. However, they are small Windows client-pixel stress/proxy sizes, not an approved definition of a modern iPhone rendering surface or the minimum supported device. Tests established specific hit/reachability/draft/replay properties, not holistic HUD readability. F1 screenshot `26-partial-portrait-compact-entry.png` already visibly contains split HUD labels; earlier functional check passes cannot establish that those labels were acceptable. A reviewed viewport matrix is needed before declaring mobile acceptance complete.

## Units and actual scaling

| Quantity | Meaning in this investigation |
| --- | --- |
| Windows client pixels | Interior game render dimensions, measured using GetClientRect rather than outer-window capture or requested flags. |
| iPhone physical pixels | Native display raster. Apple lists iPhone 16 as 1179×2556, a representative modern device, not a newly imposed supported-device minimum. |
| iPhone logical points | UIKit coordinate units converted by screen scale. At an illustrative 3× scale, 1179×2556 corresponds to 393×852 points. Points are not interchangeable with this Windows build's panel units. |
| UI Toolkit panel units | Production Panel.asset uses Constant Pixel Size; controller explicitly sets scale to `max(1, min(width,height)/720)`. Panel dimensions are render dimensions divided by that scale. |
| Moonlight stream dimensions | Encoded remote picture dimensions, distinct from the game client, captured host desktop and phone display. Streaming a Windows game does not turn it into an iOS player or substitute UIKit safe-area/point rules. No stream was involved in the owner's reported defects. |

The serialized 1200×800 reference resolution is not the active scale-with-screen-size policy: Panel.asset has scale mode 0 and runtime Layout sets scale explicitly. ReferenceShortSide 720 is a scaling reference, **not a minimum-support declaration**. Desktop physical-unit conversion returns 1, while native iOS obtains UIScreen.nativeScale through the existing plugin. SetTextSize enforces Desktop 48-pixel versus iOS 44-point targets independently of font sizes.

Calculated examples before safe-area insets:

| Render/client dimensions | Runtime scale | Panel dimensions |
| --- | ---: | --- |
| 360×640 | 1 | 360×640 |
| 640×360 | 1 | 640×360 |
| 1179×2556 | 1.6375 | approximately 720×1560.92 |
| 2556×1179 | 1.6375 | approximately 1560.92×720 |
| 1561×720 Windows proxy | 1 | 1561×720 |

Thus a 360-pixel Windows window provides about half the short-side panel space of the representative native raster under this policy. A full iOS player still differs: at 3×, a 44-point target requires 132 physical pixels, about 80.61 panel units at scale 1.6375; the Windows proxy enforces 48. Neither viewport arithmetic nor a Windows screenshot proves native iOS control fit, safe areas or physical readability.

Primary references: [Apple iPhone 16 display specifications](https://support.apple.com/en-us/121029), [Apple screen scale](https://developer.apple.com/documentation/uikit/uiscreen/scale), [Apple touch-target guidance](https://developer.apple.com/design/tips/), [Unity Panel Settings](https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-Runtime-Panel-Settings.html), [Moonlight setup/input guide](https://github.com/moonlight-stream/moonlight-docs/wiki/Setup-Guide). The 3× point example is arithmetic, not a measurement of the owner's phone or a new device requirement.

## Main comparison and Analysis contribution

Source comparison verifies byte-identical presentation policy, physical-unit conversion, Panel/Presentation assets and foreground UI between main and the qualified branch. All existing main USS rules are unchanged; #249 appends 12 Analysis rules. The top HUD UXML remains unchanged. No main player was rebuilt or run for a pixel-identical A/B comparison.

The baseline HUD places four 24%-basis cards in a row. Default cards include a non-shrinking 24-unit icon, margin, padding and a label that may shrink to zero minimum width. At 360 units, text gets only a small fraction of each roughly quarter-width card. Default text does not select the compact Large-only two-column override. In landscape, the baseline reserves 64% of top content for that same four-card row and 36% for navigation. These unchanged constraints explain the letter/syllable wrapping independently of Analysis. Baseline foreground messages and lower sheets also consume limited vertical room; only compact layouts get a separately height-bounded activity scroller.

#249 adds a button in the lower sheet header and hides the old sheet title in Normal mode. It does not add a top HUD card. Its compact/replay summary is already one line with ellipsis, following the earlier demonstrated/fixed header-overflow defect documented in [mobile verification](mobile-verification.md). Wider layouts still show a two-line summary. That can consume more header height than the old title/button row, particularly when height is constrained despite a wide panel. This is a real added space cost, not proof that every clipped lower control is a new regression. The exact net map-height delta against main is unmeasured because states differ and no matched main native build was run. Do not claim either zero layout regression or a proven new universal clipping regression.

## Two bounded native observations

The existing qualified host launcher functions were reused without modification, invoked by a separate local wrapper. Both used the unchanged Main dungeon fixture, ordinary simulation and existing exclusive save lease/two-handle exit/preservation guards. This was a layout check, not owner acceptance, profiling or a shutdown investigation.

| Observation | Requested | Actual client | Finding | Exit |
| --- | --- | --- | --- | --- |
| Portrait request, PID 1252 | 1179×2556 | **1179×1080** | Windows clamped height; actual result is landscape. Default Analysis readable; Dungeon labels wrap and lower controls remain crowded. **No representative portrait reproduction established.** | Normal, code 0 |
| Landscape panel proxy, PID 48400 | 1561×720 | **1561×720** | Default/Large HUD labels readable without letter-by-letter splitting; Edit/Replay/Display/Analysis controls visible, Analysis entry/return worked. Longer active-party text still compressed the map and floor rail. | Normal, code 0 |

The landscape result narrows the small-window finding; it does not qualify an actual iOS player. Seven new screenshots and sanitized process/size evidence are in [the observation packet](mobile-layout-triage-reports.zip). Source, capture and preservation hashes are recorded there. Both handles confirmed exit before save access/restoration. Original 47-file product inventory was restored after each observation; 654 prepared-package/historical-owner-session files are unchanged. All 1418 original Unity inputs and both 303-file build copies remain unchanged. A first package key-set audit falsely differed because Windows separators were compared with POSIX separators; its report is retained locally. Normalizing path keys resolved it without restoring or changing any file.

## Responsiveness: measured, observed and unresolved

The owner reports 1–3 second delays on the **local PC**, so Moonlight latency cannot explain those particular observations. During this check one Display response appeared on a later capture, but no input timestamp/frame trace was collected; this is not a measured latency value.

The merged [performance audit on main](https://github.com/gdg3417/Dungeon-Lord/blob/fbe4bdff0cba9fea72b42fb6a05714327f880df4/Docs/testing/foreground-performance-architecture-audit.md) reports historical corrected Windows two-floor admission-category update mean **1919.193 ms** and maximum complete frame **2030.011 ms**, with event-update mean **468.954 ms**. Those measurements predate #249. The synchronous admission/settlement path includes detached preparation, complete-save validation, checkpoint reconstruction/replay and durable persistence on the main thread. Blocking that thread can delay ordinary input delivery/rendering. This is a plausible explanation, not event-correlated proof of the owner's individual delayed clicks or a measured #249 regression.

The Analysis projection rebuilds on retained-array reference change rather than scanning retained history on every frame; its formatting/presentation still runs on shared presentation paths. There is no new measurement proving its exclusive cost zero. Carry into the separately reviewed measurement-only PR: timestamp input receipt versus visible response, correlate admission/terminal/ordinary/idle boundaries, record foreground and UI/presentation self time, validation/reader origins, allocation/GC and durable I/O. Separate nested scopes, rendering and any eventual stream encode/network/decode latency. Retain source/fixture/build/ordinary-clock identity and calibrate instrumentation overhead. Sections 10–13 prohibit using this hypothesis to remove validation, add caches/workers or adopt graphics workarounds. No optimization, new instrumentation, sustained campaign or speedup claim was made here.

## Minimal correction proposal and merge disposition

For independent review, propose a presentation-only correction using the existing responsive classes: make compact HUD cards reflow at Default as well as Large text, retain readable localized metric labels/values with adequate width, and move navigation onto a separate row when the shared row cannot fit. Bound long foreground information and optional lower detail so required controls and meaningful map space remain available. For #249 specifically, bound the compact Analysis header on height-constrained/wide layouts too, retaining the complete report in Analysis. Preserve four required HUD metrics, all hit targets, safe areas, text scaling, transaction behavior and shared reporting authority. Do not merely shrink fonts or hide critical labels. This proposal is not implemented and exact breakpoints need review/testing, not invented support thresholds.

**Exact disposition: PR #249 remains draft, unmerged and not approved for merge.** Review must decide the supported viewport/panel/device matrix and whether the baseline HUD correction belongs here or in a separate bounded UI correction; independently verify any new summary-height regression. Approve the three automated-only native edge-case limitations separately. Repeat focused readability/obstruction checks after any approved UI change, then perform actual owner Moonlight portrait/landscape and Default/Large acceptance with recorded host/client/stream dimensions. Native iOS safe-area, 44-point hit regions and device responsiveness require their own supported device evidence. Desktop reporting and launcher feedback do not complete those gates. The pre-existing Windows shutdown limitation remains unresolved; these two normal exits do not relabel earlier failures.
