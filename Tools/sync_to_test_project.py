"""Deploy only this package into the authorized, connected test project."""
from pathlib import Path
import json
import shutil
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Packages/dev.avatar-tools.build-obfuscator"
PROJECT = Path(sys.argv[1]).resolve()
if not (PROJECT / "Assets").is_dir() or not (PROJECT / "Packages").is_dir():
    raise SystemExit("Expected an existing Unity project")
manifest_path = PROJECT / "Packages/manifest.json"
manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
if "dev.avatar-tools.build-obfuscator" in manifest.get("dependencies", {}) or (PROJECT / "Packages/dev.avatar-tools.build-obfuscator").exists():
    raise SystemExit("This project already uses the UPM package. Update that package; do not create a duplicate Assets installation.")
TARGET = PROJECT / "Assets/AvatarBuildObfuscator"
for source in SOURCE.rglob("*"):
    if source.is_file():
        destination = TARGET / source.relative_to(SOURCE)
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, destination)
print(TARGET)
