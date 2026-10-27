# SPDX-License-Identifier: GPL-3.0-only
# Copyright (c) 2026 Avatar Build Obfuscator contributors

"""Build the public VPM listing from verified, published ABO ZIP archives."""
from pathlib import Path, PurePosixPath
import argparse
import hashlib
import html
import io
import json
import os
import re
import stat
import urllib.error
import urllib.parse
import urllib.request
import zipfile

REPOSITORY = 'AKaNecoo/AvatarBuildObfuscator'
PACKAGE = 'dev.avatar-tools.build-obfuscator'
LEGACY_FOLDERS = {'Assets/AvatarBuildObfuscator': hashlib.sha256(PACKAGE.encode()).hexdigest()[:32]}
SITE = 'https://akanecoo.github.io/AvatarBuildObfuscator/'
INDEX_URL = SITE + 'index.json'
NDMF_REPO = 'https://vpm.nadena.dev/vpm.json'
VERSION = re.compile(r'^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$')
COMPONENT_GUID = b'7c5e3c2dfef2cf4469c50c0ba239aac9'
MAX_ARCHIVE = 16 * 1024 * 1024


def version_key(value):
    m = VERSION.fullmatch(value)
    if not m:
        raise ValueError('Invalid release version')
    pre = m[4]
    parts = []
    for part in pre.split('.') if pre else []:
        if part.isdecimal():
            if len(part) > 1 and part.startswith('0'):
                raise ValueError('Invalid numeric prerelease')
            parts.append((0, int(part)))
        else:
            parts.append((1, part))
    return (*map(int, m.group(1, 2, 3)), not bool(pre), tuple(parts))


def fetch(url, maximum=MAX_ARCHIVE):
    headers = {'User-Agent': 'Avatar-Build-Obfuscator-VPM', 'Accept': 'application/json'}
    if urllib.parse.urlsplit(url).netloc == 'api.github.com' and os.environ.get('GH_TOKEN'):
        headers['Authorization'] = 'Bearer ' + os.environ['GH_TOKEN']
    with urllib.request.urlopen(urllib.request.Request(url, headers=headers), timeout=45) as response:
        data = response.read(maximum + 1)
    if len(data) > maximum:
        raise ValueError('Response exceeds listing size limit')
    return data


def release_list():
    releases = []
    page = 1
    while True:
        data = json.loads(fetch(f'https://api.github.com/repos/{REPOSITORY}/releases?per_page=100&page={page}'))
        if not isinstance(data, list):
            raise ValueError('Unexpected GitHub release response')
        releases.extend(data)
        if len(data) < 100:
            return releases
        page += 1


def package_manifest(data, version):
    if len(data) > MAX_ARCHIVE:
        raise ValueError('Oversized ZIP')
    with zipfile.ZipFile(io.BytesIO(data)) as archive:
        entries = archive.infolist()
        names = [e.filename for e in entries]
        if len(entries) > 512 or len({n.casefold() for n in names}) != len(names):
            raise ValueError('Duplicate or excessive archive paths')
        if sum(e.file_size for e in entries) > 64 * 1024 * 1024:
            raise ValueError('Oversized archive payload')
        for entry in entries:
            if entry.filename != entry.orig_filename:
                raise ValueError('Archive path was normalized or truncated by the ZIP reader')
            path = PurePosixPath(entry.filename)
            if path.is_absolute() or '..' in path.parts or chr(92) in entry.filename or ':' in entry.filename or stat.S_ISLNK(entry.external_attr >> 16):
                raise ValueError('Unsafe archive path')
            if path.suffix not in ('.cs', '.meta', '.json', '.md', '.shader', '.asmdef') and path.name != 'LICENSE':
                raise ValueError('Non-product archive content')
        manifest = json.loads(archive.read('package.json').decode('utf-8-sig'))
        if manifest.get('name') != PACKAGE or manifest.get('version') != version:
            raise ValueError('Release/ZIP identity mismatch')
        if manifest.get('license') != 'GPL-3.0-only' or manifest.get('unity') != '2022.3':
            raise ValueError('Unexpected license or minimum Unity version')
        meta = archive.read('Runtime/AvatarBuildObfuscator.cs.meta')
        if not re.search(rb'^guid: ' + COMPONENT_GUID + rb'\s*$', meta, re.M):
            raise ValueError('Component GUID changed')
        if not {'nadena.dev.ndmf', 'com.vrchat.avatars'} <= manifest.get('vpmDependencies', {}).keys():
            raise ValueError('Missing VPM dependencies')
        if not manifest.get('author', {}).get('name'):
            raise ValueError('Missing package author')
        if manifest.get('legacyFolders') not in (None, LEGACY_FOLDERS):
            raise ValueError('Unexpected legacy migration scope')
        return manifest


