# GunMinigame standalone

Extract the archive into the game's directory, keeping its `BepInEx/plugins` structure. It contains `GunMinigame.dll` and the `GunMinigame` content folder. BepInEx is required. NewFirearms, CUCoreLib, Together, and Gunsaw Genetics are not required by this plugin. Do not install a second copy of GunMinigame.dll alongside the bundled one.

The standalone UI supports **vanilla pistol, shotgun, and rifle only**. Makeshiftrifle and other weapons retain their existing UI. Its source configs and PNGs live independently in `GunMinigame/Guns/` and `GunMinigame/Resources/` in this repository. These were imported from the standalone archive, with shared images already stored under local `Shared/` paths. Packaging reads these folders directly and does not read or regenerate content from `Ship/`, so standalone edits are preserved independently of NewFirearms.

Edit `GunMinigame/Guns/<id>/gun.json` in the repository, or `BepInEx/plugins/GunMinigame/Guns/<id>/gun.json` in an installed copy, to configure the UI. Only its minigame configuration and UI sprite paths are read; damage, firing modes, and other NewFirearms weapon settings are not applied.

Weapons keep their original `GunScript`; ammunition and magazines keep `AmmoScript`. Dragging ammunition or magazines uses `Body.CombineItems`. Racking, ejecting a magazine, and toggling safety use the standard `PlayerCamera` gun actions. The window uses the native UI layer so clicks operate its controls instead of firing or dragging world items. A pending local trigger is released while the cursor is over the window or the gun is being manipulated. Shooting otherwise remains vanilla weapon logic. No item definitions, save format, custom network packets, or world weapon properties are replaced.

This lets a multiplayer client use the UI without installing GunMinigame on the host or other clients. The existing game's/multiplayer mod's normal actions still perform synchronization. The plugin does not implement its own multiplayer transport or bypass server validation. Live multiplayer verification with the user's Together/KrokMP release is still required.

Only each weapon's original ammo type is shown: `9mmround`, `12gauge`, or `556round`. Custom NewFirearms ammunition variants are omitted from the UI at runtime. Vanilla weapon firing modes remain unchanged; the fire-mode control toggles the original safety. When a config has no separate safety hit area, right-click the slider to toggle safety. A support grip affects only the minigame's visual recoil, preserving vanilla world recoil and network behavior.

Turn the minigame off in settings to restore vanilla UI. Missing or invalid standalone definitions log a warning and leave the affected gun's vanilla UI available. If NewFirearms is installed, GunMinigame automatically uses API-only mode for the main plugin instead. The `GunMinigame-ApiOnlyMode` marker in `BepInEx/plugins` also disables standalone activation.

Developer commands `nf_visualize` and `nf_coordinate` remain available. Player-species artwork and Milky handling use the native `Body.charType` (0: Experiment, 1: Milky, 2: Dune). Gunsaw Genetics is not referenced or needed.

To rebuild the standalone archive from this repository:

```sh
python3 scripts/build_standalone.py
```

The output is `dist/GunMinigame-Standalone.zip`. Build dependencies use the game's local assemblies as configured in `GunMinigame.csproj`; the archive ships no game or optional-mod DLLs.
