# Validation

Latest connected Editor checks: 2026-10-23 in the authorized Windows test Editor. Stable packaging/promotion checks: 2026-10-24. Editor/build/sample checks are separate from the user's online basic-function testing. Private avatar data, raw reports and name mappings are not in this repository. Older sections below retain their stated version boundaries.

## 0.7.0 stable promotion

The user explicitly requests stable publication and reports having tested basic model functionality online without finding problems. For future stable requests, that instruction also conveys the user's completed online smoke test; it does not enumerate exhaustive menu, Animator, physics, networking or face-tracking coverage.

Relative to the tested `0.7.0-beta.2` commit `57abe0d`, the stable product changes only `package.json` from `0.7.0-beta.2` to `0.7.0`. All 69 other package files, including C#, shader, assembly definitions, bundled documentation and GUID metadata, are identical. Earlier beta.2 Git-install, two-mode model, SDK Play and source-integrity results below remain evidence for that tested implementation; no new connected-Editor run is claimed for the version-only promotion.

Stable preparation reruns the 60 update-policy, 28 naming and 166 compressed-block assertions, eight archive regressions, and exact readback of all 70 product files in UPM/TGZ, ZIP and unitypackage archives. The short Chinese/English/Japanese release notes cover changes since 0.6.0, retain the experimental compatibility limits, and do not claim content encryption or measured VRChat download-size reductions. The withdrawn 0.7.0-beta.1 Release remains a draft.

## 0.7.0-beta.2 texture storage and streaming

On Unity 2022.3.22f1 / Windows D3D11 / Linear, **64 shader/texture checks**, **59 processing**, **30 enhancement** and **15 diagnostic** cases pass. The texture suite now checks original format/graphics format and every mip for unreadable RGBA32, ARGB32, BGRA32, RGB24, DXT1, DXT5, BC4, BC5 and BC7 in both storage color spaces. BC7 must either pass its per-channel 1/255 decoded quality limit or reject without changing the source. Both rotation routes check streaming enablement, priority and the single-mip exception; a compressed NPOT mip layout is retained, and format expansion is rejected.

The actual Unity-independent block rotation source passes **166** assertions, including independent endpoint/selector decoding, partial tail mips, unchanged input bytes, unchanged compressed length, two-rotation byte restoration and malformed-layout rejection. Existing update/name checks remain **60/28**, archive regressions **8**, and three archive formats read back all **70** product files. These hosted-compatible checks do not run Unity.

Fresh full-pipeline build copies of the same test avatar differ only in the experimental-UV checkbox. Both complete with AAO enabled, identical 16 skinned renderers, 33 material slots, 29 PhysBones, 178,304 sampled mesh vertices and 247 remaining BlendShapes. Texture2D dependency payload is **36,311,104 bytes (34.63 MiB) off** and **35,612,040 bytes (33.96 MiB) on**, compared with **73,185,820 bytes (69.80 MiB)** in the earlier 0.7.0-beta.1 diagnostic using the same avatar configuration. The revised output rotates six textures losslessly across two materials, retains incompatible/failed-quality materials, and produces no generated-texture Mip Streaming warning. The source-memory/dirty snapshot is unchanged immediately after these two builds. This metric is full-mip format payload across Texture2D dependencies, not driver overhead, live VRChat residency or compressed avatar-download size; no new VRCA upload/download test is claimed.

Actual Git installation of the 70-file product passes both Conservative and Aggressive baseline/protected comparisons: 16 renderer pairs, 35 geometry/pose/shape samples, 33 material slots, 2,108 source dependency files and zero missing bindings per mode. Maximum position errors are 1.1921e-7 m / 2.3958e-7 m, with maximum normal error 1.2014e-7. Both protected outputs retain compressed formats and the same 35,612,040-byte Texture2D payload; no new RGBA32 dependency is present. Nonblank 512-square previews remain visually consistent, with 59 of 262,144 pixels differing by more than three channel steps and mean maximum-channel difference below 0.01; this is not pixel identity or full animation/client acceptance.

Real Git/SDK Play enters and exits with zero observed errors, 392 generated object names, 53 Human bones and no remaining configuration markers. All 50 examined material textures meet the mip-streaming check; the Editor readback shader is absent from avatar dependencies. All 21 material notices/fingerprints match the final SDK report. That report confirms six lossless compressed rotations and explicitly retains a BC7 material that exceeds the quality threshold. Installed Git content matches source after Windows newline normalization, with all GUIDs retained.

