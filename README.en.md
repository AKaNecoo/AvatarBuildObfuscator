# Avatar Build Obfuscator (ABO)

[中文](README.md) · [日本語](README.ja.md)

Build-time obfuscation for VRChat avatars. Add the component and NDMF processes the build copy without overwriting source assets. No runtime decoder is required.

## Features

- Rename resources, objects, bones, parameters, Animator internals and dynamic BlendShapes using your username plus `Ì/Í/Î/Ï` symbols.
- Bake and remove supported fixed/unused BlendShapes while retaining required dynamic shapes.
- Independently shuffle BlendShape, sibling GameObject, mesh vertex and skin bone-palette order, updating related references.
- Randomize each build, prune zero-delta shapes and write a local residual-data audit.
- Embed multilingual authorization notices and build fingerprints. Experimental UV/texture rotation and synchronized-shape merging default off.

Experimental texture rotation covers selected lilToon 2.4.1, Poiyomi 10.0.27 and UnlitWF 2.14.0 configurations, plus the reviewed NonToon 0.3.0 default single-texture setup; see the [compatibility matrix](ARCHITECTURE.md#exact-pruning-and-experimental-transforms).

## Install

Requires **Unity 2022.3, Avatar SDK 3.7.0+ and NDMF 1.14.8+**. Install the SDK and [NDMF](https://github.com/bdunderscore/ndmf) through ALCOM, VCC or VRCLL first.

**[ALCOM](https://vrc-get.anatawa12.com/zh-hans/alcom/) / [VCC](https://vrchat.com/home/download) / [VRCLL](https://start.gamemode1.com/pages/vrclearnlauncher/) (recommended):** Open the [package repository page](https://akanecoo.github.io/AvatarBuildObfuscator/), add the repository using its button or paste the listed URL into your manager, then install **Avatar Build Obfuscator** in your project's package list. Enable prereleases for beta versions. The page also links the official NDMF repository.

**Git:** Open **Window → Package Manager → ＋ → Add package from git URL…** and enter:

```text
https://github.com/AKaNecoo/AvatarBuildObfuscator.git?path=/Packages/dev.avatar-tools.build-obfuscator#main
```

Use `#beta` for test releases. Alternatively, import a `.unitypackage` from [Releases](https://github.com/AKaNecoo/AvatarBuildObfuscator/releases). Choose one installation method to avoid duplicate copies.

To migrate to VPM, first remove a Git installation in Package Manager, then install through ALCOM, VCC or VRCLL. VPM can migrate legacy `.unitypackage` folders by GUID. Keep your own model assets outside the plugin folder; retain the component settings on your avatar.

## Use

1. Select the avatar root in **Hierarchy**. At the bottom of **Inspector**, click **Add Component**, search for **Avatar Build Obfuscator** and choose **Avatar Tools → Avatar Build Obfuscator**.
2. Enter your username and choose a mode. New components default to an existing avatar ID; edit it freely, or enter a username if no ID exists.
3. Build normally with the VRChat SDK. Inspect the build copy; original Hierarchy and Project names stay unchanged.

Up to 32 characters of your username appear as a visible prefix in build output names. With the default value, this includes part of your avatar ID; change it before building if you do not want to publish that value.

Use **Show latest build report** to search retained items and inspect notices/fingerprints. **Test build and comparison** creates temporary copies for sampled checks and cleans them when the window closes; it does not replace full VRChat testing. Per-renderer lists preserve selected operations, and presets store only operation switches and mode.

The component supports Simplified Chinese, English and Japanese. Options can be switched independently. Declared rights holders and contacts are published with the notice; it does not authenticate ownership or guarantee that AI will refuse to assist in reverse-engineering the model.

| Mode | Behavior |
| --- | --- |
| Conservative (default) | Prioritizes functionality and preserves dependencies it cannot verify. |
| Aggressive | Processes a wider range, accepting unknown compatibility risks that may affect external integrations. |

**Subtree mode overrides** let body, clothing and accessories use separate modes. The nearest configured ancestor wins. Shared resources and global parameters/controllers use conservative priority; ambiguous optimizer merge provenance falls back to conservative and is reported.

## Update

Update VPM installations in **ALCOM / VCC / VRCLL**. Git and `.unitypackage` installations use the component's version/update panel; Git also supports Package Manager. Automatic checks default on; automatic installation defaults off. Package-based updates remove temporary downloads afterward.

Git `#main` and `#beta` update independently and do not switch channels automatically. To move from beta to stable, add the same Git URL ending in `#main` again in Package Manager; existing component settings are retained.

## Notes

Obfuscation may increase the avatar's download size; the amount depends on the model and enabled options. Experimental texture rotation may increase it further. Check the SDK build statistics before uploading.

For MMD expressions, keep both **Obfuscate MMD expressions** and **Rename MMD objects** off. Object renaming changes paths such as `Body`; a client test confirmed missing expressions in an MMD world even with shape names, data and indices retained.

MMD compatibility protection is on by default. On matching meshes, Conservative retains all shapes; Aggressive keeps matching names and **every shape's data and index**, while renaming other shapes. Rules include Japanese/Chinese characters and names such as `Blink` and `a`. Nonstandard worlds may require additional names, so test Aggressive in your worlds. Enable the above options only if you do not need the corresponding MMD compatibility.

Obfuscation does not prevent extraction: texture pixels, final geometry and required dynamic BlendShapes remain extractable. Check Aggressive mode in a test build first. Console messages prefixed with `[Avatar Build Obfuscator]` report processing counts and preservation reasons.

## More

[Detailed rules and implementation](ARCHITECTURE.md) · [Validation scope](TESTING.md) · [Changelog](CHANGELOG.md) · [GPL-3.0 license](LICENSE)

From 0.5.0, licensed under GPL-3.0-only. Earlier published versions retain their MIT license; dependencies keep their own terms.

Inspired by name-obfuscation ideas in [cocokoishi/AvatarObfuscator](https://github.com/cocokoishi/AvatarObfuscator), with independently maintained build-copy handling, reference remapping and compatibility rules.
