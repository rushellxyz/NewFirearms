# Demo and full-release runtime support

Build both assemblies against the full release. The resulting DLLs also handle the demo API (validated against demo Assembly-CSharp MVID `ab887c07-f518-41c9-8ea0-15c9c29ae1af`). This is runtime compatibility, not a separate demo build.

Each assembly centralizes its optional native features in `NativeGameFeatures`. NewFirearms uses `CUCoreLib.Helpers.GameBuildInfo.IsFullGame`, since CUCoreLib is already required by the main plugin. Independent GunMinigame checks native members once, keeping standalone operation free of a CUCoreLib dependency. Calls to release-only API are isolated in `MethodImplOptions.NoInlining` methods whose signatures use only shared types; callers never resolve the absent `EndLabScript` type token in the demo.

- Demo skips `WorldGeneration.cutsceneActive`, but still hides the minigame on the shared end/death screen.
- Demo has no `Body.HasHearingProtection`; gunshots retain normal hearing-loss behavior.
- Demo skips the scientist-hit escape callback; the missing Scientist tag is never queried.
- Debug `nf_embedcargo` reports that the feature is unavailable when `Body.EmbedCargo` is absent.
- Missing marksman keybinds are registered as E (rack) and G (unload). Existing release bindings and user remaps are preserved. Input routing detects native rack/unload call sites, avoiding double actions for release Milky while enabling demo marksman input.

CUCoreLib health accessors continue to bridge the demo field and full-release property. Optional multiplayer dependencies retain their existing guards; their own binaries must support the selected game version.

Validation includes compiling Debug/Release against the release, comparing all native member references against the demo, checking that every absent member is inside a NoInlining method, and invoking production guarded methods under Mono with actual demo assemblies. Standalone runtime probes also cover release capability detection and demo/full input routing. Full in-game and multiplayer playthroughs remain separate validation.

For headless main-plugin probes, a temporary copy of CUCoreLib omits unrelated Unity-engine-dependent static initialization while retaining its original edition-detection methods. Installed game/core DLLs are not modified. GunMinigame input, settings, and animation transpilers are exercised directly against each edition's real game assembly.
