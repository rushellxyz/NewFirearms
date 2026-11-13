# Magazine row in pouches

Inside the existing `minigame` object, configure each pouch separately:

```json
"fannyPackMagazinesStartXPosition": 0.0332,
"fannyPackMagazinesStartYPosition": -0.0417,
"traumarigMagazinesStartXPosition": 0.0082,
"traumarigMagazinesStartYPosition": -0.0417,
"magazinesOffset": 0.025
```

Each pair places the first visible magazine relative to its pouch center. X uses a fraction of screen width; Y uses a fraction of screen height. Positive X moves the row right; positive Y moves it up. `magazinesOffset` keeps controlling horizontal spacing. Magazine capacities retain their existing behavior.

Omitted/null coordinates fall back independently to the legacy `magazinesStartXPosition` / `magazinesStartYPosition` fields (defaults: 85/2560 and -60/1440). The trauma rig's legacy X also subtracts one `magazinesOffset`, preserving its original placement. Explicit trauma-rig X places its first magazine directly at that coordinate, without that extra shift. Works in bundled and standalone mode.
