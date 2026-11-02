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

`batteryDrainPerShot` is a fraction of one full cell's charge, greater than zero and no more than 1 (default 0.05). One successful shot consumes that amount regardless of pellet count. The gun draws from cells in insertion order, crossing cell boundaries when needed, and refuses to fire when their combined remaining charge is insufficient. Two full cells therefore provide twice as many shots as one. Battery guns do not require or perform racking, including after spawning, loading a magazine or restoring a saved game. Rack controls do nothing and cannot eject their magazine through `removeMagazineOnRack`. There are no casings; `desiredGasTime` controls shot spacing with a minimum of 0.05 seconds. Safety and burst/automatic trigger handling still apply.

The inserted magazine's cell IDs and individual charges are saved on the gun and restored to the magazine on ejection. This never changes the gun's own condition except for the existing per-shot wear setting. Cell charges and IDs are included in gun and magazine snapshots; clients request changes and the host performs them. Battery data uses a cell count followed by each cell's ID and charge. The previous single-cell save fields and snapshot format are no longer supported.
