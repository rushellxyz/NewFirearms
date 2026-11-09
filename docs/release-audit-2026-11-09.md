# Release audit — 2026-11-09

Audited revision: `3a70f72` (`Setup inertia for shotguns`). Scope: the main plugin, GunMinigame startup and UI integration, multiplayer request/snapshot handling, bundled content, and release build configuration.

The project builds cleanly, and its existing split into two assemblies is reasonable. A rewrite is unnecessary for release. However, the remaining work includes functional defects, not just presentation tweaks: the bundled course requests a removed weapon, unarmed projectiles fail to finish on impact, and several compatibility paths still fail under identifiable conditions.

This is a source and build audit, not an in-game certification. Findings below follow directly from the checked-in code; their gameplay manifestations still need the listed runtime checks. Earlier third-party compatibility reports were not treated as proof that those integrations work or remain broken today.

## Validation performed

- `dotnet build NewFirearms/NewFirearms.csproj -c Release --no-restore -t:Rebuild` passed: **0 warnings, 0 errors**, rebuilding both assemblies against the locally available dependencies.
- Parsed the bundled JSON with comments and trailing commas accounted for: **18 guns, 11 magazines, 5 rounds**, plus settings.
- Checked **587 explicit sprite/texture path references** against the bundled files: none missing. This includes **68 COPY__ references**. File existence does not certify that every reference is cached before use at runtime.
- Compared compatible gun/magazine ammunition lists and checked the course's weapon IDs against bundled gun definitions.
- Did not launch the game, run a host/client session, exercise save/load, remove optional assemblies from an installation, or validate a packaged release archive.

## Findings to resolve before release

### R01 — P1: The course requires a weapon removed from the release pack

