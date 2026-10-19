# Validation — 0.1.0

Validated on 2026-10-19 in a connected Windows Unity Editor. This records Editor/build evidence, not a completed VRChat client or device acceptance test.

## Environment

- Unity 2022.3.22f1, Windows, Direct3D 11
- VRChat Avatars SDK 3.10.5
- NDMF 1.14.8
- Modular Avatar 1.19.0-alpha.0
- Avatar Optimizer 1.9.20
- VRCFury 1.1434.0
- Existing custom lilToon-based materials retained their shaders

## Synthetic Editor checks

**26 passed, 0 failed.** Each case captures Unity Error / Assert / Exception logs and fails if any occur during the assertion body.

Checks cover Chinese prefix handling; bake weights 0/35/100 with before/after `SkinnedMeshRenderer.BakeMesh` geometry and normal comparisons; dynamic animation curves; MMD behavior in both modes; SDK visemes and eyelid index remapping; child Animator relative roots; one controller shared by two Animator roots; persistent controllers and nested BlendTrees with forced saves and byte comparison; synced motion/behaviour overrides; AnimatorOverrideController; constant curves; manual preservation/exclusion; mesh swaps; special frames and extrapolated weights; proxy identities; unknown components; asset-name renaming disabled; cyclic expression menus; unreadable-texture identity retention; and readable RGBA32/DXT1/DXT5/BC7 GPU pixel comparisons.

`Tests~/Editor/GuardValidation.cs` is an opt-in harness. To reproduce, copy `Tests~/Editor` and `Tests~/Runtime` into separate Editor/Runtime folders under a test project's Assets directory, retain their asmdefs, and call `GuardValidation.RunAll(outputJsonPath)` from an Editor script. Its synthetic fixtures are written only under `Assets/__KiineGuardValidation_20261019/Fixtures`.

## Complete avatar builds

Three fresh NDMF builds of one existing avatar were compared. The avatar already had AAO, so the baseline includes its existing optimization. The added component did not change AAO settings.

| Metric | Baseline | Conservative | Aggressive |
| --- | ---: | ---: | ---: |
| Objects | 414 | 414 | 414 |
| Renderers | 37 | 37 | 37 |
| BlendShapes | 290 | 286 | 249 |
| Renamed retained BlendShapes | 0 | 246 | 249 |
| Materials | 18 | 18 | 18 |
| Referenced material textures | 53 | 53 | 53 |
| Controllers | 8 | 8 | 8 |
| Clips | 827 | 827 | 827 |
| Renamed mesh instances | 0 | 25 | 25 |
| Renamed materials | 0 | 18 | 18 |
| Renamed material textures | 0 | 35 | 35 |
| Renamed clips | 0 | 787 | 787 |
| Missing actual animation paths | 0 | 0 | 0 |
| Missing BlendShape bindings | 0 | 0 | 0 |
| Humanoid bones | 53 | 53 | 53 |
| AvatarGuard components in output | 0 | 0 | 0 |
| Unity Error/Assert/Exception logs during build/render | 0 | 0 | 0 |
| Build + inspection + render time, seconds | 14.28 | 14.64 | 14.07 |

AAO inserts 87 deliberate `$AvatarOptimizerClipLengthDummy$` bindings in every build. They are counted separately, not treated as actual missing targets. Runtime parameter names are not rewritten by this package; upstream tools can generate fresh internal parameter names per build.

At 512×768, a single static rendered frame differed from baseline at 3 pixels for Conservative and 4 pixels for Aggressive out of 393,216 pixels. Mean absolute channel differences were 0.000145 and 0.000764 on a 0–255 scale. This is a static appearance check, not proof of every animation, expression, runtime integration or viewing angle.

The final comparison scene was saved, closed and reopened. All three outputs retained the expected shape counts, 53 humanoid bones, and zero missing actual paths or shape bindings after reload.

## Source integrity and packaging

A SHA-256 manifest was taken before deployment for **21,666 existing files** under Assets, Packages and ProjectSettings. After verification, **0 original files changed and 0 were missing**. Source files, importers, GUIDs, controller sources and the original scene were included. Library, Temp, Logs and UserSettings were not part of this source-file comparison. Private manifests and avatar data are excluded from this repository.

Unity exports only `Assets/KiineAvatarGuard` with `ExportPackageOptions.Recurse`, without dependency inclusion. `Tools/verify_release.py` compares exported asset bytes and `.meta` files against the package source and rejects paths outside that folder. No avatar meshes, textures, scenes, generated build assets, account credentials or test fixtures are included in the release.

## Issues caught before release

Early development runs found an override-controller key remapping issue, native errors from instantiating imported unreadable textures, and native strong-pointer assertions from instantiating NDMF temporary animation graphs. The release uses explicit override reconstruction, skips unreadable/Crunch textures, and creates animation/ScriptableObject copies with `CopySerialized` followed by explicit reference remapping. A fixture also incorrectly used a Boolean as a BlendTree float parameter; that test fixture was corrected. Final checks capture native errors as failures.

## Not verified

- Actual VRChat upload, networking, menu use, face tracking and client runtime.
- Standalone Quest/Android or iOS device behavior.
- Every third-party runtime component, special world expression driver, mesh format or Unity/SDK version.
- Confidentiality against extraction: this package does not provide it.

External tools may use shape names that do not appear in local animations. Use preserve/exclusion settings, and validate a copy before relying on Aggressive mode for those integrations.
