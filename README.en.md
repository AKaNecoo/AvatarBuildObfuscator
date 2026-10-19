# Avatar Build Obfuscator

[中文](README.md) · [日本語](README.ja.md)

Add **Avatar Tools / Build Obfuscator** to the root GameObject containing `VRCAvatarDescriptor`. NDMF runs it on the build copy, after Modular Avatar, VRCFury, TexTransTool and Avatar Optimizer when present. Each user enters their own username (Unicode supported). It derives an opaque `o_…` naming sequence; resource names do not include the literal username. New components start with an empty username.

## Install

Use a Unity 2022.3 VRChat Avatar project with the Avatar SDK and [NDMF](https://github.com/bdunderscore/ndmf) already installed through VCC. Import the `.unitypackage` from [Releases](https://github.com/AKaNecoo/AvatarBuildObfuscator/releases), add the component to your avatar root, enter your own username, and build normally. Install only one copy of this tool. AAO is optional and its configuration is not changed.

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
