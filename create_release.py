#!/usr/bin/env python3

import os, zipfile, shutil

mods = [
    "BetterLevelEditor",
    "Convinience",
    "D1CooperGun",
    "DevKillsList",
    "ExtendedCheats",
    "KingsmanEasterEgg",
    "ShowdownModePauseOnDesperadoDiff",
]

RELEASES_DIR = "releases"

os.makedirs("releases", exist_ok=True)

for mod in mods:
    dll = os.path.join(mod, "bin", "Release" , mod + ".dll")
    if mod == "BetterLevelEditor":
        files = []
        files.append((dll, mod + ".dll"))
        files.append((os.path.join(mod, "spawn_codes.txt"), "spawn_codes.txt"))

        for f, _ in files:
            if not os.path.isfile(f):
                print(f"{mod}: {f} not found. Skipping")
                break
        else:
            outfile = os.path.join(RELEASES_DIR, mod + ".zip")
            with zipfile.ZipFile(outfile, "w", zipfile.ZIP_DEFLATED) as zipf:
                for f, n in files:
                    zipf.write(f, n)
    else:
        shutil.copy(dll, RELEASES_DIR)
