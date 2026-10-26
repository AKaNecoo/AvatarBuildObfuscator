# Testing and validation

This page summarizes validation for **0.8.2-beta.1**. Unity and VRChat results below were obtained for **0.8.1-beta.1 / 0.8.1**. This maintenance release changes editor text, documentation and a test-install helper; avatar processing rules and asset GUIDs are unchanged. Test results apply to the stated versions and environments, rather than every avatar or VRChat world.

## Current results

| Check | Result and scope |
| --- | --- |
| MMD expressions in VRChat | The avatar creator reports that MMD expressions worked after uploading **0.8.1-beta.1** with **Obfuscate MMD expressions** disabled. This is a real client smoke test of the previously failing behavior on that avatar, not a survey of MMD worlds. |
| Unity processing | In Unity 2022.3.22f1, 61 synthetic processing cases pass with no Console errors. With object-order shuffling enabled in both modes, detected MMD renderers retain their BlendShape name/data/index table, `Body` path and relevant sibling positions. An unrelated mesh continues to be processed. |
| Full avatar build comparison | The Git-installed beta build passes 16 renderer pairs, 35 geometry/pose/shape samples, 33 material slots and 2,108 source dependency checks, with zero missing bindings. The baseline and obfuscated `Body` each retain the same 278 post-optimizer BlendShape names and order and the same object path. |
| Source files | A post-build scan found no changed, missing or added files among 26,948 protected project files. The source scene was not saved during these checks. |
| Current maintenance package | Local .NET checks pass 60 update-policy, 28 naming, 166 texture-block and 25 VPM-policy assertions. All 31 Python tests pass, including 11 test-install safety cases. TGZ, ZIP and unitypackage archives each read back all 74 product files. Unity and VRChat tests were not repeated for these text and development-tool changes. |

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

- The reported VRChat result covers the tested avatar with MMD expression obfuscation **off**. Other MMD worlds, custom external animation paths and the option-on behavior are not established by that report.
- Build comparisons sample selected geometry, poses, shapes, materials and references. They do not exhaustively verify every Animator transition, expression menu, PhysBone, network synchronization or face-tracking interaction.
- Quest/iOS behavior and every shader or custom integration have not been tested. Aggressive mode and experimental UV/texture rotation need avatar-specific checks.
- Obfuscation is not encryption or a guarantee against extraction. Texture pixels, final geometry and required dynamic BlendShapes can still be recovered from a build.