The post-test protected-source scan covers **26,903 files, including 14,241 metas**, with zero changed, missing or added files. Manifest/lock installation changes and explicitly owned test/generated caches are separate. The source scene remains at its initial dirty state; all 858 source GameObject snapshots and the component configuration match. Three VRC rotation constraints update internal execution-group cache indices in Editor memory, which are recorded separately rather than described as byte-identical source memory. Saved project-setting bytes match their initial whole-file SHA without restoration in this run; native SDK setting caches are a separate state and must not be forced into the saved file. The withdrawn 0.7.0-beta.1 Release remains a draft; main promotion is a separate explicit user decision.

## 0.7.0-beta.1 shader-profile development

On Unity 2022.3.22f1 / Windows D3D11 / Linear color space, the candidate passes **48 shader checks**, in addition to the **59 existing processing, 30 enhancement and 15 diagnostic checks**. The shader suite requires stock lilToon 2.4.1, Poiyomi Toon 10.0.27, NonToon 0.3.0 and UnlitWF 2.14.0. NonToon is a preservation test, not a successful conversion claim.

Six lilToon base/outline/transparent/cutout variants retain GPU output within three 8-bit channel steps with nontrivial scale/offset, color/shadow/alpha/normal/matcap masks and unchanged mesh UVs. Poiyomi main textures and UnlitWF second-layer textures pass the same render comparison. A white-texture control must visibly change each selected layer, preventing blank/unexercised render passes. lilToon and Poiyomi also pass unreadable DXT5 integration cases. Readback tests exercise every mip of RGBA32, DXT1, DXT5 and BC7 in sRGB and linear storage with at most one 8-bit step of round-trip difference. This is equivalence to GPU-decoded samples within the stated tolerance, not lossless recovery of compressed source files.

Rejection tests cover renderer/shared exclusions, unsafe property blocks, animated coordinates, MirrorOnce, scrolling, parallax, active textured emission masks, Poiyomi locking and stochastic sampling. Additional cases cover independent particles/animation, copied shader identity, build-only alias normalization, color curves, material-slot swaps, reviewed absent stock-lilToon uniforms, color/bookkeeping versus indexed-UV property blocks, light/Unlit controls and scalar features at three sampled settings, and unchanged shadow-LUT pixels. Unsafe scalar curves that can change coordinate/compile branches are retained, including an initially disabled emission with a textured mask. Owned fully resident streaming copies pass every mip comparison, while unowned streaming inputs remain untouched; source PNG/meta bytes, importer flags and residency requests are checked separately. Source materials, UV arrays, original property blocks and texture references are checked separately. NonToon conversion remains disabled until a meaningful generated-variant render fixture passes.

Unity-independent update/name assertions remain **60/28**, with **8** archive regressions. All three archive formats read back the complete **66-file** package including the Editor-only mip shader and stable GUIDs. Private model assets, downloaded shader packages, raw reports and generated builds are excluded from the repository and release payload.

Fresh Milfy baseline/protected build pairs pass in both modes: 16 renderer pairs, 35 geometry samples, 33 material slots, 2,108 source dependency files and zero missing bindings. Maximum position deltas are 1.1921e-7 m (Conservative) and 2.3958e-7 m (Aggressive); source-memory and dependency checks pass. Each mode transforms four lilToon materials and nine GPU-decoded textures across three meshes, generating 47,535,436 bytes (45.33 MiB) of texture payload. The paired build/check/preview work takes approximately 103/93 seconds in this test environment, not a standalone obfuscator benchmark.

Both 512-square frontal previews are nonblank and visually consistent. In each pair, 108 of 262,144 pixels differ by more than three 8-bit channel steps, with a maximum difference of 170 and mean maximum-channel difference below 0.015. These images are not pixel-identical and cover only one view; they do not establish complete animated-material or VRChat client equivalence. The shader fixtures above provide the tighter isolated sampling assertions. Full client acceptance remains with the user.

The actual Git-installed product passes SDK Play enter/exit with zero recorded errors, 392 generated object names, 53 Human bones and zero remaining configuration markers. All 21 reachable materials carry matching notices/fingerprints; the Editor-only mip helper is absent from avatar dependencies. The 66 installed files match the product commit after Git checkout newline conversion, with every GUID identical. A ThryEditor automatic-lighting first-use dialog temporarily blocked MCP; it was dismissed by the user before the observed successful Play cycle, without a plugin workaround or third-party source change.

