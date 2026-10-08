# Windows Development qualification — 2026-10-08

This historical build is superseded for visual qualification by the corrected UI player in [ui-correction.md](ui-correction.md). Its artifact and report remain preserved.

The owner explicitly authorized proceeding to the Windows build with the two unchanged Bootstrap clipboard failures still visible. This is a sequencing exception for build and manual qualification, not successful full PlayMode qualification or merge approval. No clipboard investigation, tests, security diagnostics, or implementation changes were made in this continuation.

Built source HEAD: `2cd413b044495ab0437451796bc7b6670033b36e`, branch `codex/phase-7-transactional-graphical-room-construction`, base `72924bdf54d222332b256cab254a22126f3b4bfd`. Recommended configuration: GPT-6.1 Sol / Medium, Standard; narrowly scoped established build qualification; fallback GPT-6 Sol / Medium.

## Observed build

Existing Unity CLI `1.0.0-beta.10`, Unity `6000.3.2f1`, existing `DevelopmentBuildUtility.BuildWindowsDevelopment`, StandaloneWindows64, Development, Bootstrap-only scene. CLI exited **0**. The actual [build report](windows-build-report.json) records **Succeeded, 171,715,592 bytes, 0 errors, 1 warning**, timestamp `2026-10-08T13:20:44.0290449Z`.

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' build 'C:/Dev/Dungeon-Lord/Temp/room-construction-validation' --target StandaloneWindows64 --execute-method DungeonBuilder.M0.EditorTools.DevelopmentBuildUtility.BuildWindowsDevelopment --allow-dirty-build --log-file 'C:/Dev/Dungeon-Lord/Temp/room-construction-windows-build.log' --no-tail --timeout 1800 --no-color
```

Complete player preserved at `C:/Dev/Dungeon-Lord/Builds/Phase7RoomConstruction-2cd413b-20261008/Windows/Dungeon Lord.exe` with all adjacent files. Executable size: 667,648 bytes. Original output remains in the validation checkout's `Builds/Development/Windows`. [Artifact manifest](windows-artifact-manifest.json) binds relative paths, sizes and SHA256 hashes. The redacted log is preserved at `Builds/Phase7RoomConstruction-2cd413b-20261008/windows-build.log` and `Temp/room-construction-windows-build.log`. The player has not been launched and owner UAT has not passed.

## Warning and diagnostic text

The postprocess warning reads: `Exception occurred attempting to connect to Unity services. Native symbols will not be uploaded for this build. Exception details:` followed by `System.UriFormatException: Invalid URI: The URI is empty.`

The symbol uploader separately logs: `Unable to upload symbols to Unity Cloud Diagnostics: Unity Cloud Diagnostics credentials unavailable. Please provide an auth token with USYM_UPLOAD_AUTH_TOKEN environment variable`. This is an external symbol-upload failure; the build report has zero build errors. No credentials or security settings were changed.

The Editor also logs the existing notice: `Assembly for Assembly Definition File 'Assets/_Project/Tests 1/Tests 1.asmdef' will not be compiled, because it has no scripts associated with it.` Shutdown logs `debugger-agent: Unable to listen on 3600`; the process then exits successfully. No C# compiler warning or build-blocking error was observed. Shader names containing `FallbackError` are compiled asset names, not build failures.

## Save isolation and evidence continuity

Before boot/build, **only the disposable validation copy** was given a new save filename in its build config and SaveService fallback: `phase7-room-construction-uat-b681ebc8e4664312a1feb2a471ebd4f2.json`. This intentionally continues isolation into both Editor and standalone manual testing. It replaces the earlier proposal to restore the normal filename before build. The artifact is a validation player, with this explicit isolation difference from production configuration; it must not be described as an unmodified release artifact. Root config/source/default filename remain untouched. All 25 intended Assets hashes still match the qualified manifest. Only the validation save namespace changed; no gameplay/test implementation changed.

Owner primary save and both existing owner draft records match the recorded pre-qualification SHA256 values after build. Read-only verification is recorded at `TestResults/room-construction-owner-save-hashes-after-build.json`. Root ProjectSettings SHA256 remains `34DA6D701E4C4629CA7B1CECB638F801D9C5EA4F40D33B09FECA46777073B993`; TMP Settings blob remains `92a60536387caf4a8caaed785b4c07b48abdf201`. No owner-local files were reset, stashed, cleaned, overwritten or included in the evidence commit.

Full PlayMode remains **3,044 total / 3,032 passed / 2 failed / 10 skipped**, with original reports retained. Previously qualified EditMode, construction, economy, persistence and production gates were not repeated. Canonical schema 13, draft compatibility, final-state accounting and atomic-save evidence remain valid. The first-room Bootstrap dependency remains unchanged. External review, owner acceptance of that limitation and [Editor/standalone UAT](owner-uat.md) remain required; this PR is not merge-ready.
