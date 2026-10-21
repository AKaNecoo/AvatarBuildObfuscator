# Architecture

## Licensing and distribution

Release 0.5.0 and later use GPL-3.0-only. Root and packaged LICENSE files contain the same unmodified GPLv3 text, package.json declares that SPDX identifier, and project-owned C#/Python sources carry matching identifiers and copyright notices. Earlier published MIT releases retain their original grants; this change does not rewrite their tags or archives.

The distributed package contains plugin source and its metadata, not Unity, VRChat SDK or NDMF binaries. These separately installed dependencies retain their own terms; the GPL declaration does not relicense them. Applying the tool to avatar data does not by itself license that data under GPL. No additional linking exception is declared.

## Build lifecycle and ownership

The root component implements `IEditorOnly`. The NDMF Resolving pass captures settings and removes markers from the build clone. Optimizing runs after the configured avatar construction/optimization plugins. Adding a component never starts an import, changes source assets, or uploads an avatar. AAO is optional and is never reconfigured by this package.

The processor accepts a nonpersistent build copy and rejects prefab assets. New resources go to the NDMF asset saver. Imported textures/audio are independently copied into its generated folder before opening the serialization scope; `StartAssetEditing`/`StopAssetEditing` group copies, followed by one synchronous refresh. Loads occur after that batch completes. Import suspension and progress UI are released in `finally`, including cancellation. The path guard accepts Assets descendants and the exact `Packages/nadena.dev.ndmf/__Generated` tree used by normal Play/SDK callbacks. It rejects other package source folders and dot-segment traversal. The earlier Assets-only gate passed manual tests but broke real SDK preprocessing; regression coverage now includes both roots. Source importers, compression and Read/Write settings are not changed. Unsupported embedded/built-in assets retain their source identity.

## Names, parameters and hierarchy

A Trim/NFC-normalized username and, by default, a fresh in-memory build salt seed a SHA-256 key in the names/v3 domain. Disabling per-build randomization preserves the deterministic names/v2 stream. HMAC-SHA256 counter output generates 64 symbols from U+00CC/U+00CD/U+00CE/U+00CF (Ì/Í/Î/Ï), carrying 128 bits of naming information. The filename-safe visible username prefix is limited to 32 UTF-16 characters; unsupported filename characters become underscores. Full input still affects the seed. Shuffle seeds come directly from the HMAC bytes, independently of visible symbols. The username is not a password. Reproduction requires identical settings and call order, which upstream optimizers may change between builds.

New components use the root PipelineManager's existing avtr_… blueprint ID as the default username. The editor-only Reset callback reads its serialized field without adding an editor SDK assembly dependency to the runtime assembly. It preserves already-filled values; ordinary editing, clearing, language changes and Inspector recreation never auto-fill the field. No avatar ID means an empty field requiring manual entry.

Temporary imported texture/audio filenames fit a 240-character full-path budget, with eight characters reserved for unique suffixes. The physical name retains at least one prefix character and 32 generated symbols (64 bits); in-memory names retain the full prefix and 64-symbol suffix. If the directory itself leaves insufficient room, the original reference is kept and reported instead of attempting an unsafe native import. Source files and importers remain unchanged.

Float and object animation bindings and AvatarMask paths are rewritten relative to each Animator root before names are applied. Shared graphs are cached per relative root. Ambiguous paths, explicit preservation, Generic/invalid avatars and recognized fixed contracts protect affected hierarchy. Conservative also protects unknown drivers and external path strings; Aggressive permits unknown dependencies.

Humanoid rebuilding uses a separate hierarchy probe. Public skeleton names and Unity's private `SkeletonBone.parentName` are remapped; native AvatarBuilder output must preserve bone Transform identities and scale. Failed probes protect the affected hierarchy before any path rewrite. Rebuilt avatars are new assets.

Custom parameter mappings are shared across Animator definitions/curves, state fields, transition conditions, BlendTrees, expression menus/parameters, parameter-driver sources/targets, PhysBone prefixes/generated suffixes, and Contacts. VRChat built-ins and recognized external face-tracking inputs keep protocol names. Animator layer/state/machine/transition names can change; execution order, layer indices and transition priority remain intact. Menu labels remain user-facing text.

Per-component preservation lists protect BlendShapes, renderers, parameters and object subtrees/ancestors. Parameter entries ending in `*` match a prefix. These settings do not prevent upstream plugins from removing the same data; those plugins need their own preservation configuration. Conservative retains recognized MMD/world-driven compatibility names. Aggressive may rename retained compatibility shapes and remove unused ones. Known contracts that cannot be safely remapped remain protected in both modes.

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