The post-import baseline protects **26,903 files, including 14,241 metas**. After build/Play checks, all protected hashes and the path set match, with no changed, missing or unexpectedly added files. Source component serialization, 858-object hierarchy count and its initial unsaved-scene flag are retained. One SDK-induced settings byte is restored only after reproducing the unique full-file baseline SHA-256. The starting native BlendShape-clamp cache was false while the saved baseline was true; after Git package reload the cache follows the unchanged saved value. The original saved file is authoritative, rather than forcing that stale native cache back into it. Intentional package manifest/lock updates and owned validation/NDMF/VRCFury generated output are classified separately. User-imported shader packages are retained.

## 0.6.0 stable promotion

The stable candidate changes only README wording, release documentation and the package version relative to the tested `0.6.0-beta.3` product commit `5146934`. All C# sources, assembly definitions and GUID metadata are identical; the Editor, model and Play results below remain explicitly beta.3 evidence rather than newly repeated stable-version runs. The six Chinese/English/Japanese README notice descriptions now specify refusal to assist in reverse-engineering the model.

The final beta.3 Git installation also passed actual SDK Play enter/exit with zero observed errors, 392 generated names, 53 Human bones and zero configuration markers. All 21 reachable material notices/fingerprints matched the final SDK report. The final protected-file enumeration and hashes retained all 23,013 files, including 12,154 metas, with no changes, missing files or unexpected additions. The saved source scene and original component fields were preserved; its unsaved-scene flag was retained. Build-induced settings changes were reconciled to the exact fresh starting saved-file SHA. An initial native iOS API value disagreed with that starting file; restoring the original file and allowing normal reload retained the original saved value instead of forcing the stale native cache.

Stable preparation passes 60 actual update-policy assertions, 28 naming/notice assertions, eight archive regressions and exact readback of all 60 product files in three package formats. The six README notice descriptions, 19 relative documentation links, short trilingual release notes and unchanged processing/GUID content are checked. No Unity Editor connection or duplicate unitypackage installation is needed for this documentation/version-only change. Full VRChat client, network/device and extraction-resistance evaluations remain outside the verified scope.

## 0.6.0-beta.3 diagnostics and bone palettes

The candidate passes **59 existing processing checks, 30 enhancement checks and 15 diagnostic/palette checks**, with zero failures. New checks sample several skin poses, shared-palette remapping, manual/shared preservation, incomplete palettes, resource-renaming independence, Conservative/Aggressive shader contracts, transparent/cutout UV GPU equivalence, metadata traversal including unsaved materials, invalid fingerprint paths, identity-free presets and exact duration-placeholder classification. Update-policy/name checks remain 60/28 passes; eight Python archive regressions and all three archive formats pass exact readback of 60 product files.

The actual comparison service builds fresh baseline/protected pairs in both modes. Each candidate run checks 2,108 saved source dependency files, 16 skinned-renderer pairs, 35 geometry samples and 33 material-slot pairs, with zero unresolved animation/mask bindings, missing material/texture references or reported differences. Maximum avatar-local position deltas are 1.1921e-7 m (Conservative) and 2.3958e-7 m (Aggressive); maximum normal deltas are 0 and 1.2014e-7. Source component/hierarchy serialization and dependency hashes remain unchanged. The 174 counted duration placeholders are exact Avatar Optimizer terminal constant curves, not unresolved functional targets.

The initial live comparison-window route also completes in Aggressive mode, renders nonblank paired previews and retains matching notices/fingerprints on all 19 reachable materials. Aggressive remaps 15 bone palettes / 355 palette entries and permutes 19 meshes. The actual avatar has no eligible UV-rotation or synchronized-merge targets; those transformations are exercised by synthetic fixtures, not claimed as applied to this avatar. Cleanup closes the tool's preview scene and deletes only its unique generated directory. Later layout changes retain the same comparison service and keep the coverage warning visible.

These checks sample initial/arm/retained-shape geometry and validate references; they do not run every Animator transition, physics interaction, networked state, external device or VRChat client. Selected dependency hashing is narrower than the separate complete protected-project integrity audit. No content watermark is shipped in this release.

The isolated Inspector fixture passes 51 checks across Chinese/English/Japanese, including mouse focus, continuous edits and multiple-Inspector synchronization. All three report locales construct correctly with the live report data. The local integrity pass verifies 23,013 protected files (12,154 metas), with none changed, missing or unexpectedly added. The saved source scene is unchanged. One build-induced settings byte is restored only after the candidate reproduces the exact fresh starting file SHA; native settings retain their initial values.

## 0.6.0-beta.2 nested declarations

The local candidate passes **59 processing checks and 30 enhancement checks**, plus 60 update-policy assertions, 28 naming/notice assertions, eight Python archive regressions and exact readback of all 48 files in three archive formats. New coverage checks notice-only and fingerprint-only containers, both metadata options off, both protection modes with object renaming on/off, normal Transform-only nodes, readable leaves, audit exclusions and unchanged original objects/settings. Existing material-tag/source isolation checks continue to pass.

