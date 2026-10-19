"""Hash project content without making backups. Evidence stays in this repository."""
from pathlib import Path
import hashlib
import json
import sys

ROOT = Path(__file__).resolve().parents[1]
PROJECT = Path(sys.argv[2]).resolve()
if not (PROJECT / "Assets").is_dir() or not (PROJECT / "Packages").is_dir():
    raise SystemExit("Expected an existing Unity project")
OUT = ROOT / "Evidence~"
OUT.mkdir(exist_ok=True)
BASELINE = OUT / "project-before.json"

def digest(path):
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(4 * 1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()

def files():
    for name in ("Assets", "Packages", "ProjectSettings"):
        for path in (PROJECT / name).rglob("*"):
            if path.is_file():
                yield path.relative_to(PROJECT).as_posix(), path

if sys.argv[1] == "before":
    if BASELINE.exists():
        raise SystemExit("Baseline already exists; do not overwrite audit evidence")
    data = {name: digest(path) for name, path in files()}
    BASELINE.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"files": len(data), "manifest": str(BASELINE)}, ensure_ascii=False))
else:
    before = json.loads(BASELINE.read_text(encoding="utf-8"))
    now = dict(files())
    changed = [name for name, sha in before.items() if name in now and digest(now[name]) != sha]
    result = {"baseline_files": len(before), "changed_original": changed,
              "missing_original": sorted(set(before) - set(now)), "added": sorted(set(now) - set(before))}
    (OUT / "project-after.json").write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({**result, "added": result["added"][:12], "added_count": len(result["added"])}, ensure_ascii=False))
