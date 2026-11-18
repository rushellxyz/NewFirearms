Set `sliderRightClickFireToggle` in a gun's existing `minigame` object to switch fire modes by right-clicking the slider:

```json
{
  "minigame": {
    "sliderRightClickFireToggle": true
  }
}
```

Each right-button press on the slider's hit area calls `IMinigameGun.ToggleFireMode()`, using the existing host-authorized multiplayer action. It works with linear and rotating sliders and requires no separate `fireTogglePath`.

The option defaults to `false`. When enabled, it takes priority over `grabOnSlider` for right-clicks on the slider. Left-button dragging and the separate fire-mode button retain their behavior. Fire-mode switching through this gesture is blocked while the minigame is hidden, a game UI is open, an automated hand action is running, or the slider/support grip is already held.

Set `"locksSliderOnSafety": true` inside `minigame` to interlock the selector and the action:

- Fire modes (including safety) can only be switched with the action closed. The minigame also waits for the slider to finish returning to its closed position.
- Safety (fire mode `0`) allows grabbing and pulling the slider as far as `unrackedToRacked`, but prevents passing that opening threshold. This travel limit also applies to rotating bolts and the outside-window pump gesture. Marksmen racking and removing a magazine by fully pulling the slider require disengaging safety first.

The setting defaults to `false`, preserving existing configs. It applies to both the separate fire-mode button and right-click switching. NewFirearms validates manual rack and selector requests on the host as well. Automatic cycling is unaffected. Standalone minigame adapters enforce the same restrictions for their controls.
