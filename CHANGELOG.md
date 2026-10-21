# Changelog

## 0.6.0-beta.2 — 2026-10-21

- Group readable hierarchy notices, holder and fingerprint under one independently generated container; retain complete material tags.
- Preserve original object transforms and metadata opt-outs; no source-scene changes or runtime components are required.

## 0.6.0-beta.1 — 2026-10-21

- Add per-build randomization, local residual audits with a late SDK check, and exact zero-delta shape/curve pruning.
- Embed optional Chinese/English/Japanese authorization declarations and removable build fingerprints without claiming identity verification or guaranteed AI refusal.
- Add opt-in lossless UV/texture rotation and synchronized dynamic-shape merging within a restricted compatibility matrix; both default off.

## 0.5.1 — 2026-10-20

- Clarify the Hierarchy → Inspector → Add Component entry point in all README languages and the bundled guide.
- Keep release notes as short changelog entries. No obfuscation behavior changes.

## 0.5.0 — 2026-10-20

- License this release under GPL-3.0-only, with matching root/package license text, package metadata and source identifiers. Earlier MIT-licensed releases keep their original terms.
- Shorten Chinese, English, Japanese and bundled READMEs to installation/use guides; keep implementation and compatibility details in ARCHITECTURE.md.
- Update release validation for the GPL identifier and matching license text. Processing, Inspector and updater behavior remain unchanged from 0.4.0.

## 0.4.0 — 2026-10-20

- Use the four Unicode symbols Ì/Í/Î/Ï (U+00CC–00CF) for generated suffixes: 64 symbols carry the existing 128 bits of naming information. Retain visible user prefixes and source component fields/GUIDs.
- Derive shuffle seeds directly from the username-derived byte stream, independently of visible glyphs; update name recognition and safe temporary-path budgets for the new alphabet.
- Default newly added components to the avatar's PipelineManager blueprint ID when available. Keep manual values editable; normal Inspector edits never refill a cleared field.
- Update Chinese/English/Japanese text and add symbol/seed vectors, invalid-name checks, avatar-ID defaults and Unicode audio-path regressions. This remains build-copy obfuscation with no runtime decryption.

## 0.3.2 — 2026-10-20

- Fix Play/SDK preprocessing failures when a long username makes copied audio paths exceed the native importer's Windows path limit.
- Budget temporary texture/audio file paths, compact their disk names when necessary, and retain the full username plus 128 I/l characters on the built objects.
- Preserve the source reference with an explicit report if the output directory leaves insufficient room for a safe copy; keep source files/importers unchanged.
- Add long-path audio sample/byte integrity and overlong-directory fallback regressions.

## 0.3.1 — 2026-10-20

- Refresh legacy package metadata after manual imports that do not trigger assembly reload.
- Recheck the current version before importing a downloaded archive, preventing a stale pending download from downgrading a newer manual installation.
- Add a real-Editor cache-invalidation regression and reload-safe opt-in update-route observer.


## 0.3.0 — 2026-10-20

- Stable release of the verified Git/asset-package updater and reproducible CI distribution.
- Keep automatic installation opt-in, preserve existing component GUIDs/settings and remove temporary installer downloads.


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
