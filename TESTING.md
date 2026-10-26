# Testing and validation

This page summarizes validation for **0.8.2-beta.3**. The current Unity checks cover pinned sibling indices and the optional MMD object-name policy. Earlier VPM and VRChat client reports are versioned separately. Results apply to the stated versions and environments, rather than every avatar or VRChat world.

## Current results

| Check | Result and scope |
| --- | --- |
| Prior MMD client result | The avatar creator reported working MMD expressions with **0.8.1-beta.1** and **Obfuscate MMD expressions** disabled. That version kept the whole name table; this report does **not** validate selective Aggressive naming or the new MMD object-name option. |
| Unity processing | In Unity 2022.3.22f1, local and Git-installed candidate sources pass 69 synthetic cases with no observed native errors. Coverage includes pinned MMD/manual path indices, unrelated sibling movement, complete shape frame/index/weight retention, viseme/eyelid and animation references, original-Avatar fallback, interacting nested-Avatar rejection and preset isolation. |
| Full avatar build comparison | With **Rename MMD objects** enabled only on a test copy, local and Git comparisons pass 16 renderer pairs, 35 geometry/pose/shape samples, 33 material slots and 2,106 dependency-file checks, with zero missing bindings. The body object is renamed; its mesh retains 278 shapes, with 40 compatible names retained and 238 others renamed. Saved-scene files are checked separately from these preview-copy dependencies. |
| Original-Avatar fallback | A separate native full-avatar probe tests the MMD object-name option both off and on. A non-bone body node listed in the imported skeleton description can be renamed while retaining the original Avatar and all Human-bone Transform identities. This verifies that case, not support for every rig that fails rebuilding. |
| SDK callback chain | The Git-installed candidate passes SDK preprocessing with MMD object naming enabled on a copy: the body object is renamed, 278 shapes and 53 Human bones remain, no editor marker remains, no native error is observed and source memory is unchanged. The final audit reaches `sdk-preprocess` with zero metadata mismatches. This is separate from a VRChat upload or client test. |
| SDK preprocessing on a second avatar | A build with the VPM-installed 0.8.2-beta.2 reaches the `sdk-preprocess` audit with the original-Avatar fallback accepted after Humanoid rebuilding fails. The report records 335 object renames out of 1,199 objects and 251 shape name changes, with the shape count unchanged at 303. Six MMD-protected renderers retain 52 compatibility names and rename 250 others. This report confirms processing coverage; it does not replace a geometry comparison or VRChat client test on that avatar. |
| Source files | Comparison checks find unchanged source objects and dependency files. Broader source-asset and saved-scene hashes are checked against a fresh baseline; package installation metadata and NDMF-generated cache output are tracked separately. The source scene is not saved during these checks. |
| Package checks | Local .NET checks pass 60 update-policy, 28 naming, 166 texture-block and 25 VPM-policy assertions. All 31 Python tests pass. TGZ, ZIP and unitypackage archives each read back all 74 product files. |

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

- The reported VRChat result covers 0.8.1-beta.1 with MMD expression obfuscation **off**. Selective Aggressive naming, other MMD worlds and custom external names/paths still need client testing. **Rename MMD objects** is off by default; enabling it may affect worlds using fixed object paths even though shape names/data/indices remain protected. Conservative retains the whole MMD name table.
- Build comparisons sample selected geometry, poses, shapes, materials and references. They do not exhaustively verify every Animator transition, expression menu, PhysBone, network synchronization or face-tracking interaction.
- SDK-preprocess audits describe references at that callback, including references from editor-only components. They are not a final VRCA inventory: retained-resource counts alone do not establish which resources survive final serialization.
- Quest/iOS behavior and every shader or custom integration have not been tested. Aggressive mode and experimental UV/texture rotation need avatar-specific checks.
- Obfuscation is not encryption or a guarantee against extraction. Texture pixels, final geometry and required dynamic BlendShapes can still be recovered from a build.
