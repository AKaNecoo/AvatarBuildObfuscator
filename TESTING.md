# Validation — 0.4.0

Validated on 2026-10-20 in the authorized connected Windows test Editor. These are Editor/build/sample results, not actual VRChat client or device acceptance. Private avatar data, raw reports and name mappings are not in this repository.

## Update and CI candidate checks

The 0.3.0-rc.1 candidate passes 60 hosted-compatible assertions against the actual Unity-independent UpdatePolicy source, six Python archive/validation regressions, 52 Unity processing regressions and 51 actual Editor input checks. Nine opt-in Unity integration checks exercise JSON event parsing, build suspension and temporary archive cleanup on completion/failure/cancellation. No archived report or private avatar is needed by hosted CI.

Package CI builds TGZ, ZIP, unitypackage and checksums reproducibly, comparing every archived byte with source. It validates stable component GUIDs and complete three-language strings. It compiles the actual update-policy source with .NET; it does not run the Unity Editor. Trusted main/beta push jobs publish release assets after checks, while PR jobs cannot publish. A version cannot silently replace different released product content.

Routine feature updates use the Git installation for final connected-Editor acceptance, including the relevant processing, input, SDK Play and source-integrity checks. The unitypackage distribution does not need a duplicate full-model or input run for the same product code. CI still checks all archive formats and their bytes. Changes to import/update behavior, installation layout or GUID compatibility additionally require focused tests of the affected installation route, including temporary-download cleanup where applicable. The completed checks below are separate from hosted CI; older multi-route results remain historical evidence.

## Environment

Unity2022.3.22f1, D3D11; Avatars SDK3.10.5; NDMF1.14.8; Modular Avatar1.19.0-alpha.0; Avatar Optimizer1.9.20; VRCFury1.1434.0. Existing lilToon/custom materials, face tracking, NadeVision and third-party avatar construction are present. The package declares NDMF1.14.8+ because older 1.6 lacks a replacement-registry API used here.

## Synthetic checks

**52 PASS, 0 FAIL.** Each case fails on Unity native Error/Assert/Exception messages as well as assertions. Coverage includes deterministic username/I/l sequences, Unicode/path-safe names and empty defaults; static geometry/normals at weights0/35/100; dynamic and constant curves; MMD profiles; native visemes and eyelids; nested/shared Animator graphs; persistent-byte isolation; synced and override-controller links; manual exclusions; mesh swaps; special frames/extrapolation; SDK proxy identity; unknown-driver profiles; cyclic menus; hierarchy float/PPtr/mask paths; parameter/menu/driver/PhysBone/Contact links; coherent vertex channels/skinning/shapes; unreadable/Crunch and readable texture integrity; shader copies; and resource names surviving serialized reference traversal.

New order tests cover both modes, current weights and geometry, multiple morph frames, eyelid indices, animation target identity, independent/disabled switches, and duplicate/UI/manual order preservation. Eight imported fixture textures produce **one import callback batch**, not one per file. This is a focused import-batching regression, not a claim that every external postprocessor runs only once.

Beta.2 adds real default-output-root coverage for unreadable/Crunch imports and rejection of vendor package paths/traversal. The earlier 49 tests only used Assets destinations and therefore missed the Play failure.

Tests~ is opt-in. Copy its Editor/Runtime sources and asmdefs into a disposable test project's Assets hierarchy. Run `GuardValidation.RunAll(outputJsonPath)`. Full tests use `FullAvatarValidation.Start(sourceId, outputDirectory)`, three `RunNext()` calls, then `Finish()`. Import the test-only OrderValidationProbe too: it records renderer identities after upstream optimization and before obfuscation so order-independent comparisons are meaningful. All synthetic fixtures and build outputs stay under the validation directory. Never use tests as a reason to save over a source scene.

## Complete builds (beta.1 geometry regression snapshot)

Fresh baseline/Conservative/Aggressive builds include existing upstream optimizers. No optimizer settings were injected or changed. The final saved comparison was closed/reopened before functional sampling.

