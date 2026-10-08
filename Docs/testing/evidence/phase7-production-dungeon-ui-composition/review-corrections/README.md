# PR #230 external review corrections

Reviewed HEAD: `eb552892122abf95b2c55360c1ef75328aff6e4c`. Corrective implementation: `6438af229ffc67f805827535e013317293af1560`. Existing branch/base main `7b5911e5482aa20851e8329c0bb311f9aa6e7206` preserved; no new PR or merge. Recommended configuration: GPT-6.1 Sol / Medium, Standard, bounded integrity/presentation corrections; no availability substitution.

## Findings

**P1 source equality:** former preparation overlaid files; verification checked only expected names. Removed/renamed scripts, PNGs or metadata could remain available to Unity while equality appeared successful. The retained historical checkout currently has 1,188 expected/actual inputs and zero extras ([read-only inventory](historical-target-inventory.json)); this does not repair the original missing guarantee. Original reports remain historical. Fresh qualification uses the corrected workflow.

Preparation and verification share `Tools/Presentation/qualification_inputs.py`. Complete input roots: **Assets, Packages, ProjectSettings, ContentAuthoring**. Git-tracked and nonignored untracked files in all four roots are included, with required metadata; missing tracked source files fail. Every target file in these roots counts, including ignored/generated metadata. There is no target-only allowlist. Docs/Tools are evidence/tooling, not Unity inputs; source inspection found no Unity test dependency on Docs. Library, Logs, Builds, UserSettings and TestResults are output roots outside the inventory.

Preparation rejects extras **before copying** and directs the operator to a new disposable directory. It never deletes anything. Only a single `ui-composition-*` directory name under this repository's Temp is accepted. Absolute/nested/traversal paths, symbolic links and Windows ancestor/descendant reparse points/junctions are rejected; unreadable inventories fail. Copying is followed by complete name/content comparison. The verifier rejects missing, changed and target-only inputs. There is no cleanup path to the owner project, saves, Library or unrelated validation directories.

Explicit transformations: canonical build_config save filename and SaveService fallback become `phase7-ui-composition-validation-01a11d08.json`. Verification constructs the exact expected transformed content, rather than undoing arbitrary target substitutions. Config JSON formatting and text CRLF/LF/UTF-8 BOM normalization are allowed. Wrong namespaces and other content changes fail. Application identity is checked. Validation serialization drifts are recorded before source bytes are restored only in the identified disposable input roots.

**Collapsed rail:** expanded widths remain 128 / Large 156 layout units; collapsed widths change from 102 / 128 to **72 / 88**, recovering 56 / 68 units instead of 26 / 28. Localized compact “Show” caption/full “Show floors” tooltip retain a labeled control. Scene tests measure viewport recovery, hit width, three text sizes and both orientations; text measurement prevents splitting the fallback caption. Genuine touch/mouse clicks collapse/expand without canonical writes or draft commands. Descending floor order and expanded indicators are preserved.

**Summary semantics:** the existing FloorLayoutValidator provides a complete ActivationValid result, not a separate route result. The localized summary now says **Floor layout: Valid / Blocked**. A regression starts with a valid seeded route, supplies a fixture-only immutable presentation snapshot with zero capacity, verifies CapacityExceeded is the only issue, checks the broader label and restores the original context. No parallel validator, eligibility rule, production config or gameplay authority change.

## Helpers and focused results

`python Tools/Presentation/test_qualification_inputs.py`: **12 passed**, twice after fixture correction. Isolated temporary Git repositories cover clean acceptance, untracked assets/metadata, idempotence, preserved Library sentinel, removed C#/PNG/meta rejection, missing/changed/target-only inputs, allowed/wrong namespace transforms, unsafe targets, Windows target/descendant junctions, missing tracked source and unchanged unrelated outside sentinel. No destructive fixture uses the owner project/save directory. [Final output](helper-tests.txt).

The sandbox attempt could not initialize temporary Git repositories (24 setup/cleanup errors). First unrestricted helper run: 10 pass / 2 fixture errors because git rm refused staged additions without an initial commit. Fixture-only force removal corrected the setup without weakening comparisons.

Two intermediate scene/art runs each passed 32 / failed 1: the capacity fixture changed cloned catalog data, leaving displayed inputs unchanged. Both reports/logs are retained. Final fixture supplies/restores its own immutable context. Corrected focused composition: **3 passed / 0 failed / 0 skipped**. Final production PlayMode/Input System focused run: **29 / 0 / 0**, including touch/mouse rail and construction/recovery. Full suites also rerun scenes/art from the final implementation.

Runtime captures showed word splitting during the 64-unit / “Floors” caption experiments. Final 72/88 widths use “Show” and pass measured text fit. No art assets or authored geometry changed.

## Current qualification

Fresh qualification is complete. Parent reports remain historical, not proof the former verifier excluded stale inputs. Owner manual UI/Windows UAT remains pending external review. Schema 13, migrations, draft formats, persistence, costs, occupancy, simulation and snapshots are unchanged.

| Check | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Final isolated helpers | 12 | 0 | 0 |
| Corrected focused composition | 3 | 0 | 0 |
| Final production PlayMode / Input System | 29 | 0 | 0 |
| Full EditMode (1,613 total) | **1,612** | **0** | **1** |
| Full PlayMode (3,051 total) | **3,041** | **0** | **10** |

