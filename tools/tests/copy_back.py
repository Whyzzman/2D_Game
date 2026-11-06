#!/usr/bin/env python3
"""What the verification copy made that the project lacks (2026-11-06): after the copy's scene passes
(ZzApplyScenes — scenes rebuilt from the level texts, character normals) every file under Assets/Game whose bytes
differ from the project's, or that the project does not have. The copy-only tests (Zz*) are skipped.

  python tools/tests/copy_back.py [prefix ...]            list them (only under the prefixes, if given)
  python tools/tests/copy_back.py --apply [prefix ...]    copy them into the project (the Editor imports them when idle)

The character frames themselves (Art/Characters, Art/Source/Final/Characters) are made in the project by
tools/*/split.py and never come back from the copy, which may hold an older render.
"""
import hashlib
import shutil
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MAIN = ROOT / "Assets/Game"
COPY = ROOT / "Artifacts/Verification/Project/Assets/Game"
MADE_HERE = ("Art/Characters/", "Art/Source/Final/Characters/")


def digest(path):
    h = hashlib.blake2b(digest_size=16)
    with open(path, "rb") as f:
        for block in iter(lambda: f.read(1 << 20), b""):
            h.update(block)
    return h.digest()


def main():
    apply = "--apply" in sys.argv
    prefixes = [a.strip("/") + "/" for a in sys.argv[1:] if not a.startswith("--")]
    changed = []
    for src in COPY.rglob("*"):
        if not src.is_file() or src.name.startswith("Zz"):
            continue
        rel = src.relative_to(COPY)
        posix = rel.as_posix()
        if posix.startswith(MADE_HERE) or (prefixes and not any(posix.startswith(x) or posix + "/" == x for x in prefixes)):
            continue
        dst = MAIN / rel
        if not dst.exists() or dst.stat().st_size != src.stat().st_size or digest(dst) != digest(src):
            changed.append(rel)
    by_top = {}
    for rel in changed:
        key = "/".join(rel.parts[:2])
        by_top[key] = by_top.get(key, 0) + 1
    for key, n in sorted(by_top.items()):
        print(f"{n:6} {key}")
    if apply:
        for rel in changed:
            (MAIN / rel).parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(COPY / rel, MAIN / rel)
        print(f"copied {len(changed)} file(s) into the project")
    else:
        print(f"{len(changed)} file(s) differ")


if __name__ == "__main__":
    main()
