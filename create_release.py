#!/usr/bin/env python3

import os, zipfile, shutil
from typing import List, Tuple

mods = [
    "BetterLevelEditor",
    "Convenience",
    "D1CooperGun",
    "D1Textures",
    "DevKillsList",
    "ExtendedCheats",
    "KingsmanEasterEgg",
    "ShowdownModePauseOnDesperadoDiff",
]

RELEASES_DIR = "releases"

os.makedirs("releases", exist_ok=True)


def create_zip(mod: str, files: List[Tuple[str, str]]) -> None:
    for f, _ in files:
        if not os.path.isfile(f):
            print(f"{mod}: {f} not found. Skipping mod.")
            break
    else:
        outfile = os.path.join(RELEASES_DIR, mod + ".zip")
        with zipfile.ZipFile(outfile, "w", zipfile.ZIP_DEFLATED) as zipf:
            for f, n in files:
                zipf.write(f, n)


for mod in mods:
    dll = os.path.join(mod, "bin", "Release", mod + ".dll")
    if mod == "BetterLevelEditor":
        files = [
            (dll, mod + ".dll"),
            (os.path.join(mod, "spawn_codes.txt"), "spawn_codes.txt"),
        ]
        create_zip(mod, files)

    elif mod == "D1Textures":
        files = [(dll, f"BepInEx/plugins/{mod}.dll")]
        for fname in os.listdir(f"{mod}/Textures"):
            files.append(
                (
                    f"{mod}/Textures/{fname}",
                    f"Desperados III_Data/Resources/Textures/{fname}",
                )
            )
        create_zip(mod, files)

    else:
        shutil.copy(dll, RELEASES_DIR)
