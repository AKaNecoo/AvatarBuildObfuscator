# Avatar Build Obfuscator

[中文](README.md) · [日本語](README.ja.md)

Build-time obfuscation for VRChat avatars. Add the component and NDMF processes the build copy without overwriting source assets. No runtime decoder is required.

## Features

- Rename resources, objects, bones, parameters, Animator internals and dynamic BlendShapes using your username plus `Ì/Í/Î/Ï` symbols.
- Bake and remove supported fixed/unused BlendShapes while retaining required dynamic shapes.
- Independently shuffle BlendShape, sibling GameObject, mesh vertex and skin bone-palette order, updating related references.
- Randomize each build, prune zero-delta shapes and write a local residual-data audit.
- Embed multilingual authorization notices and build fingerprints. Experimental UV/texture rotation and synchronized-shape merging default off.

## Install

Requires **Unity 2022.3, Avatar SDK 3.7.0+ and NDMF 1.14.8+**. Install the SDK and [NDMF](https://github.com/bdunderscore/ndmf) through VCC first.

Open **Window → Package Manager → ＋ → Add package from git URL…** and enter:

```text
https://github.com/AKaNecoo/AvatarBuildObfuscator.git?path=/Packages/dev.avatar-tools.build-obfuscator#main
```

Use `#beta` for test releases. Alternatively, import a `.unitypackage` from [Releases](https://github.com/AKaNecoo/AvatarBuildObfuscator/releases). Choose one installation method to avoid duplicate copies.

## Use

1. Select the avatar root in **Hierarchy**. At the bottom of **Inspector**, click **Add Component**, search for **Avatar Build Obfuscator** and choose **Avatar Tools → Avatar Build Obfuscator**.
2. Enter your username and choose a mode. New components default to an existing avatar ID; edit it freely, or enter a username if no ID exists.
3. Build normally with the VRChat SDK. Inspect the build copy; original Hierarchy and Project names stay unchanged.

Use **Show latest build report** to search retained items and inspect notices/fingerprints. **Test build and comparison** creates temporary copies for sampled checks and cleans them when the window closes; it does not replace full VRChat testing. Per-renderer lists preserve selected operations, and presets store only operation switches and mode.

The component supports Simplified Chinese, English and Japanese. Options can be switched independently. Declared rights holders and contacts are published with the notice; it does not authenticate ownership or guarantee AI refusal.

| Mode | Behavior |
| --- | --- |
| Conservative (default) | Prioritizes functionality and preserves dependencies it cannot verify. |
| Aggressive | Processes a wider range, accepting unknown compatibility risks that may affect external integrations. |

## Update

Use the component's version/update panel to check or install updates. Git installations can also use Package Manager. Automatic checks default on; automatic installation defaults off. Package-based updates remove temporary downloads afterward.

## Notes

Obfuscation does not prevent extraction: texture pixels, final geometry and required dynamic BlendShapes remain extractable. Check Aggressive mode in a test build first. Console messages prefixed with `[Avatar Build Obfuscator]` report processing counts and preservation reasons.

## More

[Detailed rules and implementation](ARCHITECTURE.md) · [Validation scope](TESTING.md) · [Changelog](CHANGELOG.md) · [GPL-3.0 license](LICENSE)

From 0.5.0, licensed under GPL-3.0-only. Earlier published versions retain their MIT license; dependencies keep their own terms.

Inspired by name-obfuscation ideas in [cocokoishi/AvatarObfuscator](https://github.com/cocokoishi/AvatarObfuscator), with independently maintained build-copy handling, reference remapping and compatibility rules.