Full EditMode includes all 27 actual production scene cases, 6 imported-art cases and 37 transactional-editor cases, plus affected construction/movement/durability/recovery and production content/build gates. Full PlayMode includes all 29 production shell cases. Both unchanged clipboard tests pass; no PR #229 exception is applied. [Case-by-case historical comparison](historical-result-comparison.json): each suite adds one passing semantics test; no removed cases or changed outcomes. The one EditMode inverse-Windows skip and ten PlayMode skips are unchanged. Names/reasons and both intermediate scene failures are preserved in [report summary](report-summary.json) and [raw XML archive](qualification-reports.zip). [Seven redacted logs](unity-logs-redacted.zip) and [compiler/diagnostic index](log-index.json) retain actual startup/negative-test/shutdown diagnostics; final full suites/build have no C# compiler warnings/errors.

[Exact commands](commands.md). The fresh checkout was populated by the corrected helper, not an archive overlay. [Pre-launch inventory](pre-unity-source.json), [pre-full EditMode](pre-full-editmode-source.json), [pre-full PlayMode](pre-full-playmode-source.json), [pre-build](pre-build-source.json) and [final post-build comparison](qualified-source-manifest.json) each compare **1,188 expected / 1,188 actual inputs, zero missing/changed/target-only inputs**, with only the checked save namespace and text/config formatting transformations. [Qualified input trees](qualified-input-trees.json) bind the Unity/content set to corrective commit `6438af229ffc67f805827535e013317293af1560`; subsequent evidence-only changes retain these four trees.

Post-focused/full-PlayMode comparison correctly reports TMP fallback serialization and PlayerSettings ordering ([manifest](post-full-playmode-source.json), [exact diff](post-full-playmode-serialization.diff)). Post-build comparison correctly reports four derived changes: URP shader prefilters, URP runtime-settings references, PlayerSettings ordering/default batching serialization, UnityConnect enabled flag ([manifest](post-build-drift-source.json), [exact diff](post-build-serialization.diff)). No extras are found at any checkpoint. These are recorded, then restored from source in the disposable target; no output is silently exempted from comparison. Library is retained. Root settings/fonts/packages are unchanged.

**Windows Development build: Succeeded, StandaloneWindows64, Bootstrap only, 0 errors / 1 warning**: “Access token is empty. Native symbols will not be uploaded for this build.” [Build report](build-report.json), [messages](build-messages.txt), [50 actual packed artwork references](packed-dungeon-art.txt). Complete adjacent player retained at `C:/Dev/Dungeon-Lord/Builds/Phase7UIComposition-Review-6438af2-20261008/Windows/`: **303 files / 181,605,402 bytes**, copied and SHA256 verified against the build output ([artifact manifest](windows-artifact-manifest.json)). The executable was not launched for owner UAT.

Owner primary save, both draft records, root ProjectSettings and TMP settings remain byte-identical ([five hashes](owner-preservation.json)); the complete primary/draft prefix file set is unchanged ([inventory](owner-file-set.json)). Validation filename substitution and GUID fixture paths are verified before boot. No owner primary save deletion, fallback boot or unrelated settings change. No schema/migration/transaction authority changes.

## Measured rail recovery and captures

All 13 full source-comparison manifests are retained byte-for-byte in [source inventories](source-inventories.zip), verified before compacting duplicate checkpoint JSON. Compact checkpoints retain counts, failures, mismatch rows and each archived entry's SHA256. The final qualified-source-manifest.json remains fully expanded with all 1,188 rows. Original historical reports/archives are untouched.

Pixels below are actual production viewport measurements from final scene/Input System runs. The old recovery of 26/28 layout units is derived from the reviewed stylesheet (128−102 / 156−128), not claimed as a new runtime baseline capture.

| Viewport | Text | Expanded map width | Collapsed map width | Recovered pixels |
|---|---|---:|---:|---:|
| 720×1280 | Small / Default | 592 | 648 | 56 |
| 720×1280 | Large | 564 | 632 | 68 |
| 1080×1920 | Small / Default | 888 | 972 | 84 |
| 1080×1920 | Large | 846 | 948 | 102 |
| 1920×1080 | Small / Default | 1728 | 1812 | 84 |
| 1920×1080 | Large | 1686 | 1788 | 102 |

Expand-control panel widths are 60 / Large 76 units (native pixel scale 1 / 1.5 in these captures); all exceed the 48-pixel test minimum. English caption fit is measured in every size/orientation. [Normal expanded](screenshots/composition-review-rail-expanded-720x1280.png), [Normal collapsed](screenshots/composition-review-rail-collapsed-720x1280.png), [Large portrait 720](screenshots/composition-review-rail-collapsed-large-720x1280.png), [Large portrait 1080](screenshots/composition-review-rail-collapsed-large-1080x1920.png), [Large landscape](screenshots/composition-review-rail-collapsed-large-1920x1080.png) are actual Unity captures. Final “Show” caption is readable; bottom sheet remains below the viewport. Original before/after art screenshots remain in the [parent index](../screenshots.md).

Repeated presentation checks retain 4 reconstructions / 3 pooled renderers / zero draft commands across 12 focus/fit/sheet/tick cycles; 24 selected-floor switches retain pool size. No new FPS/allocation/mobile performance claim. Existing first-pass art, category fallbacks, physical-device/Japanese-font and first-room onboarding limitations remain; no new assets/dependencies or follow-on development.

Ready for another external review after publication. Owner UAT remains pending: after approval, check rail collapse/expand with mouse/touch, three sizes, portrait/landscape, floor ordering/indicators, accurate floor-layout summary, Normal selection/Focus/Fit and retained construction/invalid restart/Save/Discard using the isolated player. Follow the retained [owner UAT plan](../owner-uat.md); do not merge automatically.
