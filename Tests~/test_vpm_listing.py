# SPDX-License-Identifier: GPL-3.0-only
# Copyright (c) 2026 Avatar Build Obfuscator contributors

from pathlib import Path
import copy
import hashlib
import importlib.util
import io
import json
import unittest
import zipfile

ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location('vpm_listing', ROOT / 'Tools/vpm_listing.py')
VPM = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(VPM)


def fixture(version='0.7.0', extra=None, manifest_changes=None, component_guid=VPM.COMPONENT_GUID):
    manifest = {'name': VPM.PACKAGE, 'version': version, 'license': 'GPL-3.0-only', 'unity': '2022.3',
                'author': {'name': 'Contributors'}, 'vpmDependencies': {'nadena.dev.ndmf': '>=1.14.8 <2.0.0', 'com.vrchat.avatars': '>=3.7.0'}}
    manifest.update(manifest_changes or {})
    stream = io.BytesIO()
    with zipfile.ZipFile(stream, 'w') as archive:
        archive.writestr('package.json', json.dumps(manifest))
        archive.writestr('Runtime/AvatarBuildObfuscator.cs.meta', b'fileFormatVersion: 2\nguid: ' + component_guid + b'\n')
        if extra:
            record = zipfile.ZipInfo()
            record.filename = extra[0]
            record.orig_filename = extra[0]
            archive.writestr(record, extra[1])
    data = stream.getvalue()
    name = f'{VPM.PACKAGE}-{version}.zip'
    release = {'tag_name': 'v' + version, 'draft': False, 'prerelease': '-' in version, 'assets': [
        {'name': name, 'browser_download_url': f'https://github.com/{VPM.REPOSITORY}/releases/download/v{version}/{name}',
         'digest': 'sha256:' + hashlib.sha256(data).hexdigest(), 'size': len(data)}]}
    return release, data


class VpmListingTests(unittest.TestCase):
    def test_stable_listing_uses_verified_zip_and_keeps_dependencies(self):
        release, data = fixture()
        listing = VPM.build_listing([release], lambda _: data)
        package = listing['packages'][VPM.PACKAGE]['versions']['0.7.0']
        self.assertEqual(package['url'], release['assets'][0]['browser_download_url'])
        self.assertEqual(package['zipSHA256'], hashlib.sha256(data).hexdigest())
        self.assertEqual(listing['url'], VPM.INDEX_URL)
        self.assertIn('nadena.dev.ndmf', package['vpmDependencies'])

    def test_prereleases_sort_ahead_without_removing_stable(self):
        a, ad = fixture(); b, bd = fixture('0.8.0-beta.1')
        packages = {a['assets'][0]['browser_download_url']: ad, b['assets'][0]['browser_download_url']: bd}
        listing = VPM.build_listing([a, b], packages.__getitem__)
        self.assertEqual(list(listing['packages'][VPM.PACKAGE]['versions']), ['0.8.0-beta.1', '0.7.0'])

    def test_drafts_and_older_versions_are_not_downloaded(self):
        a, ad = fixture(); old, _ = fixture('0.6.0'); draft, _ = fixture('0.8.0-beta.1'); draft['draft'] = True
        seen = []
        VPM.build_listing([old, draft, a], lambda url: seen.append(url) or ad)
        self.assertEqual(seen, [a['assets'][0]['browser_download_url']])

    def test_modified_archive_or_size_is_rejected(self):
        release, data = fixture()
        with self.assertRaises(ValueError): VPM.build_listing([release], lambda _: data + b'changed')
        release['assets'][0]['digest'] = 'sha256:' + '0' * 64
        with self.assertRaises(ValueError): VPM.build_listing([release], lambda _: data)

    def test_untrusted_download_url_is_rejected_before_network(self):
        release, data = fixture(); release['assets'][0]['browser_download_url'] = 'https://example.invalid/package.zip'
        with self.assertRaises(ValueError): VPM.build_listing([release], lambda _: self.fail('Unexpected request'))

    def test_traversal_and_non_product_files_are_rejected(self):
        for name in ('../outside.cs', '/outside.cs', 'Runtime\\outside.cs', 'Avatar.fbx', 'Textures/private.png'):
            with self.subTest(name=name):
                release, data = fixture(extra=(name, b'private'))
                with self.assertRaises(ValueError): VPM.build_listing([release], lambda _: data)

    def test_wrong_manifest_version_guid_or_dependency_is_rejected(self):
        for changes in ({'version': '0.6.0'}, {'name': 'unrelated.package'}, {'license': 'MIT'}, {'vpmDependencies': {}}):
            release, data = fixture(manifest_changes=changes)
            with self.assertRaises(ValueError): VPM.build_listing([release], lambda _: data)
        release, data = fixture(component_guid=b'0' * 32)
        with self.assertRaises(ValueError): VPM.package_manifest(data, '0.7.0')

    def test_missing_or_duplicate_zip_fails(self):
        release, data = fixture(); release['assets'] *= 2
        with self.assertRaises(ValueError): VPM.build_listing([release], lambda _: data)
        release['assets'] = []
        with self.assertRaises(ValueError): VPM.build_listing([release], lambda _: data)

    def test_previously_listed_versions_are_immutable(self):
        release, data = fixture(); original = VPM.build_listing([release], lambda _: data)
        VPM.build_listing([release], lambda _: data, original)
        changed = copy.deepcopy(original)
        changed['packages'][VPM.PACKAGE]['versions']['0.7.0']['zipSHA256'] = 'f' * 64
        with self.assertRaises(ValueError): VPM.build_listing([release], lambda _: data, changed)
        changed['packages'][VPM.PACKAGE]['versions']['0.6.0'] = changed['packages'][VPM.PACKAGE]['versions'].pop('0.7.0')
        with self.assertRaises(ValueError): VPM.build_listing([release], lambda _: data, changed)

    def test_prerelease_only_and_invalid_versions_fail(self):
        release, data = fixture('0.8.0-beta.1')
        with self.assertRaises(ValueError): VPM.build_listing([release], lambda _: data)
        for version in ('0.08.0', '../0.8.0', '0.8.0-beta.01', '0.8'):
            with self.assertRaises(ValueError): VPM.version_key(version)

    def test_install_page_has_both_repository_routes_and_guides(self):
        release, data = fixture(); page = VPM.site_html(VPM.build_listing([release], lambda _: data))
        self.assertIn('vcc://vpm/addRepo?url=', page)
        self.assertIn(VPM.INDEX_URL, page)
        self.assertIn('https%3A%2F%2Fvpm.nadena.dev%2Fvpm.json', page)
        self.assertIn('README.en.md', page)
        self.assertIn('README.ja.md', page)


if __name__ == '__main__':
    unittest.main()
