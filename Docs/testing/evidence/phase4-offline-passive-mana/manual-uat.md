# Phase 4 canonical offline passive mana — external manual UAT

Automated qualification does not constitute manual gameplay qualification. Run these checks in both Unity Editor and the Windows Development Build using real elapsed time; do not change the operating-system clock.

## Editor

1. Start from a valid schema-9 save with mana below capacity. Record mana, Heat tier, and displayed canonical passive rate.
2. Background or pause for a measurable interval, then resume. Verify exactly one fractional offline award approximates 15% of the displayed applicable rate for the observed seconds.
3. Verify the summary shows elapsed time, effective offline rate, award, current mana, and capacity limitation when applicable; verify there are no raw localization keys.
4. Let one normal active tick occur. Verify active production resumes and the offline duration was not replayed as active ticks.
5. Background and resume again and verify only the new interval is credited. Close and reopen and verify the wallet remains durable.
6. Use the existing development-only QA wallet clear or fill control to test near-capacity and full-capacity returns. Verify the wallet never exceeds capacity and the summary explains the clamp.
7. Close cleanly, wait a measurable interval, reopen, and verify one award. Close and reopen immediately and verify the prior interval does not replay.
8. Repeat at existing Peace, Notice and Concern states using supported gameplay or QA routes. Verify the offline rate tracks the canonical displayed online rate and Heat itself does not change offline.
9. Check the Bootstrap summary at 1920×1080 and 1280×720 for unusable clipping, unreadable content, or raw keys. Existing accepted Bootstrap scrolling debt is not part of this packet.

### False-capacity correction rerun

1. Reproduce a below-capacity fractional return with a wallet near `0.423`, no active floor placement, and a measured interval near 63 seconds. Confirm the effective offline rate is 18 mana/hour for that canonical state, the award is approximately `0.315`, and the resulting wallet is approximately `0.738 / 1000`.
2. Confirm that below-capacity result does **not** show the localized capacity-limited message.
3. Repeat with a near-capacity wallet where the candidate exceeds capacity. Confirm the wallet clamps to capacity and the localized capacity-limited message appears.
4. Repeat while already at capacity with otherwise positive generation. Confirm no mana is added and the appropriate at-capacity/capacity-limited presentation remains.
5. Confirm the offline result can still be found and read well enough for validation at 1920×1080 and 1280×720. Record discoverability/readability problems as temporary Bootstrap debt; do not treat a false clamp message as accepted debt.

## Windows Development Build

1. Repeat the core clean close, real-time wait, reopen, single award, immediate reopen and no-replay path.
2. Repeat one background or resume path and one near-capacity clamp path.
3. Confirm the summary remains readable and ordinary active passive ticks resume afterward.
4. Repeat the below-capacity fractional and genuine near-capacity clamp checks above, confirming the capacity-limited message appears only for the genuine clamp.

Record the exact build commit, save setup, observed timestamps and duration, before and after wallet, displayed rate, Heat tier, screenshots at both resolutions, and any discrepancy.
