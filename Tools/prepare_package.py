"""Copy the license and adopt Unity-generated .meta GUIDs from an explicit test project."""
from pathlib import Path
import shutil
import sys

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / "Packages/dev.kiine.avatar-guard"
PROJECT = Path(sys.argv[1]).resolve()
TARGET = PROJECT / "Assets/KiineAvatarGuard"
shutil.copyfile(ROOT / "LICENSE", PACKAGE / "LICENSE")
copied = 0
for meta in TARGET.rglob("*.meta"):
    relative = meta.relative_to(TARGET)
    resource = PACKAGE / str(relative)[:-5]
    if not resource.exists():
        continue
    destination = PACKAGE / relative
    if destination.exists() and destination.read_bytes() != meta.read_bytes():
        raise SystemExit(f"Refusing to replace a different GUID: {destination}")
    if not destination.exists():
        shutil.copyfile(meta, destination)
        copied += 1
print(f"Adopted {copied} stable Unity .meta files")
