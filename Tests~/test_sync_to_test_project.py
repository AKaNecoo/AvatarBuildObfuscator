# SPDX-License-Identifier: GPL-3.0-only
# Copyright (c) 2026 Avatar Build Obfuscator contributors

import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("sync_to_test_project", ROOT / "Tools/sync_to_test_project.py")
sync_tool = importlib.util.module_from_spec(spec)
spec.loader.exec_module(sync_tool)


class SyncTests(unittest.TestCase):
    def setUp(self):
        (ROOT / "Artifacts").mkdir(exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(dir=ROOT / "Artifacts")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name).resolve()
        self.project = self.root / "project"
        (self.project / "Assets").mkdir(parents=True)
        (self.project / "Packages").mkdir()
        self.manifest = self.project / "Packages/manifest.json"
        self.manifest.write_text(json.dumps({"dependencies": {}}), encoding="utf-8")
        self.source = self.root / "package"
        (self.source / "Runtime").mkdir(parents=True)
        (self.source / "Runtime/component.cs").write_bytes(b"fixture source\n")
        (self.source / "Runtime/component.cs.meta").write_bytes(b"guid: fixture\n")
        self.target = self.project / "Assets/AvatarBuildObfuscator"

    def snapshot(self, root):
        return {p.relative_to(root).as_posix(): p.read_bytes() for p in root.rglob("*") if p.is_file()}

    def test_fresh_install_retains_source_and_metadata(self):
        original = self.snapshot(self.source)
        manifest = self.manifest.read_bytes()
        self.assertEqual(sync_tool.sync(self.project, self.source), self.target)
        self.assertEqual(self.snapshot(self.target), original)
        self.assertEqual(self.snapshot(self.source), original)
        self.assertEqual(self.manifest.read_bytes(), manifest)

    def test_existing_directory_is_unchanged(self):
        (self.target / "Runtime").mkdir(parents=True)
        (self.target / "Runtime/component.cs").write_bytes(b"existing local edits")
        (self.target / "personal.asset").write_bytes(b"user data")
        before = self.snapshot(self.project)
        with self.assertRaisesRegex(ValueError, "Refusing to overwrite"):
            sync_tool.sync(self.project, self.source)
        self.assertEqual(self.snapshot(self.project), before)

    def test_existing_target_file_is_unchanged(self):
        self.target.write_bytes(b"user file")
        with self.assertRaisesRegex(ValueError, "Refusing to overwrite"):
            sync_tool.sync(self.project, self.source)
        self.assertEqual(self.target.read_bytes(), b"user file")

    def test_orphan_target_meta_is_unchanged(self):
        meta = self.target.with_suffix(".meta")
        meta.write_bytes(b"guid: existing")
        with self.assertRaisesRegex(ValueError, "Refusing to overwrite"):
            sync_tool.sync(self.project, self.source)
        self.assertEqual(meta.read_bytes(), b"guid: existing")
        self.assertFalse(self.target.exists())

    def test_upm_dependency_is_unchanged(self):
        self.manifest.write_text(json.dumps({"dependencies": {sync_tool.NAME: "existing"}}), encoding="utf-8")
        before = self.snapshot(self.project)
        with self.assertRaisesRegex(ValueError, "already uses the UPM package"):
            sync_tool.sync(self.project, self.source)
        self.assertEqual(self.snapshot(self.project), before)

    def test_embedded_package_is_unchanged(self):
        embedded = self.project / "Packages" / sync_tool.NAME
        embedded.mkdir()
        (embedded / "package.json").write_bytes(b"existing package")
        before = self.snapshot(self.project)
        with self.assertRaisesRegex(ValueError, "already uses the UPM package"):
            sync_tool.sync(self.project, self.source)
        self.assertEqual(self.snapshot(self.project), before)

    def test_missing_manifest_does_not_install(self):
        self.manifest.unlink()
        with self.assertRaises(FileNotFoundError):
            sync_tool.sync(self.project, self.source)
        self.assertFalse(self.target.exists())

    def test_missing_assets_does_not_install(self):
        (self.project / "Assets").rmdir()
        with self.assertRaisesRegex(ValueError, "Expected a Unity project"):
            sync_tool.sync(self.project, self.source)
        self.assertFalse((self.project / "Assets").exists())

    def make_link(self, link, target, is_directory=False):
        try:
            link.symlink_to(target, target_is_directory=is_directory)
        except (OSError, NotImplementedError) as error:
            self.skipTest("Symbolic links unavailable: " + str(error))

    def test_dangling_target_link_is_preserved(self):
        missing = self.root / "missing"
        self.make_link(self.target, missing, True)
        with self.assertRaisesRegex(ValueError, "Refusing to overwrite"):
            sync_tool.sync(self.project, self.source)
        self.assertTrue(self.target.is_symlink())
        self.assertFalse(missing.exists())

    def test_assets_link_does_not_write_elsewhere(self):
        other = self.root / "other-assets"
        other.mkdir()
        (self.project / "Assets").rmdir()
        self.make_link(self.project / "Assets", other, True)
        with self.assertRaisesRegex(ValueError, "ordinary Assets and Packages"):
            sync_tool.sync(self.project, self.source)
        self.assertEqual(list(other.iterdir()), [])

    def test_source_link_does_not_install(self):
        source_file = self.root / "outside.txt"
        source_file.write_bytes(b"unrelated")
        self.make_link(self.source / "linked.txt", source_file)
        with self.assertRaisesRegex(ValueError, "must not contain links"):
            sync_tool.sync(self.project, self.source)
        self.assertFalse(self.target.exists())
        self.assertEqual(source_file.read_bytes(), b"unrelated")


if __name__ == "__main__":
    unittest.main()
