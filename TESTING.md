# Testing and validation

This page summarizes validation for **0.8.2-beta.2**. The Unity checks cover its selective MMD naming and original-Avatar fallback. The VRChat client report below predates these changes and is listed separately. Results apply to the stated versions and environments, rather than every avatar or VRChat world.

## Current results

| Check | Result and scope |
| --- | --- |
| Prior MMD client result | The avatar creator reported working MMD expressions with **0.8.1-beta.1** and **Obfuscate MMD expressions** disabled. That version kept the whole name table; this report does **not** validate the new selective Aggressive behavior in 0.8.2-beta.2. |
| Unity processing | In Unity 2022.3.22f1, 64 synthetic processing cases pass with no native errors. Coverage includes MMD names and paths, complete frame/index/weight retention, viseme and eyelid references, renamed animation bindings, the original-Avatar fallback and rejection of interacting nested Avatars. |
| Full avatar build comparison | The candidate passes 16 renderer pairs, 35 geometry/pose/shape samples, 33 material slots and 2,108 source dependency checks, with zero missing bindings. The baseline and obfuscated `Body` retain 278 shapes at the same path: 40 compatible names keep their indices, and 238 other names change. |
| Original-Avatar fallback | A separate full-avatar in-memory probe keeps the original Avatar and Human bone identities while retaining 321 of 858 transforms as naming candidates. This verifies the fallback mechanism, not support for every rig that fails rebuilding. |
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

- The reported VRChat result covers 0.8.1-beta.1 with MMD expression obfuscation **off**. The new selective Aggressive behavior, other MMD worlds, custom external names/paths and the option-on behavior still need client testing. Conservative retains the whole MMD name table.
- Build comparisons sample selected geometry, poses, shapes, materials and references. They do not exhaustively verify every Animator transition, expression menu, PhysBone, network synchronization or face-tracking interaction.
- Quest/iOS behavior and every shader or custom integration have not been tested. Aggressive mode and experimental UV/texture rotation need avatar-specific checks.
- Obfuscation is not encryption or a guarantee against extraction. Texture pixels, final geometry and required dynamic BlendShapes can still be recovered from a build.
