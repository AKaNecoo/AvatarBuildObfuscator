# Avatar Build Obfuscator

[中文](README.md) · [日本語](README.ja.md)

Add **Avatar Tools / Build Obfuscator** to the root GameObject containing `VRCAvatarDescriptor`. NDMF runs it on the build copy, after Modular Avatar, VRCFury, TexTransTool and Avatar Optimizer when present. Each user enters their own username (Unicode supported). It derives an opaque `o_…` naming sequence; resource names do not include the literal username. New components start with an empty username.

**Original names remaining in Unity's Hierarchy / Scene / Project after a build are expected.** Both modes process only the NDMF build copy. Check the `[Avatar Build Obfuscator]` counts and skip reasons in the Console, and inspect the build output. Aggressive mode mainly expands processing of MMD compatibility shapes; it also preserves bone, hierarchy and runtime parameter names.

## Install

Use a Unity 2022.3 VRChat Avatar project with the Avatar SDK and [NDMF](https://github.com/bdunderscore/ndmf) already installed through VCC. Installing this tool through Git does not install those VPM dependencies for you. AAO is optional and its configuration is not changed.

### Install with Git (recommended)

Install [Git](https://git-scm.com/) first. On Windows, Unity must be able to find Git through `PATH`.

1. Open **Window → Package Manager**.
2. Choose **＋ → Add package from git URL…**.
3. Paste this complete URL, click **Add**, and wait for compilation:

```text
https://github.com/AKaNecoo/AvatarBuildObfuscator.git?path=/Packages/dev.avatar-tools.build-obfuscator#main
```

**Enter this URL once. To update later:** select **Avatar Build Obfuscator** in **Package Manager** and click **Update** to fetch new commits from `main`. You do not need to enter the URL again.

Unity locks the installed commit; it does not fetch updates every time the project opens. Keep `?path=…` because the package lives in a repository subfolder. `#main` selects the branch used for manual updates. See Unity's [Git installation guide](https://docs.unity3d.com/2022.3/Documentation/Manual/upm-ui-giturl.html) and [locked Git dependencies](https://docs.unity3d.com/2022.3/Documentation/Manual/upm-git.html#git-locks).

### Beta and stable updates

`beta` receives ongoing changes after the relevant validation. `main` is reserved for stable versions and does not advance with every beta update. Beta versions may have undiscovered issues. To follow beta updates, install with:

```text
https://github.com/AKaNecoo/AvatarBuildObfuscator.git?path=/Packages/dev.avatar-tools.build-obfuscator#beta
```

To switch a Git installation to beta, enter this URL once using **Add package from git URL…**. This changes the branch reference for the same package. Later, select the package and click **Update** to follow `beta`. A `#main` installation stays on stable; add the `#main` URL again to switch back.

### Import a Unity package

Alternatively, import a `.unitypackage` from [Releases](https://github.com/AKaNecoo/AvatarBuildObfuscator/releases). Update this installation by importing a newer Unity package, rather than using the Git update button.

**Install only one copy.** Before switching from a Unity-package installation to Git, remove the plugin files under `Assets/AvatarBuildObfuscator`. Keep your avatar, scenes and other assets. After installing the Git package, check the existing component and username settings; duplicate installations cause assembly conflicts.

### Add the component

Add **Avatar Tools → Build Obfuscator** to the root containing `VRCAvatarDescriptor`, enter your own username, choose a mode and build normally.

To pin a release instead, replace `#main` with a published tag such as `#v0.2.0`. A pinned tag does not follow updates to `main`.

## Modes

- **Conservative**, the default: copies and renames supported asset objects; bakes ordinary unanimated blendshapes at their current weight; preserves Japanese/Chinese and common MMD compatibility shapes.
- **Aggressive**: also allows unused MMD compatibility shapes to be baked and remaining compatibility shapes to be renamed. External worlds or tools expecting those names may stop driving them.
- Both modes retain shapes used by animation curves (including constant curves), SDK visemes and eyelids. Animation bindings, viseme names and eyelid indices are updated together.
- Both modes skip uncertain shape processing: unknown drivers, animated mesh swaps, ambiguous paths, or explicit renderer exclusions. Nonstandard frames or extrapolated weights are not baked.
- Enter externally required shape names in the preserve list, or exclude a renderer. Configure upstream optimizers separately if those shapes must survive their processing too.

## Scope and limits

Supported asset copies include meshes, materials, readable non-Crunch Texture2D, clips, controllers, override controllers, blend trees, masks, Avatar objects and VRC expression menu/parameter assets. Bone and hierarchy names, runtime parameter strings, state names, visible menu labels, shaders and SDK proxy clips retain compatible names. Unreadable textures (Read/Write disabled), Crunch textures and unsupported resource types keep their original resources and names; importer settings are never changed.

Only single-frame shapes with a frame weight of 100 and a current weight within 0–100 are baked. Source files, importers and `.meta` GUIDs are not changed. Save your component configuration in the normal scene/prefab workflow.

**This is obfuscation and shape pruning, not encryption. Textures and retained dynamic blendshapes remain extractable from uploaded content.** No OSC, passwords, custom shader, network requests or automatic avatar upload is involved. The final baked appearance remains part of the mesh geometry.

See [TESTING.md](TESTING.md), [ARCHITECTURE.md](ARCHITECTURE.md) and [MIT License](LICENSE). The opt-in `Tests~` tools and private avatar fixtures are not included in the Unity package.
