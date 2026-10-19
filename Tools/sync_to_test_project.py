"""Deploy only this package into the authorized, connected test project."""
from pathlib import Path
import shutil
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Packages/dev.kiine.avatar-guard"
PROJECT = Path(sys.argv[1]).resolve()
if not (PROJECT / "Assets").is_dir() or not (PROJECT / "Packages").is_dir():
    raise SystemExit("Expected an existing Unity project")
TARGET = PROJECT / "Assets/KiineAvatarGuard"
for source in SOURCE.rglob("*"):
    if source.is_file():
        destination = TARGET / source.relative_to(SOURCE)
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, destination)
print(TARGET)
