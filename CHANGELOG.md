# Changelog

## 0.3.0-rc.1 — 2026-10-20

- Add Chinese/English/Japanese update controls with project-local preferences; default automatic checks on, automatic installation off. Respect original main/beta channels and manual pins.
- Select exact commits from successful push CI, compare SemVer correctly, avoid busy Editor/build states and repeated failed automatic attempts.
- Support Git and official Assets .unitypackage installations. Verify release archive SHA-256 and delete temporary downloads on completion, failure or cancellation, with reload cleanup.
- Add hosted C# update-policy checks and package validation/reproducible archives. Publish immutable-version release assets only after successful trusted pushes.
- Keep Unity/NDMF/Play/input acceptance distinct from hosted package CI.

## 0.3.0-beta.3 — 2026-10-20

- Replace the standard Inspector username input with a native UI Toolkit TextField bound to the existing serialized property, keeping the label-left/input-right layout. Other options and the legacy OnInspectorGUI path retain their existing interface. Size labels against actual Inspector geometry and reserve input width in narrow views.
- Prevent translated caption ChangeEvents from entering the username binding and restoring stale text; synchronize external property changes without emitting input events.
- Rename the Add Component entry and Inspector heading to Avatar Build Obfuscator; retain component class, assembly, fields and script GUID compatibility.
- Add an opt-in preview-scene input fixture with two bound Inspectors, cross-frame keyboard input, Unicode paste, language changes, Undo/Redo and Inspector recreation.

## 0.3.0-beta.2 — 2026-10-20

- Fix Play/SDK preprocessing: permit NDMF's actual default __Generated tree in addition to Assets output, keeping vendor-package and traversal rejection.
- Add a per-component Simplified Chinese / English / Japanese Inspector selector, including option labels, help and tooltips. Display language does not change build configuration.
- Expand to 52 synthetic checks; cover unreadable/Crunch copies in NDMF's default package path and unsafe-output rejection.
- Add opt-in Play-mode observation across domain reload, checking actual SDK callbacks, obfuscated output and return to Edit Mode.

## 0.3.0-beta.1 — 2026-10-20

- Restore visible per-user Unicode prefixes and 128-character ASCII I/l suffixes.
- Expand both modes to hierarchy/path, supported Humanoid, custom parameter and Animator internal-name remapping.
- Add independent BlendShape index and GameObject sibling permutations, plus coherent vertex-stream permutation without runtime decryption.
- Copy imported textures/audio in one batch, including unreadable/Crunch textures, without changing original importers.
- Support readable shader-source copies while preserving recognized fixed contracts such as NadeVision.
- Preserve canonical SDK proxy identities and synced/override graph references.
- Add cancellation boundaries, phase timing, order-independent build/pose validation and batch-import regression coverage.
- Require NDMF1.14.8+ for the APIs used by this version; retain existing component GUID/type/field migration.
- Keep main stable; beta uses the same Git install URL and Package Manager Update.

## 0.2.0 — 2026-10-19

- Rename the repository, component menu, package ID, assemblies and install folder to neutral product names.
- Derive opaque resource names from each user's manually entered username; remove literal username prefixes and author-specific defaults.
- Preserve existing entered values and component references with serialization/type migration and stable .meta GUIDs.
- Add username-dependent sequence, Unicode, uniqueness and empty-input validation.

## 0.1.0 — 2026-10-19

- Add one editor-only Avatar root component, processed automatically by NDMF.
- Legacy naming used literal Unicode prefixes; replaced by username-derived opaque names in 0.2.0.
- Conservative and Aggressive modes, manual shape preservation and renderer exclusions.
- Bake supported fixed/unused shapes; retain and correctly rebind animated shapes, SDK visemes and eyelids.
- Deep-copy animation graphs, synced overrides and override controllers before rewriting.
- Preserve unsupported/unreadable/Crunch resources and uncertain shape integrations.
- Provide Unity package, standard package-directory ZIP, MIT source and opt-in Editor validation tools.
