#!/usr/bin/env python3

import os, json, zipfile


GAME_DIR = "/mnt/g/Steam/steamapps/common/Desperados III/Mods/"
RELEASES_DIR = "releases"

SCRIPT_DIR = os.path.dirname(__file__)
RELEASES_DIR = os.path.join(SCRIPT_DIR, RELEASES_DIR)
REPO_FILE = os.path.join(SCRIPT_DIR, "Repository.json")


if not os.path.isfile(REPO_FILE):
    print("No 'Repository.json' file found")
    exit(-1)
elif not os.path.isdir(GAME_DIR):
    print(f"No game direcotry found at '{GAME_DIR}'")
    exit(-1)


with open(REPO_FILE) as f:
    repo = json.load(f)


os.makedirs("releases", exist_ok=True)


for mod in repo["Releases"]:
    name = mod["Id"]
    mod_dir = os.path.join(GAME_DIR, name)

    if not os.path.isdir(mod_dir):
        print(f"No mod directory found for '{name}' at '{mod_dir}'. Skipping.")
        continue

    dll = os.path.join(mod_dir, name + ".dll")
    info = os.path.join(SCRIPT_DIR, name, "Info.json")

    if not os.path.isfile(dll):
        print(f"No DLL found for '{name}' at '{dll}'. Skipping.")
        continue

    if not os.path.isfile(info):
        print(f"No 'Info.json' found for '{name}' at '{info}'. Skipping.")
        continue

    with zipfile.ZipFile(
        os.path.join(RELEASES_DIR, name + ".zip"), "w", zipfile.ZIP_DEFLATED
    ) as zipf:
        zipf.write(dll, name + ".dll")
        zipf.write(info, "Info.json")