| Metric | Baseline | Conservative | Aggressive |
| --- | ---: | ---: | ---: |
| Objects | 414 | 414 | 414 |
| Renamed objects | 0 | 365 | 368 |
| Renderers | 37 | 37 | 37 |
| Retained BlendShapes | 290 | 286 | 249 |
| Renamed retained shapes | 0 | 246 | 249 |
| Renamed mesh instances | 0 | 25 | 25 |
| Materials | 18 | 18 | 18 |
| Renamed materials | 0 | 18 | 18 |
| Material textures | 53 | 53 | 53 |
| Renamed material textures | 0 | 52 | 52 |
| Controllers | 8 | 8 | 8 |
| Unique clips | 827 | 827 | 827 |
| Renamed clips | 0 | 787 | 787 |
| SDK proxy clips | 40 | 40 | 40 |
| Animator states | 647 | 647 | 647 |
| Empty state motions | 40 | 40 | 40 |
| Humanoid bones | 53 | 53 | 53 |
| Missing animation paths | 0 | 0 | 0 |
| Missing shape bindings | 0 | 0 | 0 |
| Missing mask paths | 0 | 0 | 0 |
| Missing parameter references | 0 | 0 | 0 |
| Marker components in output | 0 | 0 | 0 |
| Unity Error/Assert/Exception logs | 0 | 0 | 0 |
| Renamed parameters | 0 | 262 | 291 |
| Renamed Animator internals | 0 | 2144 | 2144 |
| Rebuilt Human Avatars | 0 | 1 | 1 |
| Meshes with shuffled morph order | 0 | 2 | 2 |
| Shuffled sibling groups | 0 | 45 | 45 |
| Objects whose sibling index changed | 0 | 216 | 212 |
| Vertex-permuted meshes | 0 | 0 | 28 |
| Build/inspection/render seconds | 18.21 | 75.16 | 71.03 |

Names use a neutral Chinese fixture username; real user/asset identities are excluded. Deliberate AAO clip-length dummy paths are counted separately from real missing bindings. Built-in and SDK proxy identities remain contractual. Generic/ambiguous hierarchy, duplicate siblings, UI order and other documented exceptions remain protected.

## Reloaded pose and texture comparisons (beta.1 snapshot)

**13 paired clips, 39 sampled poses, 18 material groups and 102 texture pairs passed.** Selected clips cover Human motion, blendshapes and Transform animation at normalized0/0.37/0.83. Corresponding renderers are matched by recorded component identity, not post-shuffle traversal order. Triangle-expanded skinned positions/normals and Human bone positions are compared in avatar-local coordinates.

Maximum position difference: **3.9991e-7 metres**; bone position difference: **8.9407e-8 metres**; normal-vector difference: **2.2302e-7**. Thresholds are0.0002metres for positions/bones and0.002 for normals. GPU texture samples (up to256×256 per pair) show **0 differing pixels**, matching dimensions and graphics formats. Native sampling errors: **0**. This samples texture data rather than exhaustively comparing every full-resolution pixel/mip.

A512×768 static render differs from baseline at a small number of pixels (material/particle timing and rasterization can vary). Static screenshots are supplementary evidence; the numeric pose and texture checks establish the stronger comparison. These checks do not exercise every controller transition, networked state, audio event or external device.

## Import performance and cancellation

The initial complete Conservative run took322.4seconds while copying/importing resources individually. Batching reduced measured complete runs to about69–75seconds on this project. Final batches copy/import362 texture/audio files in roughly48–51seconds. Six shader-source copy creations measured about0.00–0.01seconds each. These are local timings, not a universal performance guarantee or a claim that a fresh GPU shader cache compiles instantly.

Copy/Shader preparation has cancellation boundaries, always releases import suspension and clears its own progress UI. An already-running Unity native import or shader compiler may not stop immediately. Native work is not interrupted by killing the Editor.

## Source integrity and installation changes

A fresh SHA-256 manifest covers22,232 pre-existing files under Assets, Packages and ProjectSettings. Excluding the intentionally replaced plugin installation, validation fixtures, manifest/lock changes and VRCFury's generated temp package leaves **21,635 protected original files**, including13,285 source Assets files. **0 protected originals changed or went missing** at the final integrity checkpoint.

AAO automatically changes `legacyClampBlendShapeWeights` from1 to0 after script reload in Edit Mode. This was traced to AAO's initialization hook; the single field was restored and the complete ProjectSettings file matched the original SHA-256 exactly. VRCFury-generated temp controllers/logs are classified as build caches, not model source changes. Library/Temp/Logs/UserSettings are not source-manifest evidence.

The duplicate old Assets plugin was byte-checked before removal; the test project uses one UPM installation. Original component GUIDs are retained. Git beta is the delivery channel for this update; existing0.2.0 release attachments were not rebuilt. No avatar assets, reports, credentials or mappings are published.

## Issues caught and remaining limits

This iteration caught canonical proxy motions being lost when temporary upstream assets were destroyed, private Humanoid parent-name remapping, NadeVision's fixed shader-name lookup, resource names being restored by a generic serialized-string walk, and repeated per-resource import overhead. Regression checks cover the repaired behavior. A new BlendTree fixture initially omitted its float parameter; the fixture was corrected before the final pass.

