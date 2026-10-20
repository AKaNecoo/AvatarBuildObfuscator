"""Publish CI-built archives on push, keeping a version's product tree immutable."""
from pathlib import Path
import argparse
import json
import os
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
REPO = "AKaNecoo/AvatarBuildObfuscator"
PACKAGE = "Packages/dev.avatar-tools.build-obfuscator"


def run(*args, check=True):
    return subprocess.run(args, cwd=ROOT, text=True, capture_output=True, check=check)


def publish(channel, commit, artifacts):
    if os.environ.get("GITHUB_REPOSITORY") != REPO or channel not in ("main", "beta"):
        raise ValueError("Release publishing is restricted to this repository's main/beta push")
    manifest = json.loads((ROOT / PACKAGE / "package.json").read_text(encoding="utf-8-sig"))
    version = manifest["version"]
    if channel == "main" and "-" in version:
        raise ValueError("main releases must have a stable version")
    tag = "v" + version
    existing = run("gh", "release", "view", tag, "--repo", REPO, "--json", "tagName", check=False)
    if existing.returncode == 0:
        # A documentation-only commit can reuse the existing artifacts; changed product bytes need a version bump.
        old = run("gh", "api", f"repos/{REPO}/commits/{tag}", "--jq", ".sha").stdout.strip()
        if run("git", "diff", "--quiet", old, commit, "--", PACKAGE, check=False).returncode != 0:
            raise ValueError("Product changed under an existing release version; bump package.json")
        if channel == "main":
            run("gh", "release", "edit", tag, "--repo", REPO, "--prerelease=false", "--latest")
        print("Existing release verified: " + tag)
        return
    result = json.loads((artifacts / "package-build.json").read_text(encoding="utf-8"))
    if result["version"] != version:
        raise ValueError("CI artifact version differs from checkout")
    names = list(result["archives"]) + ["SHA256SUMS.txt", "package-build.json"]
    paths = [str(artifacts / name) for name in names]
    for path in paths:
        if not Path(path).is_file():
            raise ValueError("Missing release artifact: " + path)
    args = ["gh", "release", "create", tag, *paths, "--repo", REPO, "--target", commit,
            "--title", "Avatar Build Obfuscator " + version, "--notes-file", str(ROOT / "RELEASE_NOTES.md")]
    args += ["--prerelease"] if channel == "beta" else ["--latest"]
    run(*args)
    print("Published " + tag)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--channel", required=True, choices=("main", "beta"))
    parser.add_argument("--commit", required=True)
    parser.add_argument("--artifacts", type=Path, required=True)
    args = parser.parse_args()
    try:
        publish(args.channel, args.commit, args.artifacts)
    except subprocess.CalledProcessError as error:
        print(error.stderr, file=sys.stderr)
        raise