def build_listing(releases, download=fetch, previous=None):
    versions = {}
    for release in releases:
        if release.get('draft') or not release.get('tag_name', '').startswith('v'):
            continue
        version = release['tag_name'][1:]
        key = version_key(version)
        # VPM distribution starts at the existing tested 0.7.0 stable release.
        if key < version_key('0.7.0'):
            continue
        if version in versions:
            raise ValueError('Duplicate version in release feed')
        filename = f'{PACKAGE}-{version}.zip'
        assets = [a for a in release.get('assets', []) if a.get('name') == filename]
        if len(assets) != 1:
            raise ValueError('Published version lacks one exact package ZIP: ' + version)
        asset = assets[0]
        expected_url = f'https://github.com/{REPOSITORY}/releases/download/v{version}/{filename}'
        if asset.get('browser_download_url') != expected_url:
            raise ValueError('Untrusted archive download URL')
        digest = asset.get('digest', '')
        if not re.fullmatch(r'sha256:[a-f0-9]{64}', digest) or not 0 < asset.get('size', 0) <= MAX_ARCHIVE:
            raise ValueError('Missing release asset integrity metadata')
        data = download(expected_url)
        if len(data) != asset['size'] or 'sha256:' + hashlib.sha256(data).hexdigest() != digest:
            raise ValueError('ZIP differs from published digest')
        manifest = package_manifest(data, version)
        manifest['url'] = expected_url
        manifest['zipSHA256'] = digest[7:]
        manifest['changelogUrl'] = f'https://github.com/{REPOSITORY}/blob/v{version}/CHANGELOG.md'
        manifest['author'].setdefault('email', 'kiinemaki@qq.com')
        versions[version] = manifest
    if not versions or not any('-' not in v for v in versions):
        raise ValueError('Listing must include a stable version')
    if previous:
        old = previous.get('packages', {}).get(PACKAGE, {}).get('versions', {})
        for version, manifest in old.items():
            if version not in versions or manifest.get('zipSHA256') != versions[version]['zipSHA256']:
                raise ValueError('A previously listed immutable version disappeared or changed: ' + version)
    return {'name': 'Avatar Build Obfuscator (ABO)', 'id': PACKAGE + '.repository', 'url': INDEX_URL,
            'author': 'Avatar Build Obfuscator contributors',
            'packages': {PACKAGE: {'versions': {v: versions[v] for v in sorted(versions, key=version_key, reverse=True)}}}}