### User-facing local diagnostics

The report window reads only project-local audit JSON, supports history/search and resolves source GlobalObjectIds or asset paths when available. Older reports lack some counters and are identified as such. Metadata inspection traverses serialized references, including inactive renderers and animation-referenced materials; native CollectDependencies alone misses unsaved material references. Fingerprint lookup accepts only 32 lowercase hexadecimal characters and checks the report's embedded ID. These IDs are removable public metadata, not authentication.

The comparison tool creates two independent preview-scene copies and runs the normal NDMF pipeline with and without this component. Renderer identity is captured after upstream optimizers, before this pass; an incompatible capture prevents a successful geometry comparison. It checks bindings, triangle-expanded positions/normals, an arm pose and matching retained shapes, with explicit coverage limits. Individual shape sampling is skipped after synchronized-shape merging, since the merged shape represents multiple driven inputs. Exact Avatar Optimizer duration placeholders are counted separately only when their path/type/property and constant terminal key match the audited contract; ordinary missing paths remain failures. Errors logged during the comparison are reported.

The tool hashes the selected avatar's existing saved dependencies and compares source component serialization, hierarchy indices and scene-dirty state before/after. It never automatically restores source changes. Results stay in Library/AvatarBuildObfuscator/Diagnostics. Its generated assets occupy a unique AvatarBuildObfuscatorDiagnostics_ directory under the NDMF generated tree; closing/disposal cleans only that exact directory and its preview scene. This is sampled Editor evidence, not validation of every Animator transition, physics/network/device or VRChat client behavior.

### Bone palettes and local controls

Palette permutation updates copied mesh bind poses and all weight indices together with every sharing renderer's bones array. Parentage, rootBone and Humanoid mappings remain intact. Unreadable meshes, Cloth, mesh swaps, incomplete palettes and explicit exclusions retain their palette. Conservative also preserves unknown drivers and unaudited shader index contracts; Aggressive permits unknown shader risk. Remapping can require a mesh copy even when asset renaming is off, but non-skinned meshes do not incur that copy solely for this option.

Per-renderer preservation lists independently cover vertex order, bone palettes and UV processing. A shared mesh is retained if any relevant user requests preservation. Presets store mode and a whitelist of operation booleans; usernames, declared ownership/contact/terms, language and object-reference lists are not captured or replaced.

### Per-build randomness and audit

By default the processor generates a 256-bit cryptographic salt in build memory and derives a separate names/v3 stream from the normalized username and that salt. The visible username prefix and four-symbol suffix remain. No salt or name mapping is written back to the component or attached to the avatar. Disabling randomization retains the deterministic names/v2 vectors; the public build identifier is then deterministic too. A fingerprint is an identifier derived in a separate HMAC domain, not an ownership certificate, content hash or tamper-proof signature.

Auditing captures original identities locally before processing and reports retained object/shape names and source-resource references after the NDMF pass. A final-order SDK preprocessing callback refreshes that report after other preprocessing callbacks; manual NDMF builds are explicitly labelled ndmf-pass. This is a pipeline audit, not inspection of a downloaded VRCA. Reports stay in Library/AvatarBuildObfuscator/Reports, retain at most12 historical identifiers plus latest.json, and are never referenced by uploaded assets. Retention reasons and normal SDK/built-in identities are reported rather than silently counted as successful obfuscation.

### Authorization declarations and fingerprints

Optional metadata is written only to owned material override tags and a dedicated empty hierarchy subtree. A username-prefixed generated container is appended under the avatar root; readable declarations, an optional holder and a fingerprint are its children, rather than immediate children of the avatar. This container is independent of object-renaming switches and has only a normal Transform, with no hide/save flags or functional bindings. Disabling both metadata options adds no container. The notice includes Chinese/English/Japanese requests to stop assisting extraction/reverse-engineering/redistribution and first verify the original creator's authorization. Declared holder/contact/additional terms are public data. Empty holder fields specify nobody; no identity, ownership or authorization is verified. The text is an ordinary rights declaration, not an authenticated instruction or a guarantee that an AI will refuse. Nesting reduces immediate hierarchy visibility, not removability or the chance of a tool dropping metadata. Fingerprints can be removed or copied and provide only matching against local records. No custom runtime component, shader decoder or parameter is required.

### Exact pruning and experimental transforms

Zero-delta pruning checks every position/normal/tangent delta in every frame for exact zero and removes matching copied animation curves. Native viseme/eyelid contracts, explicit preservation and unknown drivers are excluded. Proxy-bound shape names also retain identity because SDK proxy clips cannot be rewritten.