Actual VRChat upload/client runtime, network/menu/face-tracking interaction and Quest/iOS devices are **not verified**. Arbitrary external name/index consumers, every custom shader/mesh format and every Unity/SDK version are not covered. Conservative uses recognized contracts and protection rules; Aggressive explicitly permits unknown compatibility risks. Texture pixels, necessary dynamic morphs and final geometry remain extractable.

## Beta.2 Play and Inspector verification

The actual Editor Play transition invokes the VRChat SDK preprocessing callbacks, including NDMF's default package output directory, rather than an Assets override. It successfully reaches EnteredPlayMode and returns through ExitingPlayMode/EnteredEditMode. The observer records one obfuscated avatar, 400 generated object names, 53 Humanoid bones, zero remaining configuration markers and zero Error/Assert/Exception messages during the observed interval. This verifies Editor Play, not a VRChat client upload.

`PlayModeValidation.Arm(outputJsonPath)` is an opt-in test observer that survives domain reload using SessionState and captures state transitions/native errors. After entering and leaving Play, call `Finish()`; it fails if Play was not reached, no obfuscated output appeared, markers remained or errors occurred. It does not change Play mode or suppress callbacks itself.

All 38 Inspector translation entries contain Chinese/English/Japanese text; all used Inspector keys resolve. Serialized language choices round-trip in all three languages without changing GuardSettings. The actual Chinese Inspector was visually inspected. No language/package dependency or source-scene save is required to use the selector.

The full three-way mesh/pose/texture tables above were measured in beta.1 and are retained as that version's evidence; beta.2 reruns the synthetic suite and adds native Play-path verification. It does not claim those earlier tables were recomputed in this patch.

## Beta.3 username input and component naming

The local beta.3 package passes 51 UI assertions in a preview-scene fixture containing two independent bound Inspector views, plus all 52 synthetic processing checks. The UI checks exercise Chinese/English/Japanese label changes, real pointer hit testing, keyboard events across separate Editor updates, Unicode text, cross-view synchronization, Undo/Redo, clipboard paste and recreating an Inspector before continuing to type. They check both the component value and displayed field values. Windows IME candidate-window composition is not simulated by these Unicode key/paste checks.

The fixture is opt-in: call InspectorInputValidation.Start(outputJsonPath). It opens a temporary tool window, starts after five seconds and requires the Unity window to remain active. Conditions allow up to two seconds for asynchronous binding propagation. Cleanup closes the window and preview scene, and clears Undo records for the fixture only. The fixture must remain editable: HideAndDontSave includes NotEditable and is unsuitable here. It uses HideInHierarchy/DontSave flags instead.

The new standard Inspector keeps the label-left/input-right username row and other existing controls. A native bound TextField isolates text editing from the IMGUI layout loop. Localized caption changes are silent: a normal child Label text change was observed bubbling into Unity's string binding and restoring a stale username during a combined external value/language update. The regression above catches that behavior. The original intermittent beta.2 no-caret trigger has not been conclusively identified; passing this fixture is not a claim that every Editor/input-method condition has been reproduced.

The component menu is Avatar Tools/Avatar Build Obfuscator and Unity ObjectNames.GetInspectorTitle returns Avatar Build Obfuscator. Runtime type, assembly, serialized fields and script GUID are retained. No build processor changes are part of beta.3. The beta.1 full-model tables, beta.2 native SDK Play transition and beta.2 source-integrity checkpoint remain historical results, not fresh beta.3 full-avatar/client runs.