**Evidence:** [FirearmHandlingCourse.cs:14](../NewFirearms/FirearmHandlingCourse.cs#L14), [FirearmHandlingCourse.cs:94](../NewFirearms/FirearmHandlingCourse.cs#L94), [FirearmHandlingCourse.cs:143](../NewFirearms/FirearmHandlingCourse.cs#L143), [Ship/doc.txt:6](../Ship/doc.txt#L6).

`Weapons[0]` is `nf.m1911`, but `Ship/Guns` contains no such definition. The pack documentation explicitly says that the M1911 and its magazine were removed. The medical lesson immediately asks the instructor to fetch that ID, and `FetchInstructorItem` waits without a timeout for the matching item. Later lessons request the same ID again.

**Impact:** A clean install cannot supply the course's first demonstration gun through the bundled definitions. Depending on the underlying item creation behavior, the lesson stalls or encounters an item creation failure. An old installation retaining the removed content can conceal this defect.

**Fix:** Replace the course ID with a shipped handgun whose magazine and ammunition match the lesson, such as `pistol`. Validate course prerequisites before starting and give a clear failure message if a required definition is missing.

**Verify:** Install only the release pack into a clean plugin directory and complete the medical lesson, handgun lesson, and all three weapon practice stages.

### R02 — P2: An impact before arming does not consume the projectile

**Evidence:** [ExplosiveProjectile.cs:89](../NewFirearms/ExplosiveProjectile.cs#L89), [ExplosiveProjectile.cs:103](../NewFirearms/ExplosiveProjectile.cs#L103), [explosive-projectiles.md](explosive-projectiles.md), [grenade launcher config](../Ship/Guns/nf.grenadelauncher/gun.json).

After a collision, `Finish()` runs only inside `if (armed)`. An unarmed hit sets the rigidbody position to the impact point and returns, leaving the projectile enabled, simulated, and scheduled for network snapshots. That path also skips updating `distance` and `previousPosition`. The shipped launcher uses a minimum travel distance of five units, making nearby impacts a normal trigger.

**Impact:** A close impact leaves an active projectile instead of consuming it. It can repeatedly revisit the collision path; its later behavior depends on physics and the target. Cleanup may be delayed until the 60-second age limit. The documented no-explosion consumption behavior is not implemented.

**Fix:** Finish the projectile after either type of impact, with damage/explosion creation conditional on arming. Keep finishing idempotent. The comment claiming that finishing happens before damage callbacks also disagrees with the actual call order and should be corrected alongside the code.

**Verify:** Hit a wall inside five units: no explosion, immediate projectile removal, and no continuing host snapshots. Hit a farther wall: exactly one explosion and removal.

### R03 — P2: Client rack actions can hand the ejected magazine to the host

**Evidence:** [GunNetwork.cs:159](../NewFirearms/GunNetwork.cs#L159), [RshGun.cs:385](../NewFirearms/RshGun.cs#L385), [RshGun.cs:641](../NewFirearms/RshGun.cs#L641), [RshGun.cs:670](../NewFirearms/RshGun.cs#L670).

The server resolves the requesting player's body, but action 1 calls `Rack(manual: true)` without passing it. A gun with `removeMagazineOnRack` then calls `RemoveMag(null, playSound: false)`. Null falls back to `PlayerCamera.main.body`, and the deferred completion attempts pickup using that body.

**Impact:** A client racking a weapon with this option can cause the host to pick up the client's ejected magazine. `nf.makeshiftpistol` ships with this option. Ordinary action-3 magazine removal already passes `pb.body`; the rack path does not.

**Fix:** Carry the acting body through manual rack and magazine removal. For actions without an actor, explicitly decide whether the item should drop or be picked up rather than assuming the local camera player.

**Verify:** Have a client rack the makeshift pistol while the host has inventory space. Only the client's body should attempt pickup. Repeat on a dedicated host.

### R04 — P2: The supposedly optional spent-ammo snapshot byte is mandatory

**Evidence:** [GunNetwork.cs:67](../NewFirearms/GunNetwork.cs#L67), [GunNetwork.cs:74](../NewFirearms/GunNetwork.cs#L74), [RshGun.cs:804](../NewFirearms/RshGun.cs#L804).

The reader explicitly describes snapshots that omit the chamber's spent-ammo type, but the `AvailableBytes` guard and fallback are commented out. It unconditionally calls `GetSByte()` after the flashlight flag.

**Impact:** An otherwise compatible older snapshot ending at the flashlight flag reaches a read beyond its payload. The handler has already changed some gun state before this failure. This is a concrete backward-compatibility failure; it does not establish compatibility with still older formats that also lack other fields.

**Fix:** Restore the optional-tail check and use `-1` when the byte is absent. Document which previous packet layout is supported. If other layout changes are intentional breaks, introduce a deliberate version/migration policy rather than implying arbitrary mixed-version support.

**Verify:** Feed the reader the current layout and the same layout without the final byte. Both should apply successfully, with a generic spent casing for the older payload.

### R05 — P2: Gun snapshots throw on item spawn/despawn races

**Evidence:** [GunNetwork.cs:20](../NewFirearms/GunNetwork.cs#L20), [MagazineNetwork.cs:25](../NewFirearms/MagazineNetwork.cs#L25), [RshGun.cs:815](../NewFirearms/RshGun.cs#L815).

`GunNetwork.ClientSync` throws when the referenced item is not registered or lacks `RshGun`. Gun state uses both unreliable and reliable-unordered delivery, so a snapshot can arrive outside the item's lifetime or before its custom component is ready. The magazine receiver already treats the equivalent situation as an expected race and returns so a later snapshot can retry.

**Impact:** Normal join/spawn/despawn timing can raise packet-handler exceptions. Whether this only logs an error or disrupts the connection depends on Together's dispatcher; this audit does not claim a guaranteed disconnect.

**Fix:** Apply the magazine receiver's tolerate-and-retry behavior to gun snapshots. Also handle a component whose local definition has not yet resolved before dereferencing `prop`.

**Verify:** Join while the host is holding/firing a gun; repeatedly spawn and destroy guns during synchronization. No packet-handler exception should occur, and surviving items should converge on later snapshots.

### R06 — P2: The safety patch suppresses native guns it does not handle

**Evidence:** [PlayerCameraPatches.cs:197](../NewFirearms/PlayerCameraPatches.cs#L197), [NewDescentsCompatibility.cs:14](../NewFirearms/NewDescentsCompatibility.cs#L14).

`PlayerCameraPatch4.Prefix` returns `false` for every call to `GunToggleSafety`, including calls for items without an `RshGun`. The adjacent rack/eject patches explicitly delegate to native `GunScript`, but safety has no equivalent fallback. The New Descents bridge intentionally retains native `GunScript` for the thornslinger.

**Impact:** Native or third-party guns routed through `PlayerCamera.GunToggleSafety` lose the original safety behavior while NewFirearms is installed.

**Fix:** Suppress the original only when this patch handles an `RshGun`; otherwise return `true` so native behavior and compatible patches can run.

**Verify:** Toggle safety on a NewFirearms gun and a native `GunScript` gun, including thornslinger with New Descents installed.

### R07 — P1 for standalone distribution: Standalone startup always throws

**Evidence:** [GunMinigame/Plugin.cs:58](../GunMinigame/Plugin.cs#L58), [GunMinigame/Plugin.cs:119](../GunMinigame/Plugin.cs#L119), [StandaloneHandler.cs:14](../GunMinigame/StandaloneHandler.cs#L14).

If a `BepInEx/plugins/GunMinigame/` directory exists and neither the exact root-level `NewFirearms.dll` path nor the API-only marker exists, startup calls `PrepareStandaloneMode()`. That method always throws `NotImplementedException`, before Harmony patches and settings registration. `standalone` is never set true.

**Impact:** The advertised standalone path is unfinished. Detection is also installation-layout dependent: a bundled plugin placed in a subfolder can be misclassified if the standalone directory is present. The normal root-level bundled layout avoids this branch, so this is conditional on distribution/install layout.

**Fix:** Either complete standalone support or explicitly keep it unavailable with a clear, nonthrowing fallback. Detect the main plugin from BepInEx's discovered plugin metadata or another layout-independent capability check, accounting for initialization order. Do not label standalone support release-ready based on the successful bundled build.

**Verify:** Launch API-only, bundled, standalone, and nested-plugin layouts. Each supported layout must reach startup without exceptions; unsupported layouts should report their limitation clearly.

## Smaller follow-ups and runtime risks

These are separate from the confirmed failures above. Resolve or explicitly accept them according to release scope.

| Area | Observation | Suggested action |
| --- | --- | --- |
| Deferred magazine initialization | `RemoveMagPart2WhenCuCore` always waits at least 100 ms before applying exact contents. Until then, a newly created magazine can run `RshMag.Start` and generate its normal initial contents. The task is discarded. Interactions or saves during this window need runtime investigation. | Reuse the explicit `MagazineSpawnContents` handoff already present in the New Descents bridge, extending it for battery state, so initial contents are correct before interaction. Exercise rapid eject/reinsert and immediate save/load. |
| Settings validation | `LoadSettings` normalizes only `projectileSyncRate`. `gunSyncRate` and `magSyncRate` feed `InvokeRepeating` without finite-positive checks; `maxSliderValue` feeds slider bounds without validation. | Validate all intervals and bounds with observable defaults. |
| Default settings disagree | `Ship/settings.json` sets `strictSync: false` and one-second gun/mag intervals, while `SettingsJson` defaults to `strictSync: true` and half-second intervals. Missing or invalid settings therefore change authorization policy as well as cadence. | Choose intended defaults, align both sources, and accurately describe them. The relaxed host policy appears deliberate; it is not classified here as an accidental authorization bypass. |
| Piercing ammunition supply | Rifle and FAMAS define `nf.556piercing`, but the compatible `riflemagazine` accepts only `556round`. The first index agrees, so this is not a demonstrated index-corruption defect in shipped content. | Confirm whether piercing rounds are deliberately restricted to loose chamber loading. If magazine loading is intended, add them consistently across compatible gun/magazine definitions. |
| Pack authoring contract | Magazine insertion copies numeric ammo indices directly into guns; ejection copies them back. `CanFitMag` checks magazine family, not agreement of ammo ordering. | Validate ordering across compatible definitions or translate by round ID. A third-party pack can currently fit physically and use the wrong firing profile. |
| Partial content failure | Per-config validation catches errors and skips entries, but `CCLWorkaround` unconditionally replaces vanilla item actions with calls requiring custom components; trader/world generation also assume nonempty content pools. | Check a deliberately invalid vanilla replacement and an empty sellable pool. Keep skipped content from breaking the original item's use action or unrelated generation. |
| Resolution changes | `MinigameManager.Show` scales shared `MinigameInfo` positions in place once using `initialized`. | Change resolution after opening a gun and check alignment. Prefer retaining normalized definitions and deriving per-view coordinates if this is a supported user flow. |
| Release documentation | Hi-Point's description still refers to M1911 magazines. Projectile documentation says 30-second expiry while the implementation uses 60 seconds. README lacks dependency, installation-layout, build, and upgrade instructions. | Correct visible text and document clean upgrading from pre-2.0, including removal of obsolete content. |

## Architecture assessment

- The main plugin explicitly declares GunMinigame and CUCoreLib as hard dependencies. Together and Gunsaw Genetics are declared soft dependencies, as expected.
- The central multiplayer availability gate and the separation of transport readers from saveable components are useful. Keep them: a successful build with all DLLs present does not test Mono loading/serialization with an optional assembly absent.
- The Gunsaw Genetics species lookup has a guarded fallback and a `NoInlining` boundary. The marksman lookup uses a separate helper and broad catch with repeated error logging; test this path with the dependency absent and with an incompatible version before assuming it has the same failure behavior.
- Per-config validation, per-entry error reporting, and local definition dictionaries provide a solid base for content packs and client initialization.
- Gun snapshot revisions already address stale-state ordering. Magazine snapshots lack the equivalent revision check while mixing unreliable and reliable-unordered delivery; test rapid load/unload under latency for temporary state rollback.
- Preserve the explicit host execution model. The ownership loss in R03 needs the actor passed through existing APIs, not client-side inventory creation.

## Release validation checklist

1. Fix R01–R06. Resolve R07 if standalone is in scope; otherwise document its status and remove the throwing startup route.
2. Test a clean installation, not just the developer's existing plugin directory. Check the archive contains both matching DLLs, the full Ship payload in its expected runtime directory, and dependency instructions.
3. Launch with Together and Gunsaw Genetics absent; fire, manipulate guns, open the minigame, save, and reload. Repeat API-only and supported standalone layouts.
4. Run host plus client with strict synchronization on and off. Exercise firing, quick-tap burst, trigger release, dual wielding, jams, rack-ejection ownership, batteries, flashlight drain, and tether retraction. Include a dedicated host if supported.
5. Join late while guns and projectiles exist; destroy synchronized items during packet delivery. Check for handler exceptions and eventual convergence.
6. Test the grenade launcher against close and distant walls. Confirm unarmed removal and exactly one armed explosion.
7. Complete the course from a clean install. Save/load guns with mixed ammunition, inserted magazines, spent chambers, jams, and battery charge. Test rapid magazine ejection followed immediately by save or reinsertion.
8. Exercise the supported previous snapshot tail and legacy JSON configs. Document intentional breaks, including removed M1911 content, instead of silently relying on stale installed files.

No production code was changed as part of this audit.
