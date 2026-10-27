The `newfirearms.gunjamming` run setting enables persistent ejection and feeding failures. Wear and wetness determine the chance. Jam rolls occur when ejecting a spent casing or feeding a live round, rather than as unrelated random failures when pulling the trigger or moving an empty action.

An ejection failure retains the spent casing in its chamber. A feed failure retains the next live round as the obstruction. Both stop automatic cycling and block firing. The minigame shows the obstruction at its configured casing position, moving with the gun. Its sprite uses the ammo's `minigameCasingSprite` for a spent casing, or the existing live-round sprite from `minigame.casingsPath` for a feeding failure.

Click the obstruction to eject it. Hovering or moving the minigame hand over it does not clear the jam. Clearing leaves the action open and consumes only the obstructing round or casing. Rack closed to feed the next round. Without the minigame, the normal rack action clears the jam.

Jam state is saved with the gun. Multiplayer clients request clearing through the existing host-validated gun action handler. Jam fields are appended to gun snapshots; snapshots without those fields are still accepted. Both peers need this release to display and clear the new jams through the minigame.

In-game checks:

- Use a worn or wet gun with jamming enabled. Check that an ejection jam retains a spent casing and a feed jam retains a live round, with the correct appearance for mixed ammo.
- Confirm that shooting, dragging ammo onto the gun, and cycling the slider cannot bypass a jam.
- Hover over the obstruction and confirm it remains stuck. Click it and check that exactly one item drops, the obstruction disappears, and closing the action feeds the next round.
- Save and reload with each jam type active. Switch guns and close/reopen the window while jammed.
- Repeat on a multiplayer client. Confirm the host owns ammo changes, repeated clicks cannot duplicate drops, and a late joiner sees the obstruction.
- Disable the minigame and clear a jam with the normal rack action. Disable jamming and confirm ordinary cycling still works.