Experimental shape merging combines pairs of ordinary single-frame shapes only when their initial weights and complete known animation-curve signatures match. Both deltas are summed into one shape and the redundant curve is removed from copied clips; no runtime decoder is involved. Native contracts, exclusions, unknown drivers, mismatched/missing curves and multiframe structures remain separate. This narrow synchronized-shape reduction is not general facial-shape encryption.

Experimental UV processing rotates UV0 and texture pixels by 180 degrees, compensating static texture offsets. Its audited support matrix is the built-in Unlit/Texture, Unlit/Transparent and Unlit/Transparent Cutout family, readable uncompressed supported formats, Float32 two-component UV0, independent owned meshes/materials, and no material animation, particle mesh or unknown driver. Cloth, additional vertex streams, renderer property blocks and MirrorOnce wrapping are excluded. Every mip is permuted as raw pixel bytes, preserving formats, color space and sampling settings. Unsupported combinations retain their data with a report. This does not provide generic atlasing, lilToon conversion or static shader baking. Both experiments default off and require build-copy validation.

Opt-in `Tests~` instrumentation captures renderer identities after upstream optimization, before obfuscation. Private test reports store final renderer locations so pose/texture comparison does not assume sibling traversal order. This instrumentation is not part of the installed package, and no source-to-output map is stored as a referenced avatar asset.

Obfuscation removes descriptive names and unused morph definitions. Textures, required dynamic morphs and final geometry remain extractable. There is no runtime decoder or cryptographic confidentiality. Editor build/static/pose evidence is distinct from actual VRChat client/device acceptance.

## Inspector language

The editor-only InspectorLocalization table provides Chinese, English and Japanese labels/help/tooltips. `inspectorLanguage` is serialized per root component and defaults to Simplified Chinese; GuardSettings does not read it. The marker is removed from build output, so neither locale state nor translation data is uploaded. This editor UI does not require Localization/Addressables dependencies. SerializedProperty handles edits; label width is restored after drawing.

## Inspector input

The standard Inspector uses CreateInspectorGUI with a native UI Toolkit TextField bound to the existing userName property. Header and other options keep the existing IMGUI interface in separate containers. The username keeps its single-row layout. Label width follows actual Inspector geometry and reserves input space in narrow views. Language tracking updates the existing label/tooltip without replacing the input element; normal repaints do not rebind it. Binding owns text edits and Undo. Localized label updates use INotifyValueChanged<string>.SetValueWithoutNotify: changing a child Label with its normal text setter can bubble a string ChangeEvent into the username binding and write a stale field value back. Property tracking also synchronizes externally changed usernames without dispatching edit events or rebuilding the field. Legacy OnInspectorGUI hosts retain the original serialized-field interface. The display/menu name is Avatar Build Obfuscator; component type, assembly, existing serialized fields and script GUID are retained.


## Package updates and CI

UpdatePolicy is Unity-independent and compiled/tested directly by hosted CI. PackageUpdates uses asynchronous UnityWebRequest polling, checks successful push runs of package-ci.yml for the exact repository/channel, reads metadata at that SHA and only accepts newer SemVer versions. Main refuses prereleases. Git installs pin that verified SHA and persist the managed channel before a possible domain reload; arbitrary manual pins and development installs remain untouched.

Official Assets/AvatarBuildObfuscator installations are recognized by the stable component GUID and package identity. They download the corresponding CI-published release archive, validate the GitHub SHA-256 digest and use AssetDatabase.ImportPackage. Downloaded archives are cached only in the project Library/AvatarBuildObfuscatorUpdates and deleted on completion/failure/cancellation, with post-reload cleanup. Avatar assets are not part of the update. Future releases removing legacy scripts require an explicit reviewed migration; avoid leaving obsolete code in Assets.

UserSettings stores per-project opt-in installation, automatic checks, channel/cache and the last attempted SHA. A six-hour cache and per-SHA failed-attempt suppression prevent retry/compile loops. Automatic installs wait at least60 seconds and require background/idle Editor state; Play/build/compile/import, active text/GUI operations and the SDK control panel block installation. GuardPlugin holds a lease throughout its processing callback. No request or import is performed inside an Inspector repaint or build callback.

Package CI runs on pushes/PRs, compiles the actual update policy and tests archives, builds TGZ/ZIP/unitypackage with exact-source readback and checksums. A separate contents-write job publishes only trusted main/beta pushes and refuses product changes under an existing version. PR jobs have read-only permissions. Hosted CI requires no Unity credentials and makes no claim of running the Unity/Avatar regressions; those remain an explicit maintainer promotion check.
