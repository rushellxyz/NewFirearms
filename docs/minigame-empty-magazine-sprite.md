Set `rackedOnlyWhenMagazineEmpty` inside the gun's existing `minigame` object:

```json
{
  "minigame": {
    "rackedOnlyWhenMagazineEmpty": true,
    "rackedOnlyPath": "uiEmptyMagazine",
    "unrackedOnlyPath": "uiNormal"
  }
}
```

When enabled, `rackedOnlySprite` appears only while a magazine is installed and contains no live rounds. A loaded magazine or no magazine selects `unrackedOnlySprite`. This replaces the usual selection by action state; it does not change the action, slider movement, or firing logic.

A live round remaining in the chamber does not make the magazine non-empty. Battery magazines count as empty when their total charge is zero. The minigame reads the existing synchronized gun state, so this setting needs no new network fields.

Omit the option or use `false` to preserve the existing selection by `IsRacked()`. Update both plugin assemblies together because `IMinigameGun` now includes `HasEmptyMagazine()`.

With `chargingHandle: true`, an installed empty magazine also prevents an open action from closing. This applies to manual racking and automatic cycling, including when the minigame is disabled. The minigame keeps the released handle at `rackedIdlePosition` while the magazine holds the action open. Remove the empty magazine or insert a loaded one to allow closing again. Multiplayer uses the host's magazine state for this check.

Removing the empty magazine leaves the minigame handle at `rackedIdlePosition` while the action remains open. Its open-action sprite also remains visible, including with `rackedOnlyWhenMagazineEmpty` enabled. Removing the magazine permits manual closing; it does not make the minigame close the action automatically. Inserting a loaded magazine restores the normal charging-handle return.

For firearms with `chargingHandle: true`, inserting a loaded magazine also closes an open action through the normal gun cycling logic and feeds the chamber. The host performs this operation and synchronizes the resulting state, regardless of whether the minigame is visible or enabled. Empty magazines leave the action open, and inserting a magazine into a closed action does not cycle it. Normal feed-jam rules still apply.

While an installed empty magazine blocks closing, the linear slider is constrained between `sliderMinimumPosition` and the closing threshold `rackedToUnracked`. Dragging cannot move it past this threshold toward the closed position or send repeated close requests. This also supports reversed slider ranges. Removing the magazine restores manual slider travel.
