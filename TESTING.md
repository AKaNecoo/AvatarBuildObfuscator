# Testing and validation

This page covers **0.8.4-beta.1**, which adds Inspector branding, consistent controls, grouped options and the VCL manager name. Avatar-processing code is unchanged from **0.8.3**. The UI checks below were run for this update; processing, SDK and client results were carried forward from the explicitly stated earlier versions and were not rerun as new avatar/client acceptance for this UI release. Results apply to the stated versions and environments, rather than every avatar or VRChat world.

## Current results

| Check | Result and scope |
| --- | --- |
| Inspector UI | In Unity 2022.3.22f1, the centered wordmark and title are displayed with standard IMGUI controls. The two MMD options are adjacent. Real Windows clicks, mixed English/Chinese text entry and Ctrl+Z were checked on a disposable test object. The source avatar component settings remain unchanged. This does not exhaustively cover every input method or editor theme. |
| Prior MMD client result | The avatar creator reported working MMD expressions with **0.8.1-beta.1** and **Obfuscate MMD expressions** disabled. That version kept the whole name table; this report does **not** validate selective Aggressive naming or the new MMD object-name option. |
| Prior MMD client comparison | On **0.8.2-beta.3** in Aggressive mode, the avatar creator reported missing MMD expressions with **Rename MMD objects** enabled and **Obfuscate MMD expressions** disabled. Turning only **Rename MMD objects** off, with other settings unchanged, restored expressions. This identifies an object-name/path compatibility problem in that tested avatar/world combination. Selective shape naming worked there with object paths retained; this does not establish compatibility with every MMD world. |
| Generated material references | Eight new saved-material cases cover both modes, readable/unreadable generated masks and resource naming on/off. Pre-fix processing shared with 0.8.2 fails six cases by clearing valid references; the fix passes all eight. Checks retain shared reflection/MatCap bindings, empty slots, pixels, texture transforms, values, keywords, pass state, render queue and source inputs. A separate Texture Compressor integration probe reproduces a lost main texture before the fix and retains it afterward. |
| Original-project visual retest | After updating to **0.8.3-beta.1**, the avatar creator reported that the originally affected outfit's reflection/MatCap appearance was correct. This is targeted visual confirmation for that avatar and configuration, not exhaustive shader, world or avatar-feature coverage. |
| Unity processing | In Unity 2022.3.22f1, local and actual Git-installed candidate sources pass 77 synthetic cases. Coverage includes the eight material cases, pinned MMD/manual path indices, unrelated sibling movement, complete shape frame/index/weight retention, viseme/eyelid and animation references, original-Avatar fallback, interacting nested-Avatar rejection and preset isolation. Each case checks for native errors. |
| Full avatar build comparison | The local candidate passes 16 renderer pairs, 35 geometry/pose/shape samples, 33 material slots and 2,108 dependency-file checks in Aggressive mode, with zero missing bindings or observed native errors. Source memory and dependency files remain unchanged. The two MMD opt-in options are off; experimental UV/texture rotation remains enabled on this test avatar. |
| Earlier original-Avatar fallback probe | On **0.8.2-beta.3**, a separate native full-avatar probe tested the MMD object-name option both off and on. A non-bone body node listed in the imported skeleton description could be renamed while retaining the original Avatar and all Human-bone Transform identities. This verifies that case, not support for every rig that fails rebuilding. |
| SDK callback chain | The actual Git-installed candidate passes the SDK callback chain on a full avatar copy with an added generated-texture fixture. Reflection/MatCap masks and the main texture remain present after final serialization; shared mask identity, enabled effects and mask pixels are preserved. The output material and mask are saved, 53 Human bones remain, no editor marker or native error remains, and original source memory is unchanged. This is separate from a VRChat upload, Play-mode entry or client test. |
| VPM SDK build on a second avatar | With VPM-installed **0.8.2-beta.3** and **Rename MMD objects** enabled, the `sdk-preprocess` audit records 343 object renames out of 1,199 objects and 127 shuffled sibling groups. The previously named `Body` and `Body_b` meshes have generated output paths, matched by their retained-shape name sets. The shape count stays at 303: 251 names change and 52 MMD-compatible names remain. The original-Avatar fallback accepts non-bone MMD object renaming. This is a read-only report review, not a separate geometry comparison or VRChat client test on that avatar. |
| Source files | Comparison checks find unchanged source objects and dependency files. Broader source-asset and saved-scene hashes are checked against a fresh baseline; package installation metadata and NDMF-generated cache output are tracked separately. The source scene is not saved during these checks. |
| Package checks | Local .NET checks pass 60 update-policy, 28 naming, 166 texture-block and 25 VPM-policy assertions. All 33 Python tests pass. TGZ, ZIP and unitypackage archives each read back all 77 product files, including the editor-only wordmark and its metadata. Unrelated texture files remain rejected. |

The [package CI workflow](https://github.com/AKaNecoo/AvatarBuildObfuscator/actions/workflows/package-ci.yml) reruns the Unity-independent checks and publishes archives only after they pass. It does not run the Unity Editor or a VRChat client in CI.

## Run the checks

From the repository root, use Python 3.12 and .NET 10:

```sh
dotnet run --project Tests~/UpdatePolicy/UpdatePolicy.Tests.csproj --configuration Release
python -m unittest discover -s Tests~ -p 'test_*.py' -v
python Tools/ci_package.py
```

`ci_package.py` validates the package identity, license, GUIDs and metadata, then builds and reads back reproducible TGZ, ZIP and unitypackage archives. Its output is placed under the local `Artifacts/` directory.

Unity Editor checks are opt-in. In a disposable Unity 2022.3 project with the VRChat Avatar SDK and NDMF installed, copy `Tests~/Editor` and `Tests~/Runtime` with their assembly definitions under `Assets`. An Editor script can call `AvatarTools.Obfuscation.Validation.GuardValidation.RunAll(outputJsonPath)` to create and run synthetic fixtures. Full-avatar comparison uses `FullAvatarValidation` with an avatar you are authorized to test; it creates temporary validation assets. Review the output and remove the fixture after testing. These Editor checks are separate from the commands above and require a Unity installation.

For a fresh Assets installation in a disposable project, `python Tools/sync_to_test_project.py <project>` copies the package with its existing GUIDs. It refuses an existing target directory or `.meta`, an existing UPM installation and linked project directories. Use the package manager to update existing installations.

## Remaining limits

- Keep both **Obfuscate MMD expressions** and **Rename MMD objects** off when MMD expressions are required. The 0.8.2-beta.3 client comparison confirmed failure with object renaming enabled and recovery after disabling it. That comparison covers one avatar/world combination; other MMD worlds and custom external names/paths still need testing. Conservative retains the whole MMD name table, but changing object paths can still break externally owned animations.
- The installation guide includes VCL as a VPM manager. The configured link targets and generated repository page are checked; native repository registration, installation and updating in VCL have not been separately exercised.
- The generated-texture fix has Editor/SDK checks and the original-project visual feedback described above. Build comparisons sample selected geometry, poses, shapes, materials and references. They do not exhaustively verify every Animator transition, expression menu, PhysBone, network synchronization or face-tracking interaction, or every outfit/shader combination.
- SDK-preprocess audits describe references at that callback, including references from editor-only components. They are not a final VRCA inventory: retained-resource counts alone do not establish which resources survive final serialization.
- Quest/iOS behavior and every shader or custom integration have not been tested. Aggressive mode and experimental UV/texture rotation need avatar-specific checks.
- Obfuscation is not encryption or a guarantee against extraction. Texture pixels, final geometry and required dynamic BlendShapes can still be recovered from a build.
