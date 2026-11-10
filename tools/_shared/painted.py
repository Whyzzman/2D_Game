"""Drawn art the generator must not overwrite: Assets/Game/Art/Painted.txt, one path under Art/ per line.

A tool that writes a drawn PNG into Assets/Game/Art calls mark() with the files it wrote; ContentBuilder then
leaves them alone (PlaceholderImport.IsPainted). gamedesign/ART_ASSET_LIST.md, section 1.
"""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets/Game/Art"
LIST = ART / "Painted.txt"
HEADER = ("# Drawn art: files under Assets/Game/Art the generator never overwrites. One path per line.\n"
          "# Written by tools/_shared/painted.py; see gamedesign/ART_ASSET_LIST.md.\n")


def mark(paths):
    """Adds the files (absolute, or relative to Art/) to the list; returns how many were new."""
    known = set()
    if LIST.exists():
        known = {line.strip() for line in LIST.read_text(encoding="utf-8").splitlines()
                 if line.strip() and not line.startswith("#")}
    before = len(known)
    for path in paths:
        path = Path(path)
        known.add((path.resolve().relative_to(ART.resolve()) if path.is_absolute() else path).as_posix())
    if len(known) != before:
        LIST.write_text(HEADER + "\n".join(sorted(known)) + "\n", encoding="utf-8", newline="\n")
    return len(known) - before
