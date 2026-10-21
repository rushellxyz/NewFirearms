# Localization

English UI text lives in:

- `NewFirearms/lang/EN-com.rushellxyz.newfirearms.json`
- `GunMinigame/lang/EN-com.rushellxyz.gunminigame.json`

Both projects embed `lang/*.json` into their DLLs. CUCoreLib discovers these files automatically and applies English fallback, the selected language, then external overlays. Main-plugin lookups use `LocaleRegistry.Get` through `ModLocale`; the minigame also fills missing settings text from its embedded file when CUCoreLib is absent. Newtonsoft.Json is supplied by the game; the minigame does not reference CUCoreLib.

To translate, copy a file and replace `EN` with the exact in-game language name, keeping the plugin GUID suffix and all JSON keys. For example, use `zh-CN-com.rushellxyz.newfirearms.json`. Put it in the project's `lang` directory to embed it on the next build, or ship it directly beside the corresponding DLL for CUCoreLib to load as an external overlay.

Keep numbered placeholders such as `{0}` and `{1}` intact. Translate the surrounding text and reorder placeholders if needed. JSON `\n` introduces a line break. Multiplayer damage descriptions have separate default and `.mp` entries.

Item names and descriptions in gun/magazine/round content JSON remain backward-compatible defaults; CUCoreLib localizes them through the `item` category. In-game `createLocale com.rushellxyz.newfirearms` generates registered item translations after content registration; merge that file's `item` section with the shipped file's `other` section for a complete translation. The vanilla-item compatibility shim also respects these translations.

Course dialogue, objectives, results, settings, command help, and player-facing denial messages are in `other`. Diagnostic logs and internal Unity object names stay in code.
