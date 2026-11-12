Set `magazineBackLayerPath` in the existing `minigame` object to draw an additional sprite behind the magazine and in front of `sliderBack`:

```json
"magazineBackLayerPath": "uiMagazineBackLayer"
```

The path resolves like other minigame sprite paths (omit `.png`). The sprite uses the full minigame image size at the gun origin and follows the gun's recoil and handedness. It does not intercept clicks.

When configured, the order from back to front is `sliderBack` → `magazineBackLayer` → magazine. This also applies to the staged P90 magazine. Without the sprite, the existing magazine order is preserved. Works with NewFirearms and standalone.
