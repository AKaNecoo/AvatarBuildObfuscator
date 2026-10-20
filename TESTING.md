# Validation — 0.3.0

Validated on 2026-10-20 in the authorized connected Windows test Editor. These are Editor/build/sample results, not actual VRChat client or device acceptance. Private avatar data, raw reports and name mappings are not in this repository.

## Update and CI candidate checks

The 0.3.0-rc.1 candidate passes 60 hosted-compatible assertions against the actual Unity-independent UpdatePolicy source, six Python archive/validation regressions, 52 Unity processing regressions and 51 actual Editor input checks. Nine opt-in Unity integration checks exercise JSON event parsing, build suspension and temporary archive cleanup on completion/failure/cancellation. No archived report or private avatar is needed by hosted CI.

Package CI builds TGZ, ZIP, unitypackage and checksums reproducibly, comparing every archived byte with source. It validates stable component GUIDs and complete three-language strings. It compiles the actual update-policy source with .NET; it does not run the Unity Editor. Trusted main/beta push jobs publish release assets after checks, while PR jobs cannot publish. A version cannot silently replace different released product content.

Before stable promotion, the maintainer additionally verifies the real Git/package upgrade routes, temporary-download deletion, native SDK Play transition and protected-source integrity in the connected Unity project. Candidate test results above are local checks; successful remote CI and actual upgrade observations are recorded after they run. They are not implied by this candidate declaration.

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
