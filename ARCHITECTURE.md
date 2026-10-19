# Architecture

## Build lifecycle

`AvatarBuildObfuscator` implements the SDK's `IEditorOnly` marker. The Resolving pass copies its settings into NDMF build state and removes the marker from the build copy. Optimizing runs after known avatar construction/optimization plugins. No source-scene callbacks, import hooks or automatic asset migration run when a component is merely added.

AAO is optional. Its public component documentation does not support configuration from scripts, so Avatar Guard does not inject or reconfigure AAO components. Instead it independently analyzes the final avatar and conservatively handles supported static shapes. An existing AAO configuration may already have removed shapes before this pass.

## Shape usage

All root descriptor layers and all nested Animators contribute clips with their respective relative roots. Direct clip/controller references on components and additive-reference clips are included. Any blendshape animation curve, including a constant curve, protects that shape's data. SDK viseme names, jaw shape and eyelid indices also protect data. Unknown runtime components/behaviours disable shape editing. Mesh-swap curves, duplicate paths and explicit exclusions protect the affected renderer.

Normal static shapes are baked into copied base positions, normals and tangents. Retained shape frames are rebuilt without altering their deltas. Current retained weights and SDK eyelid indices are remapped. Bounds, skin weights, topology and other mesh data are preserved through `Object.Instantiate`. Nonstandard frame layouts are not baked. Conservative mode additionally protects a broad set of world-driven compatibility names.

## Isolation and names

Every modified resource is a new object. Animation and ScriptableObject copies are created with a fresh object plus `EditorUtility.CopySerialized`. Instantiating an entire temporary NDMF animation graph can trigger Unity native strong-pointer assertions, so it is not used for those objects. Serialized-reference traversal then deep-copies controller states, transitions, nested trees, behaviours and synced-layer overrides, preserving fields not represented by hand-written controller reconstruction. Cycles are handled by registering the copy before traversing references. Override controllers use `GetOverrides` / `ApplyOverrides` after the copied base is assigned, because assigning a new base can rebuild Unity's native override-key table.

Animation graphs are cached per Animator relative root. A clip used by two nested Animators can require two distinct shape-binding rewrites. Shared non-animation assets use a global cache. Meshes, textures and Avatar objects do not recursively traverse their large numeric buffers. A normalized, manually entered username derives a SHA-256 key; HMAC-SHA256 of a big-endian counter produces each `o_` plus 32-hex-digit name. No literal username is appended. Identical usernames and call order produce identical sequences; upstream optimizer traversal can change between builds, so per-resource names are not promised to stay fixed. Usernames are not passwords or encryption keys. No source-to-output mapping is stored as a referenced uploaded asset.

SDK proxies, shader names, bone names, hierarchy paths, state names and runtime parameter strings retain identity. Texture copies never call `Apply`, GPU readback or importer reconfiguration. Unreadable textures and Crunch textures retain their original resource and name: Unity can log native errors when imported unreadable textures are instantiated, even when synthetic in-memory fixtures appeared to copy successfully. No source asset is saved or marked dirty by the processor. Only NDMF's generated asset saver receives new objects.

## Security boundary

Obfuscation removes descriptive names and unused morph definitions. It provides no cryptographic confidentiality. Retained dynamic shapes and texture pixels are still part of the uploaded asset graph. The final baked face/body geometry is also present. This package makes no anti-extraction guarantee.
