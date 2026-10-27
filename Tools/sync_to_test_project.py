# SPDX-License-Identifier: GPL-3.0-only
# Copyright (c) 2026 Avatar Build Obfuscator contributors

"""Install a fresh Assets copy in an explicitly selected disposable Unity project."""
import argparse
from pathlib import Path
import json
import os
import shutil

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Packages/dev.avatar-tools.build-obfuscator"
NAME = "dev.avatar-tools.build-obfuscator"


def sync(project, source=SOURCE):
    project = Path(project).resolve(strict=True)
    for name in ("Assets", "Packages"):
        directory = project / name
        if not directory.is_dir() or directory.resolve() != directory:
            raise ValueError("Expected a Unity project with ordinary Assets and Packages directories")
    manifest = json.loads((project / "Packages/manifest.json").read_text(encoding="utf-8-sig"))
    if NAME in manifest.get("dependencies", {}) or os.path.lexists(project / "Packages" / NAME):
        raise ValueError("This project already uses the UPM package. Update that package; do not create a duplicate Assets installation.")
    target = project / "Assets/AvatarBuildObfuscator"
    if os.path.lexists(target) or os.path.lexists(target.with_suffix(".meta")):
        raise ValueError("Refusing to overwrite an existing Assets/AvatarBuildObfuscator path or its .meta file. Use a fresh disposable test project.")
    source = Path(source)
    if source.is_symlink() or source.is_junction() or not source.is_dir():
        raise ValueError("Expected an ordinary package source directory")
    if any(path.is_symlink() or path.is_junction() for path in source.rglob("*")):
        raise ValueError("Package source must not contain links or junctions")
    # copytree also refuses a destination created after the checks above.
    shutil.copytree(source, target)
    return target


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("project", type=Path, help="Existing disposable Unity project")
    args = parser.parse_args()
    try:
        print(sync(args.project))
    except (OSError, ValueError) as error:
        parser.exit(1, str(error) + "\n")
