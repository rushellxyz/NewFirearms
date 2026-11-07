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
