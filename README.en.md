# Avatar Build Obfuscator

The username keeps its label-left/input-right row and uses a native bound text field for focus, IME and Undo. Search **Avatar Build Obfuscator** in Add Component and choose **Avatar Tools / Avatar Build Obfuscator**.

[中文](README.md) · [日本語](README.ja.md)

Add **Avatar Tools / Avatar Build Obfuscator** to the root containing `VRCAvatarDescriptor`, enter your own username and build with NDMF. **0.3.2** uses **`username_IlIl…`** names: a visible Unicode prefix and 128 ASCII capital `I` / lowercase `l` characters. The complete normalized username also seeds the deterministic sequence. New components leave it empty.

There is **no OSC, password or runtime shader decryption**. Uploaded textures, final geometry and required dynamic blendshapes remain extractable. Obfuscation does not provide confidentiality.

## Install and update

Requires Unity **2022.3**, Avatar SDK **3.7.0+** and [NDMF](https://github.com/bdunderscore/ndmf) **1.14.8+**, installed through VCC. Git installation does not resolve VPM dependencies. AAO, MA, VRCFury and TTT are optional.

In **Window → Package Manager → ＋ → Add package from git URL…**, enter the stable URL once:

```text
https://github.com/AKaNecoo/AvatarBuildObfuscator.git?path=/Packages/dev.avatar-tools.build-obfuscator#main
```

Use `#beta` for previews. Alternatively import the `.unitypackage` from [Releases](https://github.com/AKaNecoo/AvatarBuildObfuscator/releases). Keep only one installation; remove only the other plugin installation before switching, retaining avatars. Script GUIDs remain stable; check component references after migration.

The component's **Version and updates** section shows version/channel/status and **Check now / Update now / Changelog**. Automatic checks default on with a six-hour cache. Automatic installation is opt-in per project. It runs after 60 seconds with Unity idle in the background and the SDK control panel closed, outside Play/build/compilation/import. A failed commit is not retried automatically. Offline checks do not block the plugin.

Git updates install the channel's exact successful Package CI commit through Package Manager and retain the channel for future updates. Asset-package installations download the CI-published `.unitypackage`, verify GitHub's SHA-256 digest, and import it. The temporary `Library/AvatarBuildObfuscatorUpdates/pending.unitypackage` is deleted after success, failure or cancellation; cleanup resumes after reload. Updates replace plugin code and recompile/reload; they do not run avatar processing or modify model assets.

Preferences live only in this project's `UserSettings/AvatarBuildObfuscatorUpdates.asset`, outside avatar data. No username/model is sent. Manual tag/commit pins, other repositories, relocated Assets copies and local `file:` development packages are not overwritten. Asset packages initially follow main for stable versions or beta for prereleases, then retain the channel. Re-adding the original `#main`/`#beta` Git URL or using Package Manager remains available. Never keep Git and Assets copies together.

## CI and releases

[Package CI](https://github.com/AKaNecoo/AvatarBuildObfuscator/actions/workflows/package-ci.yml) validates package layout, GUIDs and update policy on main/beta pushes and PRs. It builds reproducible UPM `.tgz`, ZIP, `.unitypackage` and checksums and reads every archive back against source. Only this repository's main/beta pushes publish releases; PRs cannot publish. Main is stable, beta is prerelease. Released product content is immutable for each version; bump the version for product changes.

Hosted CI compiles/tests the independent update-policy code and verifies packaging. It does **not** substitute for Unity Editor or actual-avatar acceptance. Maintainers run Unity compilation, NDMF, input and Play checks in an authorized project before promoting main; see [TESTING.md](TESTING.md). CI does not upload avatars, private logs or projects.

## Use

The **语言 / Language / 言語** selector supports Simplified Chinese (default), English and Japanese. The choice is stored per component and changes Inspector labels, help and tooltips only. It does not alter usernames, modes or build output.

Play/SDK builds support NDMF's default `Packages/nadena.dev.ndmf/__Generated` output tree as well as manual output under Assets.

Enter a username, select a mode and build normally. A maximum 32-character filename-safe prefix is displayed; unsafe characters become underscores. The full Trim/NFC-normalized input still seeds HMAC. Identical input and call order produce identical names; optimizer traversal can change between builds.

Inspect **`[Avatar Build Obfuscator]`** Console counts and skip reasons, plus the actual build output. **Original names remaining in Hierarchy / Scene / Project are expected.** Both modes operate on the NDMF copy.

## Modes

Both aim for broad coverage. Conservative requires recognized, consistently remapped dependencies; Aggressive permits unknown name dependencies. Independent switches control asset names, shapes, hierarchy, bones, parameters, animator internals, blendshape index order, sibling order and vertex order.

- Both copy supported assets, rewrite hierarchy and mask paths, rebuild supported Humanoid avatars, rename custom parameters and animator internals, and preserve essential dynamic shapes.
- Conservative protects uncertain drivers, external string paths and MMD compatibility shapes. Its vertex permutation is restricted to audited built-in shaders.
- Aggressive also processes uncertain drivers and compatibility shape names, and permits vertex permutation with custom shaders. External integrations may break.
- Explicit shape, renderer, hierarchy/subtree and parameter preservation is available; parameter patterns support a trailing `*`.

Blendshape permutation remaps current weights and SDK eyelid indices while preserving every retained frame. Sibling permutation changes neither parenting nor local transforms. UI, duplicate sibling names, Generic skeletons and manual preservation retain their order; Humanoid bone identities are rechecked. Both order switches work independently of renaming.

Imported copies use one asset-import batch rather than an import per file. Copy progress supports cancellation and always releases import suspension on errors. Unity native imports/shader compilation may not stop immediately.

## Coverage and contracts

Imported textures, including unreadable/Crunch data, and audio use independent generated files/importer copies without changing source settings or compressed data. Supported graphs include clips, controllers, override controllers, synced overrides, blend trees, masks and expression assets.

Parameter remapping covers definitions, Animator curves, conditions, state fields, blend trees, menu main/sub-parameters, drivers, PhysBone prefixes/generated suffixes and Contacts. Visible menu labels remain readable. VRChat built-ins, known external face-tracking/OSC inputs and true SDK proxy identities retain their contracts.

Humanoid names and internal parent names are rebuilt and checked for bone mapping and scale. Unsupported Generic/invalid avatars, ambiguous paths and failed rebuilds preserve affected hierarchy. Vertex permutation keeps raw vertex streams, all channels, topology, variable bone weights and morph frames together; no runtime reversal is needed. Cloth, additional vertex streams and unreadable meshes retain order.

Readable `.shader` sources can be copied and recompiled under a new name while retaining property/keyword/pass interfaces. Built-in/mobile-whitelisted/container-generated shaders and NadeVision's fixed shader identity are preserved. Shader property protocol obfuscation is not implemented. Unknown-resource fallbacks are reported.

Only ordinary single-frame shapes with frame weight 100 and current weight 0–100 are baked; constant animation curves count as dynamic usage. Mesh swaps and explicit exclusions retain their protected shape structures.

Source scenes, assets, importers and original `.meta` GUIDs are never overwritten by the processor. Adding the component alone does not run it. No automatic avatar upload occurs. Conservative is not a universal compatibility guarantee for every unrecognized world, device or extension.

See [TESTING.md](TESTING.md), [ARCHITECTURE.md](ARCHITECTURE.md), [CHANGELOG.md](CHANGELOG.md) and [MIT License](LICENSE). Opt-in `Tests~` fixtures and private avatar evidence are excluded from the installed package. Editor results do not replace client/device acceptance.

Existing component GUIDs and migration metadata are retained. Version 0.3 beta restores visible username prefixes by request and enables new switches by default; review settings before building. Naming ideas were informed by [AvatarObfuscator](https://github.com/cocokoishi/AvatarObfuscator); this implementation uses ASCII `I/l` and independently maintains its asset isolation and reference mapping.

Temporary texture/audio disk names are shortened to fit the full native-import path budget. Built object names retain the full username plus 128 I/l characters. If the project/output directory leaves insufficient room even for a compact copy, the resource keeps its original reference with a report; shorten the project path or avatar root name to enable copying. Source files and importers stay unchanged.
