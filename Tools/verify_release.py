"""Verify Unity's exported package contains only this package and exact source bytes."""
from pathlib import Path, PurePosixPath
import hashlib
import json
import re
import sys
import tarfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / "Packages/dev.avatar-tools.build-obfuscator"
ARTIFACTS = ROOT / "Artifacts"
VERSION = json.loads((PACKAGE / "package.json").read_text(encoding="utf-8"))["version"]
EXPORT = ARTIFACTS / f"AvatarBuildObfuscator-{VERSION}.unitypackage"

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

if sys.argv[1:] == ["zip"]:
    ARTIFACTS.mkdir(exist_ok=True)
    with zipfile.ZipFile(ARTIFACTS / f"dev.avatar-tools.build-obfuscator-{VERSION}.zip", "w", zipfile.ZIP_DEFLATED) as z:
        for p in sorted(PACKAGE.rglob("*")):
            if p.is_file():
                z.write(p, p.relative_to(PACKAGE).as_posix())
    print("VPM package ZIP created")
    raise SystemExit()

expected = {p.relative_to(PACKAGE).as_posix(): p for p in PACKAGE.rglob("*") if p.is_file() and p.suffix != ".meta"}
seen = set()
with tarfile.open(EXPORT, "r:gz") as archive:
    paths = [m for m in archive.getmembers() if m.name.endswith("/pathname")]
    for record in paths:
        imported = archive.extractfile(record).read().decode("utf-8-sig").strip()
        assert ".." not in PurePosixPath(imported).parts, imported
        assert imported == "Assets/AvatarBuildObfuscator" or imported.startswith("Assets/AvatarBuildObfuscator/"), imported
        if imported == "Assets/AvatarBuildObfuscator":
            continue
        relative = imported[len("Assets/AvatarBuildObfuscator/"):]
        source = PACKAGE / relative
        assert source.exists(), imported
        if source.is_dir():
            continue
        stem = record.name.rsplit("/", 1)[0]
        assert archive.extractfile(stem + "/asset").read() == source.read_bytes(), imported
        source_meta = Path(str(source) + ".meta")
        assert source_meta.is_file(), f"Missing source GUID: {source}"
        assert archive.extractfile(stem + "/asset.meta").read() == source_meta.read_bytes(), imported
        seen.add(relative)
assert seen == set(expected), sorted(set(expected) - seen)
guids = []
for meta in PACKAGE.rglob("*.meta"):
    match = re.search(r"^guid: ([a-f0-9]{32})$", meta.read_text(encoding="utf-8"), re.M)
    assert match, str(meta)
    guids.append(match.group(1))
assert len(guids) == len(set(guids)), "Duplicate .meta GUID"
result = {"version": VERSION, "verified_source_files": len(seen), "unique_guids": len(guids),
          "artifacts": [{"name": p.name, "bytes": p.stat().st_size, "sha256": sha(p)}
                        for p in (EXPORT, ARTIFACTS / f"dev.avatar-tools.build-obfuscator-{VERSION}.zip")]}
(ARTIFACTS / "verification.json").write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
(ARTIFACTS / "SHA256SUMS.txt").write_text("".join(x["sha256"] + "  " + x["name"] + "\n" for x in result["artifacts"]), encoding="utf-8")
print(json.dumps(result, ensure_ascii=False, indent=2))
