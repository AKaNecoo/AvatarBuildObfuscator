"""Validate and reproducibly build the distributable package, without a Unity license."""
from pathlib import Path
import argparse
import gzip
import hashlib
import io
import json
import re
import tarfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / "Packages/dev.avatar-tools.build-obfuscator"
NAME = "dev.avatar-tools.build-obfuscator"
VERSION = re.compile(r"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-[0-9A-Za-z.-]+)?$")


def validate(package=PACKAGE):
    manifest = json.loads((package / "package.json").read_text(encoding="utf-8-sig"))
    if manifest["name"] != NAME or not VERSION.fullmatch(manifest["version"]):
        raise ValueError("Invalid package identity/version")
    if manifest["unity"] != "2022.3" or manifest["license"] != "MIT":
        raise ValueError("Unexpected minimum Unity version/license")
    entries = sorted(package.rglob("*"))
    guids, paths = set(), set()
    for path in entries:
        if path.is_symlink():
            raise ValueError(f"Symlink in package: {path}")
        relative = path.relative_to(package).as_posix()
        if relative.lower() in paths:
            raise ValueError(f"Case-insensitive path collision: {relative}")
        paths.add(relative.lower())
        if path.suffix == ".meta":
            if not Path(str(path)[:-5]).exists():
                raise ValueError(f"Orphan meta: {relative}")
            match = re.search(r"^guid: ([a-f0-9]{32})\s*$", path.read_text(encoding="utf-8-sig"), re.M)
            if not match or match[1] in guids:
                raise ValueError(f"Invalid or duplicate GUID: {relative}")
            guids.add(match[1])
        else:
            if not Path(str(path) + ".meta").is_file():
                raise ValueError(f"Missing .meta: {relative}")
            if path.is_file() and path.suffix not in (".cs", ".asmdef", ".json", ".md") and path.name != "LICENSE":
                raise ValueError(f"Non-product file: {relative}")
            if path.suffix in (".asmdef", ".json"):
                json.loads(path.read_text(encoding="utf-8-sig"))
    marker = (package / "Runtime/AvatarBuildObfuscator.cs.meta").read_text(encoding="utf-8-sig")
    if "guid: 7c5e3c2dfef2cf4469c50c0ba239aac9" not in marker:
        raise ValueError("Existing component GUID changed")
    translations = {}
    text = (package / "Editor/InspectorLocalization.cs").read_text(encoding="utf-8-sig")
    for key, body in re.findall(r'\{ "([^"\n]+)", new\[\] \{ (.*?) \} \}', text):
        values = json.loads("[" + body + "]")
        if key in translations or len(values) != 3 or not all(isinstance(v, str) and v.strip() for v in values):
            raise ValueError("Incomplete or duplicate translation: " + key)
        translations[key] = values
    required = {"userName", "intro", "mode"}
    for source in (package / "Editor").glob("*.cs"):
        required.update(re.findall(r'"(update\.[a-zA-Z.]+)"', source.read_text(encoding="utf-8-sig")))
    if not required.issubset(translations):
        raise ValueError("Missing translation keys: " + str(required - translations.keys()))
    return manifest


def tar_bytes(items):
    raw = io.BytesIO()
    with tarfile.open(fileobj=raw, mode="w", format=tarfile.USTAR_FORMAT) as archive:
        for name, data in sorted(items.items()):
            entry = tarfile.TarInfo(name)
            entry.size, entry.mode, entry.mtime = len(data), 0o644, 0
            archive.addfile(entry, io.BytesIO(data))
    result = io.BytesIO()
    with gzip.GzipFile(fileobj=result, mode="wb", filename="", mtime=0) as compressed:
        compressed.write(raw.getvalue())
    return result.getvalue()


def build(output, package=PACKAGE):
    manifest = validate(package)
    output.mkdir(parents=True, exist_ok=True)
    version = manifest["version"]
    files = {p.relative_to(package).as_posix(): p.read_bytes() for p in sorted(package.rglob("*")) if p.is_file()}
    upm = output / f"{NAME}-{version}.tgz"
    upm.write_bytes(tar_bytes({"package/" + name: data for name, data in files.items()}))
    zipped = output / f"{NAME}-{version}.zip"
    with zipfile.ZipFile(zipped, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        for name, data in sorted(files.items()):
            entry = zipfile.ZipInfo(name, date_time=(1980, 1, 1, 0, 0, 0))
            entry.compress_type = zipfile.ZIP_DEFLATED
            entry.external_attr = 0o100644 << 16
            archive.writestr(entry, data)
    unity = {}
    # Stable legacy root folder; all actual product assets keep their checked-in GUIDs.
    root_guid = hashlib.sha256(NAME.encode()).hexdigest()[:32]
    unity[root_guid + "/pathname"] = b"Assets/AvatarBuildObfuscator"
    unity[root_guid + "/asset.meta"] = f"fileFormatVersion: 2\nguid: {root_guid}\nfolderAsset: yes\n".encode()
    for path in sorted(package.rglob("*")):
        if path.suffix == ".meta":
            continue
        relative = path.relative_to(package).as_posix()
        meta = Path(str(path) + ".meta").read_bytes()
        guid = re.search(rb"^guid: ([a-f0-9]{32})", meta, re.M)[1].decode()
        unity[guid + "/pathname"] = ("Assets/AvatarBuildObfuscator/" + relative).encode()
        unity[guid + "/asset.meta"] = meta
        if path.is_file():
            unity[guid + "/asset"] = path.read_bytes()
    exported = output / f"AvatarBuildObfuscator-{version}.unitypackage"
    exported.write_bytes(tar_bytes(unity))
    # Read back every archive and compare exact bytes. Never extract untrusted paths.
    with tarfile.open(upm, "r:gz") as archive:
        actual = {p.name[8:]: archive.extractfile(p).read() for p in archive.getmembers() if p.isfile() and p.name.startswith("package/")}
        if actual != files:
            raise ValueError("UPM archive differs from source")
    with zipfile.ZipFile(zipped) as archive:
        if {p: archive.read(p) for p in archive.namelist()} != files:
            raise ValueError("ZIP archive differs from source")
    with tarfile.open(exported, "r:gz") as archive:
        if {p.name: archive.extractfile(p).read() for p in archive.getmembers()} != unity:
            raise ValueError("Unity archive differs from source")
    hashes = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in (upm, zipped, exported)}
    (output / "SHA256SUMS.txt").write_text("".join(f"{value}  {name}\n" for name, value in hashes.items()), encoding="utf-8")
    result = {"version": version, "package_files": len(files), "archives": hashes}
    (output / "package-build.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
    return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=ROOT / "Artifacts")
    args = parser.parse_args()
    print(json.dumps(build(args.output), indent=2))
