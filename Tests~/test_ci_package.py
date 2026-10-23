# SPDX-License-Identifier: GPL-3.0-only
# Copyright (c) 2026 Avatar Build Obfuscator contributors

import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("ci_package", ROOT / "Tools/ci_package.py")
ci = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ci)


class PackageTests(unittest.TestCase):
    def setUp(self):
        (ROOT / "Artifacts").mkdir(exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(dir=ROOT / "Artifacts")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.package = self.root / "package"
        shutil.copytree(ci.PACKAGE, self.package)

    def test_reproducible_archives(self):
        a = ci.build(self.root / "a", self.package)
        b = ci.build(self.root / "b", self.package)
        self.assertEqual(a, b)
        self.assertEqual(a["package_files"], len(list(p for p in self.package.rglob("*") if p.is_file())))

    def test_missing_meta(self):
        (self.package / "README.md.meta").unlink()
        with self.assertRaisesRegex(ValueError, "Missing .meta"):
            ci.validate(self.package)

    def test_duplicate_guid(self):
        shutil.copyfile(self.package / "README.md.meta", self.package / "LICENSE.meta")
        with self.assertRaisesRegex(ValueError, "duplicate GUID"):
            ci.validate(self.package)

    def test_orphan_meta(self):
        (self.package / "README.md").unlink()
        with self.assertRaisesRegex(ValueError, "Orphan meta"):
            ci.validate(self.package)

    def test_private_asset_rejected(self):
        (self.package / "private.fbx").write_bytes(b"fixture")
        (self.package / "private.fbx.meta").write_text("guid: " + "f" * 32)
        with self.assertRaisesRegex(ValueError, "Non-product"):
            ci.validate(self.package)

    def test_component_guid_retained(self):
        p = self.package / "Runtime/AvatarBuildObfuscator.cs.meta"
        p.write_text(p.read_text().replace("7c5e3c2dfef2cf4469c50c0ba239aac9", "e" * 32))
        with self.assertRaisesRegex(ValueError, "component GUID"):
            ci.validate(self.package)

    def test_license_identifier_matches_release(self):
        p = self.package / "package.json"
        manifest = json.loads(p.read_text())
        manifest["license"] = "MIT"
        p.write_text(json.dumps(manifest))
        with self.assertRaisesRegex(ValueError, "license"):
            ci.validate(self.package)

    def test_license_text_matches_repository(self):
        (self.package / "LICENSE").write_text("MIT License\n")
        with self.assertRaisesRegex(ValueError, "GPL license text"):
            ci.validate(self.package)

    def test_vpm_download_version_and_legacy_scope(self):
        p = self.package / 'package.json'
        original = json.loads(p.read_text())
        for key, value in (('url', 'https://example.invalid/package.zip'), ('changelogUrl', 'https://example.invalid/log'),
                           ('legacyFolders', {'Assets': ''})):
            with self.subTest(key=key):
                manifest = dict(original)
                manifest[key] = value
                p.write_text(json.dumps(manifest))
                with self.assertRaises(ValueError):
                    ci.validate(self.package)


if __name__ == "__main__":
    unittest.main()
