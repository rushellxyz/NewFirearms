# Full-game transition audit

## Full release verified on 2026-11-09

`/home/rushell/Documents/CU` now contains the full-release game assemblies. Both projects compile against these assemblies in Debug and Release.

The release's `Body.HandleVisuals(Painkillers)` obtains `GunScript` from a held gun-tagged item and dereferences its transform. NewFirearms weapons use `RshGun`, so this lookup can return null and stop the rest of the body's visual update. `GunAnimationPatch` replaces just this transform lookup with a native-first fallback to the held item's transform. It keeps the original animation method, gun flags, aiming calculations, and vanilla GunScript behavior; no dummy GunScript is attached. The transpiler is checked against the actual release method: exactly one transform call changes, and branch labels move with its inserted body-context load.

GunMinigame now reads native `Body.charType`: 0 is Experiment, 1 is Milky, 2 is Dune. Milky enables marksman handling unless the run setting already forces it for everyone. Species selection uses the supplied body where available, with Experiment as the fallback for missing/unknown characters. The Gunsaw Genetics plugin dependency, assembly reference, and integration code were removed.

The notes below describe the earlier pre-release review. They do not replace full-release validation or establish compatibility of the installed third-party multiplayer/core mods.

Reviewed against the [updated CUCoreLib transition guide](https://cucorelib.web.app/docs/full-game-transition/) on 2026-11-03.

`agentbox/CUCoreLib-main` is now a Git checkout of upstream `main` at `7f28e98` (2026-10-27). The previous source snapshot is preserved in `agentbox/CUCoreLib-before-update-2026-11-03`. The deployed transition guide is newer than the copy committed upstream; its machine-readable content is saved separately as `agentbox/full-game-transition-live-2026-11-03.json`.

## Documented API breaks

- `BuildingEntity.health`: damage, trader death checks, and course enemy checks already use CUCoreLib's `GetHealth()` / `SetHealth()` accessors. Keep using these: full-game health is a property whose setter can trigger destruction immediately.
- `Body.Eat`: neither assembly calls or patches it.
- `GlobalDark.betaBuild`, `Locale.GetPauseQuote`, and `PlayerCamera.CharacterIndexToLocaleName`: neither assembly references them.
- Overloaded `Body.Eat` / `WorldGeneration.SetBlock`: neither assembly patches them. Existing name-only Harmony targets each resolve to one method in the installed demo; recheck against full-game assemblies before claiming runtime support.
- Terrain and liquid allocation changes: the mod registers no custom terrain or liquid IDs and does not query CUCoreLib registrations using hardcoded numeric indices. Full-game wearable refresh and registration compatibility remain CUCoreLib's responsibility; use a CUCoreLib release supporting that game build.

No additional runtime shim is needed for the breaks listed in the guide. Optional Together, New Descents, and Gunsaw Genetics compatibility still depends on those integrations supporting the running game; this audit does not establish their full-game compatibility.

## Building against a specific installation

Both project files now consistently use `CU_PATH` when selecting `GameDirectory`; previously they checked `CU_PATH` but read `GAME_PATH`, potentially falling back to the local demo.

```sh
CU_PATH="/path/to/full-game" dotnet build NewFirearms/NewFirearms.csproj -c Release
# An explicit property takes precedence and is also passed to GunMinigame:
dotnet build NewFirearms/NewFirearms.csproj -c Release -p:GameDirectory="/path/to/full-game"
```

The selected installation must contain the referenced game, BepInEx, CUCoreLib, and optional-integration build assemblies. Optional integrations remain optional at runtime.

## Earlier pre-release validation

Debug and Release builds pass against the installed demo assemblies. Source inspection finds no direct references to the removed/changed members listed above. `CU_PATH` selection is checked for both projects.

The available CU, CU2, CU3, and CUV assemblies still expose the demo signatures (`Body.Eat(float, float)` and field-based `BuildingEntity.health`). Full-game compilation and live testing remain required when its assemblies are available. In particular, test plugin startup/patch application, firing and destroying enemies, inventory and magazines, the handling course, and optional multiplayer with a compatible Together version. A successful demo build is not proof of full-game runtime compatibility.
