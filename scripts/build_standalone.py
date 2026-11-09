#!/usr/bin/env python3
"""Build a vanilla-only archive from the independent GunMinigame content folders."""
import argparse
from pathlib import Path
import subprocess
import zipfile


def main():
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--configuration", choices=("Debug", "Release"), default="Release")
    parser.add_argument("--output", type=Path, default=root / "dist/GunMinigame-Standalone.zip")
    args = parser.parse_args()
    subprocess.run(["dotnet", "build", str(root / "GunMinigame/GunMinigame.csproj"),
                    "-c", args.configuration, "--nologo"], cwd=root, check=True)
    content = root / "GunMinigame"
    files = {}
    if not (content / "Resources").is_dir():
        raise FileNotFoundError(f"Missing standalone resources: {content / 'Resources'}")
    for path in (content / "Resources").rglob("*.png"):
        files[path.relative_to(content).as_posix()] = path
    for gun in ("pistol", "shotgun", "rifle"):
        folder = content / "Guns" / gun
        config = folder / "gun.json"
        if not config.is_file():
            raise FileNotFoundError(f"Missing standalone config: {config}")
        files[config.relative_to(content).as_posix()] = config
        for path in folder.rglob("*.png"):
            files[path.relative_to(content).as_posix()] = path
    args.output.parent.mkdir(parents=True, exist_ok=True)
    temporary = args.output.with_suffix(".zip.tmp")
    with zipfile.ZipFile(temporary, "w", zipfile.ZIP_DEFLATED) as archive:
        archive.write(root / "GunMinigame/bin" / args.configuration / "netstandard2.1/GunMinigame.dll",
                      "BepInEx/plugins/GunMinigame.dll")
        archive.write(root / "docs/standalone-minigame.md", "README.md")
        for name, path in sorted(files.items()):
            archive.write(path, "BepInEx/plugins/GunMinigame/" + name)
    temporary.replace(args.output)
    print(f"Standalone archive: {args.output} ({len(files)} content files)")


if __name__ == "__main__":
    main()
