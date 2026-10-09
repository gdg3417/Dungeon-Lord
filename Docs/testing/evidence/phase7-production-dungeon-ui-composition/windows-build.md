# Windows Development build

**Succeeded**, Unity `6000.3.2f1`, StandaloneWindows64, Development Build, Bootstrap entry scene only. Build report: **0 errors, 1 warning**. CLI exited 0. Qualified Unity/content source commit: `5d850dfe3f2befb4e708c02b6829fcdf1c7bf9b6`; later evidence-only commits retain identical project inputs.

The complete retained artifact is:

`C:/Dev/Dungeon-Lord/Builds/Phase7UIComposition-5d850df-20261008/Windows/`

It contains `Dungeon Lord.exe`, `Dungeon Lord_Data`, `UnityPlayer.dll`, `MonoBleedingEdge`, D3D12 libraries, crash handler, WinPix runtime, Burst debug information and adjacent reports. **303 files / 181,605,067 retained bytes** were copied and SHA256 checked against the build directory. Keep the complete folder. [Artifact manifest](windows-artifact-manifest.json) lists every file. This is a local retained artifact, not a hosted binary download.

The established `DevelopmentBuildUtility.BuildWindowsDevelopment` performed the build. The small editor qualification entry point then compared Bootstrap/Visuals/USS dependency art against `BuildReport.packedAssets` contents. All **50 referenced PNGs** were actually packed, including the identity atlas, stone/corridor/rock surfaces, boundary/selection/invalid/grid/anchor sprites, fallback emblems, resource icons and panel/action frames. Three currently unused floor/focus/collapse icons are imported/tested but are not build dependencies. [Packed dungeon art](packed-dungeon-art.txt), [complete packed sources](packed-source-assets.txt), [build report](build-report.json) and [build messages](build-messages.txt) are retained. Unity documents the [packed asset report](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Build.Reporting.BuildReport-packedAssets.html) and its [asset contents](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Build.Reporting.PackedAssets-contents.html).

The single warning is: “Access token is empty. Native symbols will not be uploaded for this build. Please make sure you are signed in to the Unity Cloud.” It concerns Cloud diagnostics symbol upload, not compilation or missing dungeon art. No new C# compilation warning was recorded in the final build. The full log is in `unity-logs-redacted.zip` as `ui-composition-windows-build.log`; local original remains `Temp/ui-composition-windows-build.log`. Shutdown Mono abort/debugger-port messages are retained without presenting them as test failures.

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' run 'C:/Dev/Dungeon-Lord/Temp/ui-composition-final-validation' --timeout 1800 --no-color -- -buildTarget StandaloneWindows64 -executeMethod DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildWindows -logFile 'C:/Dev/Dungeon-Lord/Temp/ui-composition-windows-build.log'
```

The build's config **and** SaveService fallback retain `phase7-ui-composition-validation-01a11d08.json`. The normal repository namespace is unchanged. No owner primary save or draft record was opened for modification. The executable was not launched for owner UAT.

All 1,188 inputs matched source immediately before the build in [prebuild-source-manifest.json](prebuild-source-manifest.json). Unity's build then serialized derived URP shader-prefilter/runtime settings, Standalone batching defaults and application-identifier ordering, and UnityConnect enablement in the **disposable copy**. [Post-build drift manifest](postbuild-source-manifest.json) and [exact serialization diff](post-build-serialization.diff) retain those four changed files. These outputs were not copied to source or committed. After restoring validation-only files from source, [qualified-source-manifest.json](qualified-source-manifest.json) again reports 1,188 matches and zero mismatches, with only documented save-namespace/text normalization permitted.

Full raw prebuild/post-test/post-build comparison manifests and the original packed-source listing/serialization diff are retained in [validation-source-history.zip](validation-source-history.zip). The readable summaries retain comparison counts, all mismatching rows and original SHA256. Readable listing/diff copies trim trailing spaces only; original build files and retained player hashes remain unchanged. [Qualified input tree IDs](qualified-input-trees.json) tie the final PR's Unity/content trees to the implementation commit.

Owner Windows UAT remains **pending external review**. Build success and packed-art verification do not establish low-end mobile performance, final visual quality or release readiness. See [proposed owner UAT](owner-uat.md).
