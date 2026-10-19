# Changelog

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