The first hosted candidate run succeeded in both validation/build and release publication: [Package CI 39379028930](https://github.com/AKaNecoo/AvatarBuildObfuscator/actions/runs/39379028930). The actual Unity updater read this successful push and candidate metadata from GitHub, retained beta and reported update.current. The candidate release includes all three archives and checksums, with GitHub SHA-256 digests.

The RC1-to-0.3.0 manual Assets update succeeded using the published archive, preserving component fields/GUID and deleting the temporary installer. A subsequent manual metadata-only reimport exposed a stale cached version; 0.3.1 invalidates that cache on manual imports and rechecks version eligibility immediately before archive import. A targeted Legacy integration regression covers this finding. The completed 0.3.1 results follow below.


## Completed 0.3.1 update and Play acceptance

The 0.3.1 product commit is `9c9ad54cd649a8ba35cf5930350f7ebcd26a913a`. [Package CI 39380302132](https://github.com/AKaNecoo/AvatarBuildObfuscator/actions/runs/39380302132) successfully compiled/ran the 60 update-policy assertions, passed six packaging regressions, built all archives/checksums and published v0.3.1 candidate assets. These are 0.3.1 checkpoint results; stable promotion was subsequently paused following a new long-username Play failure.

| Actual route | Starting version | Result | Source settings / script GUID | Temporary installer |
| --- | --- | --- | --- | --- |
| Assets, one-click update | Official 0.3.0-rc.1 | 0.3.1, passed | Preserved | Deleted |
| Assets, automatic update | Official 0.3.0-rc.1 | 0.3.1, passed | Preserved | Deleted |
| Git, one-click update | Managed 0.3.0-rc.1 | Exact verified 0.3.1 SHA, passed | Preserved | Not downloaded |
| Git, automatic update | Managed 0.3.0-rc.1 | Exact verified 0.3.1 SHA, passed | Preserved | Not downloaded |

Automatic routes enabled the project preference and let the production Editor update loop install; they did not invoke the manual installation action. Each started with an old-version fixture, retained beta and recorded zero native errors. Git fixture preparation restored a known CI-passed old SHA with its managed channel metadata; this does not grant update permission to arbitrary manually pinned installations. All routes restored automatic installation to its original off state.

The final Git package reran 52 processing checks (zero failures), 51 Inspector input assertions and nine Git-mode updater integration assertions. Assets mode passed ten integration assertions, including manual-import metadata-cache invalidation. Cleanup checks cover successful/failed/cancelled imports. The real SDK/NDMF Play test reached all four entry/exit events, observed one obfuscated avatar, 400 generated object names, 53 Humanoid bones, zero configuration-marker remnants and zero Error/Assert/Exception messages, then returned to Edit. It neither suppressed callbacks nor saved the source scene.

During repeated installation-mode test switches, an old compiled assembly remained loaded after source replacement. One clean script compilation resolved the test environment mismatch; the loaded updater was verified against its corrected method, and the Assets integration suite then passed. No compiler-cache cleanup runs as part of the shipped updater. Windows IME candidate composition and actual VRChat client/device acceptance remain outside these checks.

The route observer accepts a unique scene component or an explicit component instance ID and persists its GlobalObjectId across reloads. It has no private avatar/scene-name dependency. Arm with `PackageUpdateRouteValidation.Arm(output, route, expectedSource, commit, version, optionalSourceInstanceId)`. Its JSON records original fields, the resolved channel/commit, native errors, cleanup and automatic-opt-in state; it restores opt-in after completing.

At the completed 0.3.1 checkpoint, a fresh scan verified 21,635 protected source files against their pre-test SHA-256 values with zero changed/missing files, including the saved source scene, models, textures, animations and metas. SDK/AAO changes to PlayerSettings were reconciled only after the complete file hash matched the fresh baseline; no source-scene save was performed.

## 0.3.2 long-path Play fix

A subsequently entered long username reproduced an actual SDK/NDMF Play failure in the imported-audio copy step. The failing full path was 262 characters. Copying the same original Ogg file into controlled fixture paths succeeded at 230, 250 and 258 characters but failed to import at 262 and 275, despite CopyAsset returning true. This isolated the native audio import limit rather than a C# compilation error or the generic SDK popup itself.

Temporary texture/audio filenames now fit a 240-character full-path budget with extra room reserved for unique suffixes. Internal build-object names retain the full generated username prefix and 128 I/l characters. If the output directory cannot accommodate at least one prefix character and 64 suffix bits, the original reference is kept with a report. The change does not change source files/importers or disable SDK/NDMF callbacks.

The local 0.3.2 Editor run passed all 54 processing cases, including two new long-path regressions covering unchanged audio samples/file bytes/importer bytes and safe overlong-directory fallback. [Package CI 39385369091](https://github.com/AKaNecoo/AvatarBuildObfuscator/actions/runs/39385369091) passed 60 update-policy assertions and six archive checks, built all three archives with byte verification, and published the beta release. The installed Git package was verified as commit `31c23bc9529f95c9758b7383a216ec0e11189770`, version 0.3.2.

Both the local and published Git distributions passed native SDK/NDMF Play with the actual long username: all four entry/exit events, one obfuscated avatar, 400 generated objects, 53 Humanoid bones, zero remaining markers and zero errors. Both observers were finished and the Editor returned to Edit. The local run also checked all 279 temporary audio paths at 232 characters; their in-memory names retained the full 161-character generated name. The actual username/mode/language were retained and no source-scene save was performed. Actual VRChat client upload and device behavior are outside this result. Main promotion remains on hold at the user's request.

After the Git Play run, the fresh protected-source scan was repeated: all 21,635 files matched the same pre-test SHA-256 baseline, with zero changed/missing files. The SDK/AAO PlayerSettings side effect was reconciled using native APIs and exact whole-file baseline matching. The Editor was left in Edit mode with Git beta 0.3.2, automatic installation off, no duplicate Assets installation and no temporary downloaded installer.

## 0.4.0 four-glyph names and avatar-ID defaults

Generated suffixes now use exactly U+00CC/U+00CD/U+00CE/U+00CF: 64 symbols encode 128 bits of naming information. Names retain a sanitized visible prefix of at most 32 characters. Shuffle seeds come directly from the byte stream rather than testing visible letters. This changes generated output names; it does not change the component GUID or stored user settings, encrypt texture/mesh data, or require runtime decryption.

Newly added components read an existing avatar blueprint ID as the initial username. No ID means an empty field requiring manual entry. Ordinary edits, clearing, language changes and Inspector recreation do not refill the field or overwrite entered text. The editor-only Reset implementation reads PipelineManager serialization without adding an editor-only SDK DLL dependency to the runtime assembly.

Local checks passed 59 processing cases, 51 Inspector input assertions, 60 update-policy assertions, 15 hosted checks against the actual name generator and six archive regressions. New checks include independent HMAC name/seed vectors, exact code points/lengths, invalid-name rejection, username-dependent shuffle seeds, native component-add defaults, retained manual edits and Unicode long-path audio sample/file/importer integrity. A test-only SendMessage Reset invocation initially caused an Editor native assertion; the fixture now invokes that callback directly, while component addition still checks the real Unity Reset path. Final results use the refreshed compiled fixture.

A newly added third-party atlas configuration produced missing shader-property errors in the no-obfuscator baseline and native Play. The operator removed that component; no atlas configuration, third-party code or material files were changed by this implementation. The subsequent baseline completed with zero native errors. The following final model results use that operator-adjusted scene.

| Full model check | Baseline | Conservative | Aggressive |
| --- | --- | --- | --- |
| Objects | 414 | 414 | 414 |
| Generated object names | 0 | 365 | 368 |
| Retained shapes | 290 | 286 | 249 |
| Missing animation paths / shape bindings / mask paths / parameters | 0 / 0 / 0 / 0 | 0 / 0 / 0 / 0 | 0 / 0 / 0 / 0 |
| Native errors | 0 | 0 | 0 |

The generated comparison scene was saved, unloaded and reloaded before functional sampling. Thirteen clip groups, 39 sampled poses, 18 material pairs and 102 texture pairs passed. Pixel differences were zero; maximum position/normal/bone differences were about 4.0e-7 / 2.3e-7 / 8.9e-8. Only test copies were sampled. Comparison and sampling scenes were closed afterward; the source scene was not saved.

Native SDK/NDMF Play with the local 0.4.0 package passed all four entry/exit events: one obfuscated avatar, 400 generated object names, 53 Humanoid bones, zero residual markers and zero Error/Assert/Exception messages. The observer was finished and the Editor returned to Edit. A live Animator probe distinguished all four chosen code points and found no parameter-hash collisions across the eight inspected controllers. These are Editor checks, not an actual VRChat upload/client/device or extraction-resistance acceptance test. Main remains on hold.

[Package CI 39391572616](https://github.com/AKaNecoo/AvatarBuildObfuscator/actions/runs/39391572616) passed and published the [0.4.0 prerelease](https://github.com/AKaNecoo/AvatarBuildObfuscator/releases/tag/v0.4.0). The connected Editor was switched to Git beta and verified as version 0.4.0, exact product commit `dec5d7ed70a06dd4fc9d33998558ff548c1a1bc2`. Final Git SDK/NDMF Play passed with the same four events, one avatar, 400 generated names, 53 Humanoid bones, zero markers and zero errors; the observer was finished. No unitypackage was imported for this feature-only update, and full input/model tests were not duplicated by installation format.

The final scan compared all 21,635 protected files with SHA-256 values freshly read before this iteration. Exactly 21,630 matched and none were missing. The other five were the VPM manifest and four lilToon files (package metadata, two changelogs and openlit_core.hlsl), changed by an operator-confirmed lilToon 2.4.0-to-2.4.1 update during testing. Those changes were preserved. The saved source scene, model/texture/material/animation files, metas and complete ProjectSettings file matched the fresh baseline. SDK/AAO native settings were reconciled to that verified baseline, the actual component fields were retained, and the source scene was not saved. The Editor was left in Edit mode with one Git installation, automatic installation off and no pending installer.