Local SDK Play enters and returns to Edit with zero observed errors, 392 generated object names, 53 Human bones and zero markers. All five configured readable leaves (three notices, holder and fingerprint) share one generated direct-child container; none is an immediate child of the avatar. The container has identity local transforms, only Transform components and no hide/save flags. All 20 material notices/fingerprints still match the late SDK report. Immediately after each Local/Git Play test, the original source component's serialized settings match the fresh starting snapshot and its 858-object hierarchy is retained.

The actual Git beta revision `17d0be1` passes the same SDK Play enter/exit and nested-metadata checks with zero errors. Its generated container and fingerprint differ from the local build. [Package CI and publication](https://github.com/AKaNecoo/AvatarBuildObfuscator/actions/runs/39525765908) passed; all five release assets match their digests and all three archive payloads match the 48 Git-source files. The installed Git files match that content, with 38 text files differing only by Windows Git CRLF/LF conversion. The managed beta channel is retained; main stays at 0.5.1.

A fresh 40,527-file starting enumeration and 40,599-file final enumeration leave **23,013 protected source files, including 12,154 metas, all unchanged**, with none missing or unexpectedly added. Owned validation output, NDMF/VRCFury generated caches (including their directory metas) and intentional manifest/lock changes are classified separately. Protected files are hashed and their path set is checked again after hashing, without silently skipping a missing source file. The source scene was not saved. One SDK graphics-setting byte was restored only after its candidate reproduced the complete original settings-file SHA-256; initial native settings were retained.

Only the declaration-parenting logic and version changed in the installed product. The username Inspector, existing hierarchy renaming/remapping and geometry/texture processors are unchanged; beta.1 pose/texture and input results below remain that version's evidence. No duplicate unitypackage installation or full pose/input rerun is required for this scoped change.

## 0.6.0-beta.1 processing and metadata (historical)

The local development package passes **59 existing processing checks, 28 enhancement checks and 51 Inspector input checks**. Enhancements cover randomized and reproducible builds; exact-zero pruning and copied-curve removal; native/manual/proxy preservation; synchronized-shape geometry and rejection cases; independent material metadata; metadata opt-outs; SDK-stage audit refresh; and UV raw-mip/GPU sampling with Repeat/Clamp/Mirror wrapping. Unsupported shaders, unreadable textures, material animation/sharing, MirrorOnce and renderer property blocks are exercised as preservation cases. The Unity-independent checks pass **60 update-policy and 28 naming/notice assertions**; eight Python archive regressions and all three archive formats pass exact-source readback for 48 package files.

Two fresh baseline/Conservative/Aggressive build sets pass, with the experimental switches off and then on. Each set retains 53 Human bones and has zero missing animation/shape/mask/parameter targets and zero native errors. Current avatar counts are 405/409/409 objects, 0/356/359 renamed objects and 288/284/247 retained shapes. Four appended empty metadata objects explain the object-count increase. After saving only new comparison scenes and reopening them, each set passes **13 paired clips, 39 poses, 18 material groups and 102 sampled texture pairs**: zero differing sampled pixels, maximum position/normal/bone differences of 3.9991e-7/2.2302e-7/8.9407e-8. The two experimental transformations have **zero eligible targets on this avatar**; controlled fixtures exercise actual transformations. No lilToon UV conversion, generic atlas or general facial-shape encryption is claimed.

Real SDK Play with the local package enters and returns to Edit with zero observed errors, 391 generated object names, 53 Human bones and zero remaining markers. The output contains all three hierarchy notices; 20 materials carry the notice and the same build identifier as the hierarchy and the final `sdk-preprocess` report. This verifies metadata persistence through SDK preprocessing, not through a downloaded/uploaded VRCA. A declaration is neither authenticated ownership nor a guarantee of AI refusal.

A freshly enumerated 36,543-file baseline covers Assets, Packages and ProjectSettings. Excluding the explicitly owned validation directory, NDMF/VRCFury generated caches and the intentional manifest/lock installation changes leaves **23,013 protected files, including 12,154 metas**. A final fresh scan after Git installation and Play finds all 23,013 identical, zero missing and no new files outside those named generated/validation directories. SDK testing changed one iOS graphics-setting byte; guarded restorations reproduced the complete original ProjectSettings SHA-256 and retained the initial native settings. The original scene was not saved and its file hash matches. The 28,610-record memory check retains all existing source-component fields; ten new settings account for its schema difference. Eighteen NadeVision and ten SDK-proxy clips have non-dirty memory serialization differences while their files are unchanged. These are recorded separately and do not establish that every source object's memory bytes are identical.

The published Git beta revision `d5d06a7` also passes all **51 Inspector input checks** and the real SDK Play enter/exit check with zero errors, 391 generated object names, 53 Human bones and zero markers. All three notices and 20 material tags match the final SDK report; the fresh identifier differs from the local run. The five release assets match their published digests, and all three archives contain the exact 48 Git-source file payloads. The installed Git package has the same 48 files and source content; Windows Git converted CRLF/LF in 38 text files, which is recorded separately from exact archive-byte verification. The component GUID/settings and managed beta channel are retained. Both experiments default off, and this larger feature update stays on beta while main retains 0.5.1. [Package CI and publication](https://github.com/AKaNecoo/AvatarBuildObfuscator/actions/runs/39468730576) passed; hosted CI does not run Unity.

## 0.5.1 documentation follow-up

All six component-add instructions (three standalone guides and three bundled languages) explicitly include Hierarchy, Inspector and Add Component. Nineteen relative links pass, release notes contain three short bullets per language, and all three archives pass validation and source readback. C# sources, assembly definitions and metas are unchanged from 0.5.0; no extra Unity/runtime run is claimed.

## 0.5.0 license and documentation checks

The 0.5.0 release changes licensing, documentation and distribution validation. All 26 C# files differ from 0.4.0 only by SPDX/copyright comments; all assembly definitions and metas are unchanged. The root/package GPLv3 text and GPL-3.0-only metadata match. Earlier MIT releases retain their original terms.

Local checks pass 60 assertions against the actual update policy, 15 name-generator assertions, eight archive/validation regressions (including license identifier/text rejection), and three reproducible archive formats with exact-source readback. Three-language README structure, UTF-8/code fences and relative links were checked. No Unity project changes or runtime reruns were needed; the Editor/model/Play results below remain the 0.4.0 checkpoint evidence.

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

Both the local and published Git distributions passed native SDK/NDMF Play with the actual long username: all four entry/exit events, one obfuscated avatar, 400 generated objects, 53 Humanoid bones, zero remaining markers and zero errors. Both observers were finished and the Editor returned to Edit. The local run also checked all 279 temporary audio paths at 232 characters; their in-memory names retained the full 161-character generated name. The actual username/mode/language were retained and no source-scene save was performed. Actual VRChat client upload and device behavior are outside this result. Main promotion was on hold at that checkpoint; the owner subsequently restored release authorization after the 0.4.0 checks.

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

Native SDK/NDMF Play with the local 0.4.0 package passed all four entry/exit events: one obfuscated avatar, 400 generated object names, 53 Humanoid bones, zero residual markers and zero Error/Assert/Exception messages. The observer was finished and the Editor returned to Edit. A live Animator probe distinguished all four chosen code points and found no parameter-hash collisions across the eight inspected controllers. These are Editor checks, not an actual VRChat upload/client/device or extraction-resistance acceptance test.

[Package CI 39391572616](https://github.com/AKaNecoo/AvatarBuildObfuscator/actions/runs/39391572616) passed and published the [0.4.0 prerelease](https://github.com/AKaNecoo/AvatarBuildObfuscator/releases/tag/v0.4.0). The connected Editor was switched to Git beta and verified as version 0.4.0, exact product commit `dec5d7ed70a06dd4fc9d33998558ff548c1a1bc2`. Final Git SDK/NDMF Play passed with the same four events, one avatar, 400 generated names, 53 Humanoid bones, zero markers and zero errors; the observer was finished. No unitypackage was imported for this feature-only update, and full input/model tests were not duplicated by installation format.

The final scan compared all 21,635 protected files with SHA-256 values freshly read before this iteration. Exactly 21,630 matched and none were missing. The other five were the VPM manifest and four lilToon files (package metadata, two changelogs and openlit_core.hlsl), changed by an operator-confirmed lilToon 2.4.0-to-2.4.1 update during testing. Those changes were preserved. The saved source scene, model/texture/material/animation files, metas and complete ProjectSettings file matched the fresh baseline. SDK/AAO native settings were reconciled to that verified baseline, the actual component fields were retained, and the source scene was not saved. The Editor was left in Edit mode with one Git installation, automatic installation off and no pending installer.

The owner restored main-release authorization after these checks. Version 0.4.0 is the stable-promotion candidate with the same tested product bytes; only release documentation changes during promotion. Main advances for completed, verified releases with no unresolved blocking defect, rather than following every beta commit. Installation-specific regressions are required when those flows change, not for an unchanged package payload.
