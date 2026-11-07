Set `roundHeight` inside a gun's `minigame` object to control the height of its chamber preview, ejected rounds/casings, and jam obstruction:

```json
{
  "minigame": {
    "roundHeight": 20
  }
}
```

Merge the field into the existing `minigame` object. The height applies to both live rounds and spent casings. Width follows each sprite's aspect ratio, including custom `ammoTypes[].minigameCasingSprite` overrides. Sprite dimensions include any transparent padding in the image.

The value uses local UI units before the existing visual scale of `9 * Screen.height / 1440` is applied. For example, `20` produces a height of 180 screen pixels at 1440p. Width uses the existing horizontal scaling of `9 * Screen.width / 2560`. Changing screen resolution scales these visuals with the rest of the minigame.

Omit the field or use `0` to keep the original sizes: live rounds fit a 12 by 26 box and spent casings fit a 9 by 19 box. Negative and non-finite values are rejected during config validation. This option changes minigame visuals; world-item sprites and inventory icons retain their sizes.