def site_html(listing):
    add = 'vcc://vpm/addRepo?url=' + urllib.parse.quote(INDEX_URL, safe='')
    ndmf = 'vcc://vpm/addRepo?url=' + urllib.parse.quote(NDMF_REPO, safe='')
    managers = ('<a href="https://vrc-get.anatawa12.com/zh-hans/alcom/">ALCOM</a> / '
                '<a href="https://vrchat.com/home/download">VCC</a> / '
                '<a href="https://start.gamemode1.com/pages/vrclearnlauncher/">VRCLL</a>')
    links = ''.join(f'<li><a href="https://github.com/{REPOSITORY}/releases/tag/v{html.escape(v)}">{html.escape(v)}</a></li>'
                    for v in listing['packages'][PACKAGE]['versions'])
    return f'''<!doctype html>
<html lang="zh-Hans"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>ABO · ALCOM / VCC / VRCLL</title>
<style>body{{font:16px/1.7 system-ui,sans-serif;max-width:760px;margin:64px auto;padding:0 24px;color:#222;background:#fafafa}}a{{color:#2057a5}}.add{{display:inline-block;padding:10px 18px;margin:8px 12px 8px 0;background:#2057a5;color:white;text-decoration:none;border-radius:6px}}code{{display:block;overflow-wrap:anywhere;padding:12px;background:#eee}}h1{{line-height:1.2}}hr{{border:0;border-top:1px solid #ddd;margin:32px 0}}</style>
<h1>Avatar Build Obfuscator <small>ABO</small></h1>
<p>免费、开源的 VRChat Avatar 构建时混淆组件。仅处理构建副本。<br>
Free, open-source build-time avatar obfuscation. Processes build copies.<br>
無料・オープンソースのビルド時アバター難読化。ビルドコピーを処理します。</p>
<p>软件包管理器下载 · Download a package manager · パッケージマネージャー：{managers}</p>
<p><a class="add" href="{add}">添加仓库 · Add repository · リポジトリを追加</a></p>
<p>也可在仓库设置中粘贴以下地址。<br>Or paste this URL in repository settings. / リポジトリ設定に以下のURLを入力できます。</p>
<code>{INDEX_URL}</code>
<p>首次安装需要 VRChat Avatars SDK 和 NDMF。若列表找不到 NDMF，请先添加其官方仓库。<br>
Requires the VRChat Avatars SDK and NDMF. Add the official NDMF repository if it is unavailable.<br>
VRChat Avatars SDK と NDMF が必要です。NDMF が見つからない場合は公式リポジトリを追加してください。</p>
<a href="{ndmf}">添加 NDMF 官方仓库 · Add NDMF repository · NDMF公式リポジトリを追加</a>
<p>安装与更新请在 ALCOM／VCC／VRCLL 的工程包列表中操作。显示 beta 版本需开启预发布版本。<br>
Install and update in your project's package list in ALCOM/VCC/VRCLL. Enable prereleases to see beta versions.<br>
ALCOM／VCC／VRCLL のプロジェクトのパッケージ一覧からインストール・更新してください。beta はプレリリースの表示を有効にします。</p>
<p>从 Git 或 Unity 安装包迁移前，请阅读安装说明。<br>Read the installation guide before migrating from Git or unitypackage.<br>Git・unitypackageからの移行前に導入ガイドをご確認ください。</p>
<p><a href="https://github.com/{REPOSITORY}/blob/beta/README.md">中文说明</a> · <a href="https://github.com/{REPOSITORY}/blob/beta/README.en.md">English</a> · <a href="https://github.com/{REPOSITORY}/blob/beta/README.ja.md">日本語</a> · <a href="https://github.com/{REPOSITORY}">GitHub</a> · GPL-3.0-only</p>
<hr><h2>Versions</h2><ul>{links}</ul><p>Bug: <a href="mailto:kiinemaki@qq.com">kiinemaki@qq.com</a></p></html>'''


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--releases-file', type=Path)
    parser.add_argument('--previous', action='store_true')
    args = parser.parse_args()
    releases = json.loads(args.releases_file.read_text(encoding='utf-8-sig')) if args.releases_file else release_list()
    previous = None
    if args.previous:
        try:
            previous = json.loads(fetch(INDEX_URL))
        except urllib.error.HTTPError as error:
            if error.code != 404:
                raise
    listing = build_listing(releases, previous=previous)
    args.output.mkdir(parents=True, exist_ok=True)
    (args.output / 'index.json').write_text(json.dumps(listing, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    (args.output / 'index.html').write_text(site_html(listing), encoding='utf-8')
    (args.output / '.nojekyll').write_bytes(b'')
    print('Verified VPM versions: ' + ', '.join(listing['packages'][PACKAGE]['versions']))


if __name__ == '__main__':
    main()
