# Architecture — 0.3 beta

## Build lifecycle and ownership

The root component implements `IEditorOnly`. The NDMF Resolving pass captures settings and removes markers from the build clone. Optimizing runs after the configured avatar construction/optimization plugins. Adding a component never starts an import, changes source assets, or uploads an avatar. AAO is optional and is never reconfigured by this package.

The processor accepts a nonpersistent build copy and rejects prefab assets. New resources go to the NDMF asset saver. Imported textures/audio are independently copied into its generated folder before opening the serialization scope; `StartAssetEditing`/`StopAssetEditing` group copies, followed by one synchronous refresh. Loads occur after that batch completes. Import suspension and progress UI are released in `finally`, including cancellation. The path guard accepts Assets descendants and the exact `Packages/nadena.dev.ndmf/__Generated` tree used by normal Play/SDK callbacks. It rejects other package source folders and dot-segment traversal. The earlier Assets-only gate passed manual tests but broke real SDK preprocessing; regression coverage now includes both roots. Source importers, compression and Read/Write settings are not changed. Unsupported embedded/built-in assets retain their source identity.

## Names, parameters and hierarchy

A Trim/NFC-normalized username seeds a SHA-256 key. HMAC-SHA256 counter output generates 128 ASCII I/l characters; the filename-safe visible username prefix is limited to 32 UTF-16 characters. Full input still affects the seed. The username is not a password. Names and permutations are deterministic for identical call order, which upstream optimizers may change between builds.

Float and object animation bindings and AvatarMask paths are rewritten relative to each Animator root before names are applied. Shared graphs are cached per relative root. Ambiguous paths, explicit preservation, Generic/invalid avatars and recognized fixed contracts protect affected hierarchy. Conservative also protects unknown drivers and external path strings; Aggressive permits unknown dependencies.

Humanoid rebuilding uses a separate hierarchy probe. Public skeleton names and Unity's private `SkeletonBone.parentName` are remapped; native AvatarBuilder output must preserve bone Transform identities and scale. Failed probes protect the affected hierarchy before any path rewrite. Rebuilt avatars are new assets.

Custom parameter mappings are shared across Animator definitions/curves, state fields, transition conditions, BlendTrees, expression menus/parameters, parameter-driver sources/targets, PhysBone prefixes/generated suffixes, and Contacts. VRChat built-ins and recognized external face-tracking inputs keep protocol names. Animator layer/state/machine/transition names can change; execution order, layer indices and transition priority remain intact. Menu labels remain user-facing text.

## Animation graph isolation and proxy identity

Animation and ScriptableObject copies are created as fresh objects plus `CopySerialized`; a reference walk then copies states, trees, behaviours, synced overrides and cycles. The copy is registered before descending. AnimatorOverrideController overrides use GetOverrides/ApplyOverrides after assigning the copied base to preserve Unity's native override-key table. Large numeric resource buffers are not traversed as generic graphs.

SDK proxy motions must retain their canonical identity, not a temporary upstream clone. NDMF ObjectRegistry resolves that original and records replacement ownership for new graph resources. A temporary proxy that upstream processing later destroys would otherwise become a null state motion. The package requires NDMF 1.14.8+ for the APIs used here.

## BlendShape data and order

Usage includes descriptor layers, nested Animators, direct references, constant curves, SDK visemes/jaw and eyelid indices. Mesh swaps, explicit renderer exclusions and ambiguous renderer paths preserve shape structures. Conservative unknown drivers skip shape editing. Conservative also preserves world-driven compatibility names.

Only ordinary unreferenced single-frame shapes with frame weight 100 and current weight 0–100 are baked into copied positions, normals and tangents. Special frames and extrapolation are retained. Retained shapes may be permuted independently of renaming; their internal frame order/deltas stay intact. Current weights and native eyelid indices use an old-to-new index mapping; animation/viseme names use the name mapping. No mapping is attached to the upload.

## Sibling order

A username-derived Fisher–Yates permutation changes sibling indices, not parenting or local transforms. Duplicate-name groups, UI/Canvas/RectTransform ordering, Generic skeletons and manual subtree/ancestor exclusions retain order. Conservative skips this operation for unknown drivers; Aggressive permits that uncertainty. After applying order, Human Avatars are rebound and bone Transform identities checked. A mismatch restores all sibling orders and reports the fallback.

## Vertex streams, textures and shaders

Vertex permutation moves every raw vertex stream and its exact format, remaps submesh indices and variable-length bone weights, and permutes all retained morph frames consistently. It does not change triangle order, material slots, bounds, or intended geometry. Cloth, extra vertex streams and unreadable meshes retain order. Conservative limits permutation to audited built-in shaders; Aggressive permits custom shaders that may use vertex IDs.

Readable shader source can be copied/recompiled under a generated name with relative includes resolved. Property, keyword and pass contracts remain. Built-in/mobile-whitelisted/container shaders and NadeVision's fixed shader identity are preserved. NadeVision actually searches by shader name; bypassing its validation would conceal a functional break. Shader compilation can take time; creation has progress and timing diagnostics. Native compilation is not guaranteed to respond to cancellation immediately.

## Validation and security boundary

Opt-in `Tests~` instrumentation captures renderer identities after upstream optimization, before obfuscation. Private test reports store final renderer locations so pose/texture comparison does not assume sibling traversal order. This instrumentation is not part of the installed package, and no source-to-output map is stored as a referenced avatar asset.

Obfuscation removes descriptive names and unused morph definitions. Textures, required dynamic morphs and final geometry remain extractable. There is no runtime decoder or cryptographic confidentiality. Editor build/static/pose evidence is distinct from actual VRChat client/device acceptance.

## Inspector language

The editor-only InspectorLocalization table provides Chinese, English and Japanese labels/help/tooltips. `inspectorLanguage` is serialized per root component and defaults to Simplified Chinese; GuardSettings does not read it. The marker is removed from build output, so neither locale state nor translation data is uploaded. This editor UI does not require Localization/Addressables dependencies. SerializedProperty handles edits; label width is restored after drawing.

## Inspector input

The standard Inspector uses CreateInspectorGUI with a native UI Toolkit TextField bound to the existing userName property. Header and other options keep the existing IMGUI interface in separate containers. The username keeps its single-row layout. Label width follows actual Inspector geometry and reserves input space in narrow views. Language tracking updates the existing label/tooltip without replacing the input element; normal repaints do not rebind it. Binding owns text edits and Undo. Localized label updates use INotifyValueChanged<string>.SetValueWithoutNotify: changing a child Label with its normal text setter can bubble a string ChangeEvent into the username binding and write a stale field value back. Property tracking also synchronizes externally changed usernames without dispatching edit events or rebuilding the field. Legacy OnInspectorGUI hosts retain the original serialized-field interface. The display/menu name is Avatar Build Obfuscator; component type, assembly, serialized fields and script GUID are unchanged.
