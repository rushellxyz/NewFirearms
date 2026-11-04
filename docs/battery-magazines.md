Magazine configs now use `type` instead of the removed `pseudoMagazine` boolean:

| `type` | Numeric value | Behavior |
| --- | --- | --- |
| `regular` | 0 | Load and unload individual rounds. |
| `pseudo` | 1 | Spawn full, cannot load/unload loose rounds, require a racked gun for insertion, and disappear when ejected empty. |
| `battery` | 2 | Accept up to `capacity` batteries and track each cell's charge. |

Existing regular configs default to `regular` if omitted. Packs using `"pseudoMagazine": true` must replace it with `"type": "pseudo"`; the old boolean is no longer supported. Bundled magazines have been migrated. `magazineType` remains the list of compatibility IDs, separate from this behavior selector.

A battery magazine can use these fields alongside its normal name, weight, value and item.png:

```json
{
    "type": "battery",
    "capacity": 2,
    "magazineType": ["powercell"],
    "ammoTypes": ["smallbattery"]
}
```

`ammoTypes` lists accepted battery item IDs. Battery magazines start empty; `capacity` sets the number of battery slots (default 1). Dragging an accepted battery onto the magazine consumes that item and stores its ID and charge (`Item.condition`, 0–1). Loading is rejected when all slots are occupied, including slots containing discharged cells. Using the magazine removes the oldest inserted cell and returns its remaining charge, including an exhausted battery.

Magazine condition is total remaining cell charge divided by slot capacity: one full cell in a two-slot magazine is 50%, and two full cells are 100%. Mixed accepted battery IDs and partial charges are preserved individually. Its hover text displays this overall charge instead of round count.

Battery guns use the following fields alongside their normal textures, audio and firing parameters:

```json
{
    "feedType": 5,
    "internalCapacity": 1,
    "magazineType": ["powercell"],
    "batteryDrainPerShot": 0.05,
    "desiredGasTime": 0.1,
    "fireModes": [0, 2, 3],
    "ammoTypes": [{"round": "smallbattery", "structureDamage": 10, "animalDamage": 10}]
}
```

Feed type 5 accepts only battery magazines, and regular magazine feed rejects them. Battery guns require exactly one `ammoTypes` entry; it defines the shot's damage, projectile, recoil and other effects. Its `round` ID can also supply the battery icon for the minigame bandolier. Loose batteries cannot be inserted directly into the gun.

`batteryDrainPerShot` is a fraction of one full cell's charge, greater than zero and no more than 1 (default 0.05). One successful shot consumes that amount regardless of pellet count. The gun draws from cells in insertion order, crossing cell boundaries when needed, and refuses to fire when their combined remaining charge is insufficient. Two full cells therefore provide twice as many shots as one. Battery guns do not require or perform racking, including after spawning, loading a magazine or restoring a saved game. Unless `needRetract` is enabled, rack controls do nothing. They cannot eject the magazine through `removeMagazineOnRack`. There are no casings; `desiredGasTime` controls shot spacing with a minimum of 0.05 seconds. Safety and burst/automatic trigger handling still apply.

Set `"needRetract": true` on a battery gun to require manual retraction after every shot (default `false`). A persistent `Special/TurretLine` connects the barrel to the first hit block/entity and follows moving targets. A miss leaves the line at the ray's maximum range; explosive ammunition follows its projectile until impact. Further shots, including burst/automatic follow-ups, are blocked until the player presses rack (`Z`, requires Marksman) or clicks the optional minigame rack trigger (available to all players). Retraction removes the line and uses `rackAudio`; it changes no chamber state and does not remove the magazine. Retraction is free by default. Normal shot cooldown still applies. Removing/replacing the battery magazine does not bypass retraction. Destroying an attached object/enemy automatically retracts the tether silently; in multiplayer the host performs and syncs this change. Terrain hits track the impacted cell and automatically retract if it becomes empty or changes block type, even while the shared terrain collider remains. Miss endpoints still require manual retraction.

`batteryDrainPerRetract` optionally charges for manual retraction, using the same full-cell fraction as `batteryDrainPerShot` (0–1, default 0). For example, `"batteryDrainPerRetract": 0.1` requires and consumes 10% of one cell across the inserted batteries. Insufficient charge leaves the tether attached and plays the dry-fire sound; no power is consumed. Insert a charged magazine to retract. Repeated rack presses while already retracted cost nothing. Automatic retraction on target or terrain-block destruction, or before saving, stays free. The host checks and consumes the charge, then syncs the battery and tether state together.

`minigame.rackTriggerPath` specifies an optional transparent PNG hit-area mask, using the same layout as `magReleasePath` (path relative to the gun folder, without `.png`). For example, `"rackTriggerPath": "uiRackTrigger"` uses `uiRackTrigger.png`; clicking its opaque area calls the gun's normal rack action, or retracts a battery gun. Missing or blank disables the trigger. The `Z` hotkey still requires Marksman, including on retractable guns. Set `minigame.onlyUnrackedToRacked` to `true` to make this button work only while the gun is unracked; already-racked guns ignore the click. It defaults to `false` and affects only this button. Battery-gun retraction remains available because battery guns do not enter the racked state.

Battery guns can add `"hasFlashlight": true` (default `false`) and a `minigame.flashlightTogglePath` PNG hit-area mask. Both a minigame definition and the toggle path are required when enabling this feature. For example:

```json
{
    "feedType": 5,
    "hasFlashlight": true,
    "minigame": {
        "flashlightTogglePath": "uiFlashlightToggle"
    }
}
```

This fragment supplements the gun's existing config. Clicking opaque pixels in `uiFlashlightToggle.png` switches the light on/off; there is no hotkey or inventory toggle, and no Marksman requirement. It starts off, including after loading a save. The world light is cloned from the emergency light's light child and uses the normal flashlight's low intensity (0.5). It follows the barrel and faces the firing direction.

While switched on, it drains the inserted magazine's cells in order at the stock flashlight's low-mode rate for a small battery: `flashlight.rotSpeed * 0.005 * globalDecayRate` full-cell fractions per second, with the stock sixfold wet penalty. Shooting and lighting draw from the same cells. The light can use charge below the amount needed for another shot; it switches off when charge runs out or the magazine is removed. Swapping batteries does not automatically switch it back on. Inside a container its beam is hidden while its power drain continues, as with a switched-on normal light. The host authorizes toggles and consumes power; gun snapshots sync charge and light state to clients.

Tethers retract silently before the gun is saved; the firing lock and endpoint are runtime-only and are not saved. In multiplayer, the host performs this retraction and syncs it to clients. Manual retraction uses the existing host-authorized rack action. Gun snapshots append the tether state and, when available, a synchronized target ID/local attachment point so clients follow the moving target. Targets without a synchronized ID use host position updates. A single gun-wide snapshot revision prevents delayed or duplicate packets from overwriting any gun state, including ammunition, battery charge, jams, the tether, and flashlight state. The host increments it for every snapshot, and clients check it before applying state or playing snapshot sounds. It is runtime-only and not saved.

**Multiplayer compatibility break:** gun snapshots now use `NewFirearms_RshGunSync_v2`, with the revision immediately after the gun ID; the old per-subsystem revision fields are removed. Every player must use the updated mod version. Older gun snapshot formats are no longer accepted.

The inserted magazine's cell IDs and individual charges are saved on the gun and restored to the magazine on ejection. This never changes the gun's own condition except for the existing per-shot wear setting. Cell charges and IDs are included in gun and magazine snapshots; clients request changes and the host performs them. Battery data uses a cell count followed by each cell's ID and charge. The previous single-cell save fields and snapshot format are no longer supported.
