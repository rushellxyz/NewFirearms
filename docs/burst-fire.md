Add `4` to a gun's `fireModes` array to enable three-round burst, for example `"fireModes": [0, 2, 4]` for safe, semi-auto, and burst. Existing mode IDs remain unchanged.

Each trigger press fires up to three rounds at the configured `desiredGasTime` interval. A quick trigger release still completes the current burst; holding the trigger does not start another burst. Jams, insufficient ammo, losing the held weapon, manual cycling, magazine removal, and selector changes interrupt the burst. A burst-capable gun with no positive gas time uses a 0.05-second minimum interval.

The host counts burst shots in multiplayer. Clients use the existing shoot/release requests and receive each shot's existing visual/state updates. No new packet fields are required.

The default selector sprite for mode 4 is `Resources/fireBurst.png`. Custom `fireModesSpritePath` and minigame `fireModePaths` arrays are indexed by numeric mode, so a custom array covering burst needs a fifth entry (index 4). The legacy selector can also reference the fallback explicitly as `COPY__Resources/fireBurst`.

In-game checks: test a quick tap, holding fire, repeated clicks during a burst, one/two rounds remaining, jams on each shot, switching modes/weapons, manual racking, and magazine removal. Repeat on a multiplayer client and verify that both peers observe exactly the rounds consumed by the host.
