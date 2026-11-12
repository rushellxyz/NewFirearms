# Support-grip hand position

Set optional coordinates in the gun's existing `minigame` object:

```json
"grabHandXPosition": 0.08,
"grabHandYPosition": -0.02
```

X is a fraction of screen width and Y a fraction of screen height, matching `casingXPosition` and `magazineXPosition` units. Coordinates are local to the gun center, or to the slider center with `grabOnSlider: true`. Positive X moves the hand right; positive Y moves it up. The configured point mirrors with the weapon when changing the leading hand.

Either coordinate can be omitted or null to preserve its existing placement. Without explicit coordinates, `grabOnSlider` uses the inertia barrel anchor; other grips use the clicked point plus the existing 5% window-width horizontal offset. An explicit X replaces that offset. Zero is a valid explicit coordinate.

This only positions the support-grip hand sprite. The grab mask and weapon inertia anchors keep their existing settings. Works in standalone and bundled mode.
