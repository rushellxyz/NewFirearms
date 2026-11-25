# Reimporting guns during pack development

Edit the installed files under `BepInEx/plugins/NewFirearms/Guns/<id>/`, then use:

```text
nf_reload nf.hipoint
nf_reload
nf_reload nf.hipoint --destroy
```

Without an ID, the command uses the gun in the player's leading hand. It works
in single-player and outside multiplayer sessions. The ID must already be
registered by NewFirearms; adding an entirely new gun, magazine or ammunition
definition still requires a restart. This command targets the NewFirearms pack,
not GunMinigame's standalone gun configs.

Reimport reads `gun.json`, PNGs (including `COPY__` references directly from disk)
and audio. It updates the registry, shop eligibility, crafting recipe, existing
scene instances, and the displayed minigame. Audio loads asynchronously, using
the usual fallback until loading completes. A new sprite cache is used for each
reimport, so repeated edits to a shared PNG are visible on the reimported gun.
Other guns that reference the same PNG retain their previous sprites until they
are also reimported.

Ordinary reimport preserves ammunition, condition, attachments' state and action
position. If the selected firing mode was removed, the first configured mode is
selected. Existing instances must keep the same `feedType`, `internalCapacity`,
ammo IDs and their order, `magazineType`, `hasLaser`, `needRetract`, and `openBolt`.

Use `--destroy` to bypass those structural restrictions. After JSON validation
and asset preparation succeed, **all scene instances of that gun ID are deleted**,
including guns in inventories and their loaded magazines/ammunition. Other gun
IDs are unaffected. Spawn a new gun with the usual game console to test its new
structure. `nf_reload --destroy` uses the leading-hand gun's ID.

Release any dragged round/magazine and finish minigame actions before reimport.
Invalid JSON, failed validation, and missing/invalid PNGs fail before replacing
the registered definition or deleting instances. Audio loading errors are logged
and keep the normal fallback audio. Only the registered gun is reimported;
magazine/round definitions, plugin DLLs, and standalone content are not reloaded.
