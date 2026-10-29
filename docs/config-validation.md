ItemLoader validates guns, magazines, and rounds before loading assets or registering items. Validation reports the config filename and all detected bad fields together. JSON syntax/type errors retain Newtonsoft.Json's field path and line information. Unexpected failures include their stack trace; sprite/texture decoding errors also identify the source field.

Array requirements:

- `ammoTypes` must contain non-null entries with a `round` ID.
- `bulletsSpritesPath`, when supplied, needs at least `ammoTypes.Count + 2` entries: empty, spent casing, then one icon per ammo type. Omission uses generic round icons for every ammo type.
- `minigame.ammosInHandPaths` and `minigame.ammosInPtrPaths` need an entry per ammo type.
- `minigame.casingsPath`, when supplied, needs a spent-casing entry followed by one live-round entry per ammo type.
- `minigame.ammosInMagazinePaths`, when supplied, needs a matching key for every magazine in `minigame.magazinePaths`, with an array covering every ammo type.
- Fire-mode sprite arrays are indexed by the numeric mode, including unused slots between modes.

Missing required fields, invalid capacities/feed types/fire modes, null recipes on craftable items, unresolved `COPY__` references, and missing PNGs report their exact field. Paths are relative to the item's folder and omit `.png`. Empty `magazineType` arrays remain valid for ammo boxes. Audio loading retains its fallback behavior and names the configured audio field in diagnostics.

For example:

```text
[NewFirearms] Invalid config '.../Guns/nf.famas/gun.json':
  - minigame.magazineDragPath: sprite file '.../Guns/nf.famas/uiDragTrigger.png' does not exist; paths must omit .png.
```

An invalid item is skipped and the next config is attempted. Validation does not rewrite configs.
