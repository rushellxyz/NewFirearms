# NewFirearms compatibility audit — 2026-10-31

Static review of the 50 archives supplied in `agentbox/other-plugins`, against repository commit `4ed07d188a6855b8127a15233ca4eae22d286b9f`. The existing user change to `Ship/Guns/nf.hipoint/uiSliderFront.png` was left untouched. No runtime mod code, installed plugins, settings or saves were changed. None of the downloaded plugin code was executed.

All 50 archives extracted successfully (45 ZIP and 5 RAR). The inventory contains 90 DLL copies. After hashing duplicates and excluding BepInEx/bootstrap/support libraries as requested, **49 distinct plugin assemblies** were decompiled to C# and inspected for shared patch targets, item creation/registration, ammunition state, damage, multiplayer authority, saves, localization and input. The translation-only archive was inspected as data. Dependencies were resolved from the supplied archives and local game installation. Some output contains missing-reference/attribute annotations; those are not reported as demonstrated runtime failures.

These findings establish conflicting code paths, with explicit triggers; **they are not live gameplay reproductions**. Shared Harmony targets alone were not counted as breakages. Findings below contain only identified failures/mismatches. Other observations are in [unusual findings](compatibility-unusual-2026-10-31.md). A blank findings column in the [coverage index](compatibility-coverage-2026-10-31.csv) means no concrete breakage was recorded, not that the mod has been certified compatible.

Extraction logs, exact DLL hashes, duplicate mappings and complete decompiled sources remain in `agentbox/compatibility-audit/` (ignored by git): `archives.json`, `assemblies.json`, `plugins.json`, `plugin-index.txt`, `unpacked/`, `decompiled/`. Source links below point to that local evidence. Reproduce extraction with `unpack.py`; C# output was generated with ILSpyCmd 11.2.0.10050 using `decompile.py`. Support-library output, if present from the initial inventory pass, is outside this report’s review scope.

## Findings

### B01 — CPUOptimization2 loses round lists when an item is streamed out and recreated

**Impact:** High: ammunition/state loss; SP recreation path, also any MP path that recreates rather than revives a pinned object.

**Trigger:** A loaded NewFirearms gun or magazine is left in a chunk which CPUOptimization2 unloads, then that chunk is revisited. Its Stream/Enabled setting defaults to true.

Its foreign-component snapshot saves declared scalar/enum fields, but rejects List<sbyte> and dictionaries. This preserves existed, mag, racked and jam flags while dropping rounds and spentAmmoTypes. The round count fallback only handles AmmoScript. Restoring the object therefore cannot restore its ammunition: a magazine can have existed=true with an empty round list; a gun can claim an inserted magazine while its chamber/round list is empty. Component Start may also be explicitly skipped, so its initialization and repeating network sync are not a recovery path. This applies when the original object is destroyed and recreated; the MP pinned-object revival path is a different case.

**Evidence:** [CPUOptimization2.dll:2309](../agentbox/compatibility-audit/decompiled/07-CPUOptimization2-b25139b1/CPUOptimization2.decompiled.cs:2309), [CPUOptimization2.dll:2877](../agentbox/compatibility-audit/decompiled/07-CPUOptimization2-b25139b1/CPUOptimization2.decompiled.cs:2877), [CPUOptimization2.dll:2435](../agentbox/compatibility-audit/decompiled/07-CPUOptimization2-b25139b1/CPUOptimization2.decompiled.cs:2435), [CPUOptimization2.dll:2515](../agentbox/compatibility-audit/decompiled/07-CPUOptimization2-b25139b1/CPUOptimization2.decompiled.cs:2515), [CPUOptimization2.dll:11390](../agentbox/compatibility-audit/decompiled/07-CPUOptimization2-b25139b1/CPUOptimization2.decompiled.cs:11390), [CPUOptimization2.dll:12606](../agentbox/compatibility-audit/decompiled/07-CPUOptimization2-b25139b1/CPUOptimization2.decompiled.cs:12606), [CPUOptimization2.dll:22612](../agentbox/compatibility-audit/decompiled/07-CPUOptimization2-b25139b1/CPUOptimization2.decompiled.cs:22612), [RshMag.cs:17](../NewFirearms/RshMag.cs:17), [RshGun.cs:168](../NewFirearms/RshGun.cs:168).

### B02 — CatPatch gun self-harm dereferences a removed GunScript

**Impact:** High: NullReferenceException; multiplayer with CatPatch enabled.

**Trigger:** CatPatch selects a NewFirearms gun for its low-mood gun-suicide branch.

NewFirearms deliberately disables its own SelfHarmer patch when CatPatch is active. CatPatch then finds the item by the gun tag and unconditionally accesses item.GetComponent<GunScript>().roundInChamber. NewFirearms has removed that component and supplies RshGun instead. The coroutine fails before reaching its normal completion/cleanup. Later racking and firing accesses make the same assumption.

**Evidence:** [CatPatch.dll:11995](../agentbox/compatibility-audit/decompiled/14-CatPatch-5a6dbf76/CatPatch.decompiled.cs:11995), [CatPatch.dll:12866](../agentbox/compatibility-audit/decompiled/14-CatPatch-5a6dbf76/CatPatch.decompiled.cs:12866), [CatPatch.dll:12902](../agentbox/compatibility-audit/decompiled/14-CatPatch-5a6dbf76/CatPatch.decompiled.cs:12902), [SelfHarmerPatch.cs:14](../NewFirearms/SelfHarmerPatch.cs:14), [RshComponent.cs:19](../NewFirearms/RshComponent.cs:19).

### B03 — Axivorn gun-equipped NPCs cannot operate converted weapons

**Impact:** High: ranged NPC combat and gun/ammo loot fail for converted IDs.

**Trigger:** Axivorn equips an NPC with a converted vanilla pistol/rifle/shotgun, or a NewFirearms weapon.

NPC gun setup and combat resolve GunScript, not RshGun. Once NewFirearms removes GunScript, GetGun returns null, CanFireNow cannot find a live chamber, and its gun/ammo loot extraction also skips that held item. The NPC may visibly hold the weapon and be marked HasGun while unable to use it.

**Evidence:** [AxivornsExpieNPCMod.dll:3885](../agentbox/compatibility-audit/decompiled/06-AxivornsExpieNPCMod-ee7694e2/AxivornsExpieNPCMod.decompiled.cs:3885), [AxivornsExpieNPCMod.dll:3909](../agentbox/compatibility-audit/decompiled/06-AxivornsExpieNPCMod-ee7694e2/AxivornsExpieNPCMod.decompiled.cs:3909), [AxivornsExpieNPCMod.dll:3965](../agentbox/compatibility-audit/decompiled/06-AxivornsExpieNPCMod-ee7694e2/AxivornsExpieNPCMod.decompiled.cs:3965), [AxivornsExpieNPCMod.dll:4910](../agentbox/compatibility-audit/decompiled/06-AxivornsExpieNPCMod-ee7694e2/AxivornsExpieNPCMod.decompiled.cs:4910), [AxivornsExpieNPCMod.dll:4928](../agentbox/compatibility-audit/decompiled/06-AxivornsExpieNPCMod-ee7694e2/AxivornsExpieNPCMod.decompiled.cs:4928), [RshComponent.cs:19](../NewFirearms/RshComponent.cs:19).

### B04 — NewFirearms bullets do not injure Axivorn Body-based NPCs in single player

**Impact:** High: player firearms cannot damage these NPCs through the ordinary bullet path.

**Trigger:** In single player, shoot an Axivorn Experiment NPC with a non-explosive NewFirearms round.

Axivorn constructs these NPCs from Body/Limb objects. NewFirearms handles BuildingEntity enemies first, but gates all Body/Limb firearm damage behind multiplayer plus PvP. In SP it continues before finding the victim or calling InduceDamage. Consequently these NPCs can receive an impulse or cloud effect without bullet injury. Explosions use a different game path and are not covered by this finding.

**Evidence:** [AxivornsExpieNPCMod.dll:9453](../agentbox/compatibility-audit/decompiled/06-AxivornsExpieNPCMod-ee7694e2/AxivornsExpieNPCMod.decompiled.cs:9453), [ShootManager.cs:160](../NewFirearms/ShootManager.cs:160), [ShootManager.cs:169](../NewFirearms/ShootManager.cs:169).

### B05 — Native GunScript weapons added by other mods lose parts of their gun interface

**Impact:** High/medium: reload, safety and self-harm failures; New Descents and LEWD Armory provide concrete affected weapons.

**Trigger:** Use an unconverted GunScript weapon such as thornslinger or arm_compact/arm_mp9 alongside NewFirearms.

Three distinct mismatches share this component boundary: (1) standard smallmagazine/riflemagazine/boxof12gauge instances become RshMag and lose AmmoScript, while these weapons still load through GunScript.LoadMag(AmmoScript), so standard-magazine reloads cannot take those items; custom .45/other Armory ammunition is not automatically included in this claim. (2) NewFirearms globally replaces PlayerCamera.GunToggleSafety and returns false even when the held weapon has no RshGun, suppressing the native safety action without a fallback. (3) without CatPatch, NewFirearms selects any gun-tagged item for its own self-harm coroutine and eventually dereferences GetComponent<RshGun>().prop; a native mod gun has no RshGun. These failures do not require that the native weapons themselves be converted.

**Evidence:** [New Descents.dll:19274](<../agentbox/compatibility-audit/decompiled/32-New Descents-2dba4590/New Descents.decompiled.cs:19274>), [New Descents.dll:19280](<../agentbox/compatibility-audit/decompiled/32-New Descents-2dba4590/New Descents.decompiled.cs:19280>), [LewdStatus.dll:1939](../agentbox/compatibility-audit/decompiled/28-LewdStatus-a14a617c/LewdStatus.decompiled.cs:1939), [LewdStatus.dll:2361](../agentbox/compatibility-audit/decompiled/28-LewdStatus-a14a617c/LewdStatus.decompiled.cs:2361), [CCLWorkaround.cs:13](../NewFirearms/CCLWorkaround.cs:13), [RshComponent.cs:19](../NewFirearms/RshComponent.cs:19), [PlayerCameraPatches.cs:196](../NewFirearms/PlayerCameraPatches.cs:196), [SelfHarmerPatch.cs:108](../NewFirearms/SelfHarmerPatch.cs:108).

### B06 — Prosthetics Steady Hand recoil reduction does not reach remote players’ host-authorized shots

**Impact:** Medium: multiplayer augment benefit absent or inconsistent.

**Trigger:** A client has a powered Steady Hand implant and fires a NewFirearms gun; the host is not independently mutating that same gun definition.

The NewFirearms bridge only finds PlayerCamera.main.body and edits its local AmmoInfo values. NewFirearms fires on the host via GunNetwork.ServerReceiver and uses the host’s local definition. No bridge message carries the client’s modifier to that shot, and its holding-arm lookup also only examines the local player. Thus the client’s local 50% mutation does not reduce the host-computed recoil/muzzle-rise sent back for that shot.

**Evidence:** [prosthetics.dll:36313](../agentbox/compatibility-audit/decompiled/37-prosthetics-1f440dc0/prosthetics.decompiled.cs:36313), [prosthetics.dll:36355](../agentbox/compatibility-audit/decompiled/37-prosthetics-1f440dc0/prosthetics.decompiled.cs:36355), [prosthetics.dll:42546](../agentbox/compatibility-audit/decompiled/37-prosthetics-1f440dc0/prosthetics.decompiled.cs:42546), [GunNetwork.cs:127](../NewFirearms/GunNetwork.cs:127), [RshGun.cs:204](../NewFirearms/RshGun.cs:204).

### B07 — Prosthetics Steady Hand changes shared gun definitions, rather than one gun instance

**Impact:** Medium: modifier leaks to another instance using that definition during the mutation window.

**Trigger:** A powered Steady Hand holds a gun while another instance with the same item ID fires on that process between the bridge’s Update and LateUpdate.

RshGun.prop is resolved directly from Plugin.GunDefinitions, so instances of an item ID share their AmmoInfo objects. Prosthetics writes knockback and muzzleRise on that shared object, then restores them in LateUpdate. It therefore changes all same-definition guns for that window, not just the augmented holder. In particular, host-side shots for another player can receive a reduction they did not earn if processed inside that window.

**Evidence:** [prosthetics.dll:36336](../agentbox/compatibility-audit/decompiled/37-prosthetics-1f440dc0/prosthetics.decompiled.cs:36336), [prosthetics.dll:36355](../agentbox/compatibility-audit/decompiled/37-prosthetics-1f440dc0/prosthetics.decompiled.cs:36355), [prosthetics.dll:36387](../agentbox/compatibility-audit/decompiled/37-prosthetics-1f440dc0/prosthetics.decompiled.cs:36387), [RshGun.cs:155](../NewFirearms/RshGun.cs:155), [Plugin.cs:29](../NewFirearms/Plugin.cs:29).

### B08 — Prosthetics can miss Steady Hand battery consumption after chamber rotation

**Impact:** Medium: shots can consume no augment charge.

**Trigger:** Fire a NewFirearms weapon which rotates directly from one live chamber to another before the bridge’s LateUpdate; leave another live round at rounds[0].

The bridge detects a shot solely as rounds[0] changing from nonnegative at Update to negative at LateUpdate. NewFirearms’ immediate RotateChambers path can leave both sampled values nonnegative. The recoil mutation can still be applied, but this detector never charges the implant for that shot. Multi-chamber/immediate-rotation weapons are affected; this is not a claim that every firing mode skips consumption.

**Evidence:** [prosthetics.dll:36342](../agentbox/compatibility-audit/decompiled/37-prosthetics-1f440dc0/prosthetics.decompiled.cs:36342), [prosthetics.dll:36365](../agentbox/compatibility-audit/decompiled/37-prosthetics-1f440dc0/prosthetics.decompiled.cs:36365), [RshGun.cs:274](../NewFirearms/RshGun.cs:274), [RshGun.cs:397](../NewFirearms/RshGun.cs:397).

### B09 — Prosthetics’ Deus trader-death exemption misses NewFirearms’ penalty

**Impact:** Medium: the augment’s death-penalty exemption is bypassed in single player.

**Trigger:** In single player, a Deus-enabled player kills a trader through NewFirearms.

Prosthetics snapshots and refunds the death-related happiness/trauma/sickness changes around TraderScript.OnWillRenderObject. NewFirearms applies its penalty in a first-priority TraderScript.Update prefix and sets didDeathMoodDebuff there. By the time the render callback is sampled, those values are already penalized, so that callback cannot observe or refund the NewFirearms penalty.

**Evidence:** [prosthetics.dll:24776](../agentbox/compatibility-audit/decompiled/37-prosthetics-1f440dc0/prosthetics.decompiled.cs:24776), [prosthetics.dll:24794](../agentbox/compatibility-audit/decompiled/37-prosthetics-1f440dc0/prosthetics.decompiled.cs:24794), [prosthetics.dll:24812](../agentbox/compatibility-audit/decompiled/37-prosthetics-1f440dc0/prosthetics.decompiled.cs:24812), [TraderDeathPatch.cs:8](../NewFirearms/TraderDeathPatch.cs:8).

### B10 — New Descents’ Gun crafting requirement excludes custom NewFirearms guns

**Impact:** Medium: eligible-looking custom guns cannot satisfy the thornslinger recipe.

**Trigger:** Try to satisfy New Descents’ gun crafting-quality ingredient using only a custom nf.* gun.

New Descents adds crafting quality gun (display name Gun) to four vanilla IDs, and its thornslinger recipe requires CraftingQuality("gun", 1f). NewFirearms custom registrations do not add that quality. The ordinary gun item tag does not satisfy this crafting-quality check. Converted vanilla IDs receiving New Descents’ edit are a separate case.

**Evidence:** [New Descents.dll:11359](<../agentbox/compatibility-audit/decompiled/32-New Descents-2dba4590/New Descents.decompiled.cs:11359>), [New Descents.dll:11418](<../agentbox/compatibility-audit/decompiled/32-New Descents-2dba4590/New Descents.decompiled.cs:11418>), [Plugin.cs:157](../NewFirearms/Plugin.cs:157), [ItemLoader.cs:385](../NewFirearms/ItemLoader.cs:385).

### B11 — LEWD Armory’s reload, ammo HUD and weapon commentary do not recognize RshGun

**Impact:** Medium: advertised Armory gun features disappear on converted vanilla and custom NewFirearms guns.

**Trigger:** Enable Armory’s ammo counter, reload shortcut or weapon commentary while holding a NewFirearms gun.

The HUD requires GetComponent<GunScript>(); the reload command explicitly returns "not a gun" without that component; its hotkey silently does nothing. The warning/shot/find lines hook GunScript.Update/Fire or require that component too. NewFirearms replaces it, so these are feature-level integration failures, rather than evidence that its own gun minigame cannot shoot.

**Evidence:** [LewdStatus.dll:1265](../agentbox/compatibility-audit/decompiled/28-LewdStatus-a14a617c/LewdStatus.decompiled.cs:1265), [LewdStatus.dll:1436](../agentbox/compatibility-audit/decompiled/28-LewdStatus-a14a617c/LewdStatus.decompiled.cs:1436), [LewdStatus.dll:1451](../agentbox/compatibility-audit/decompiled/28-LewdStatus-a14a617c/LewdStatus.decompiled.cs:1451), [LewdStatus.dll:5577](../agentbox/compatibility-audit/decompiled/28-LewdStatus-a14a617c/LewdStatus.decompiled.cs:5577), [LewdStatus.dll:5591](../agentbox/compatibility-audit/decompiled/28-LewdStatus-a14a617c/LewdStatus.decompiled.cs:5591), [LewdStatus.dll:5991](../agentbox/compatibility-audit/decompiled/28-LewdStatus-a14a617c/LewdStatus.decompiled.cs:5991), [RshComponent.cs:19](../NewFirearms/RshComponent.cs:19).

### B12 — LEWD Armory’s default surrender key also racks the minigame gun

**Impact:** Medium: one key press can surrender/accept a deal while operating a gun.

**Trigger:** Use the minigame’s Z racking shortcut while an Armory encounter/deal is eligible, with Armory’s default SurrenderKey=Z.

GunMinigame hardcodes Z for HandleMarksmanHotkey; Armory independently reads its Z surrender binding in its encounter update and calls Deal.Accept or Capture.Surrender. Neither handler consumes the key for the other. The conflict is conditional on an eligible encounter and the minigame shortcut’s usual UI/hover guards; rebinding Armory changes that condition.

**Evidence:** [LewdStatus.dll:1862](../agentbox/compatibility-audit/decompiled/28-LewdStatus-a14a617c/LewdStatus.decompiled.cs:1862), [LewdStatus.dll:4974](../agentbox/compatibility-audit/decompiled/28-LewdStatus-a14a617c/LewdStatus.decompiled.cs:4974), [MinigameManager.cs:1036](../GunMinigame/MinigameManager.cs:1036).

### B13 — Casualty Psychology’s firearm events miss NewFirearms, and magazine insertion can register as loss

**Impact:** Medium: missing firearm reactions and false negative item-loss events.

**Trigger:** Use a NewFirearms gun for dry firing/safety handling; or insert an already-tracked, non-consumable RshMag after the tracking spawn-protection period, outside treatment/crafting suppression windows.

Psychology’s firearm reaction hooks run on GunScript.Update, which is absent. Its ammo-consumed notification hooks AmmoScript.LoadRound, while NewFirearms inserts a magazine by copying its rounds and destroying the item. RshMag registrations use belttool rather than ammo/bullet tags, so the tracking rules can treat that magazine as valuable and non-consumable. The destroyed-item path then dispatches an item-lost event rather than recognizing a legitimate insertion. For instance, a tracked magazine bought from a trader can produce the traded-item loss memory. Ordinary bullet-tagged loose rounds are excluded from this loss claim.

**Evidence:** [CasualtyPsychology.dll:6551](../agentbox/compatibility-audit/decompiled/12-CasualtyPsychology-d089dea6/CasualtyPsychology.decompiled.cs:6551), [CasualtyPsychology.dll:11777](../agentbox/compatibility-audit/decompiled/12-CasualtyPsychology-d089dea6/CasualtyPsychology.decompiled.cs:11777), [CasualtyPsychology.dll:29211](../agentbox/compatibility-audit/decompiled/12-CasualtyPsychology-d089dea6/CasualtyPsychology.decompiled.cs:29211), [CasualtyPsychology.dll:29245](../agentbox/compatibility-audit/decompiled/12-CasualtyPsychology-d089dea6/CasualtyPsychology.decompiled.cs:29245), [CasualtyPsychology.dll:29282](../agentbox/compatibility-audit/decompiled/12-CasualtyPsychology-d089dea6/CasualtyPsychology.decompiled.cs:29282), [CasualtyPsychology.dll:29395](../agentbox/compatibility-audit/decompiled/12-CasualtyPsychology-d089dea6/CasualtyPsychology.decompiled.cs:29395), [RshGun.cs:439](../NewFirearms/RshGun.cs:439), [ItemLoader.cs:485](../NewFirearms/ItemLoader.cs:485).

### B14 — Whole Meal’s firearm suppression and stomach-wall damage context are bypassed

**Impact:** High for firearm escape mechanics; also bypasses its attack suppression rules.

**Trigger:** Fire an ordinary NewFirearms bullet while swallowed/in a Whole Meal stomach pocket, particularly during a state that suppresses attacks.

Whole Meal brackets GunScript.Fire to reject suppressed shots and enter its firearm AttackContext. NewFirearms shoots through RshGun/ShootManager, so neither hook runs. Its DamageBlock interception returns immediately when AttackContext.Active is false, preventing that ordinary shot from reaching the stomach-specific wall-damage/redirection logic. Ejected items also miss SharedPocketItemPatches.BeforeFire’s spawn tracking. This finding concerns the custom firearm path: grenade impacts still call the game’s CreateExplosion, which has its own Whole Meal hooks.

**Evidence:** [WholeMeal.dll:8217](../agentbox/compatibility-audit/decompiled/49-WholeMeal-d45514f6/WholeMeal.decompiled.cs:8217), [WholeMeal.dll:8254](../agentbox/compatibility-audit/decompiled/49-WholeMeal-d45514f6/WholeMeal.decompiled.cs:8254), [WholeMeal.dll:9883](../agentbox/compatibility-audit/decompiled/49-WholeMeal-d45514f6/WholeMeal.decompiled.cs:9883), [WholeMeal.dll:9918](../agentbox/compatibility-audit/decompiled/49-WholeMeal-d45514f6/WholeMeal.decompiled.cs:9918), [WholeMeal.dll:9931](../agentbox/compatibility-audit/decompiled/49-WholeMeal-d45514f6/WholeMeal.decompiled.cs:9931), [RshGun.cs:204](../NewFirearms/RshGun.cs:204), [ShootManager.cs:147](../NewFirearms/ShootManager.cs:147).

### B15 — ShrapnelVariants cannot assign its Bullet profile to NewFirearms bullet wounds

**Impact:** Low/medium: incorrect fragment count/category, not an extraction-minigame crash.

**Trigger:** A NewFirearms ordinary bullet produces a shrapnel wound.

ShrapnelVariants changes TurretScript.Shoot to record its Bullet profile (4–5 metal fragments), but it does not hook NewFirearms.ShootManager. NewFirearms directly assigns limb.shrapnel=5 without recording categories. Its extraction UI consequently fills missing categories using ChooseFallbackCategory instead of the Bullet profile. Explosive grenades use CreateExplosion and therefore follow the separate explosion hook.

**Evidence:** [ShrapnelVariants.dll:133](../agentbox/compatibility-audit/decompiled/43-ShrapnelVariants-be7895ef/ShrapnelVariants.decompiled.cs:133), [ShrapnelVariants.dll:336](../agentbox/compatibility-audit/decompiled/43-ShrapnelVariants-be7895ef/ShrapnelVariants.decompiled.cs:336), [ShrapnelVariants.dll:202](../agentbox/compatibility-audit/decompiled/43-ShrapnelVariants-be7895ef/ShrapnelVariants.decompiled.cs:202), [ShrapnelVariants.dll:253](../agentbox/compatibility-audit/decompiled/43-ShrapnelVariants-be7895ef/ShrapnelVariants.decompiled.cs:253), [ShootManager.cs:237](../NewFirearms/ShootManager.cs:237).

### B16 — Item Spawner lists custom NewFirearms items but cannot instantiate them

**Impact:** Medium: spawning custom guns, magazines and rounds fails.

**Trigger:** Select an nf.* item in Item Spawner and press spawn.

Item Spawner builds its catalog from Item.GlobalItems, which contains CUCoreLib registrations, then only uses Resources.Load<GameObject>(id) and Object.Instantiate for creation. Custom NewFirearms IDs are runtime registry templates, not game Resources entries; its null check logs "Resources.Load returned null" and returns false. Converted vanilla IDs can still resolve native resources. CUCoreLib’s Utils.Create interception is not used by this spawner path.

**Evidence:** [ItemSpawner.build.dll:1552](../agentbox/compatibility-audit/decompiled/25-ItemSpawner.build-40d05db2/ItemSpawner.build.decompiled.cs:1552), [ItemSpawner.build.dll:2099](../agentbox/compatibility-audit/decompiled/25-ItemSpawner.build-40d05db2/ItemSpawner.build.decompiled.cs:2099), [ItemSpawner.build.dll:2129](../agentbox/compatibility-audit/decompiled/25-ItemSpawner.build-40d05db2/ItemSpawner.build.decompiled.cs:2129), [Plugin.cs:157](../NewFirearms/Plugin.cs:157), [CUCoreLib UtilsCreatePatches.cs:27](../agentbox/CUCoreLib-main/Patches/UtilsCreatePatches.cs:27).

### B17 — RuScavMods’ included NewFirearms item translations target the old registration system and IDs

**Impact:** Medium: NewFirearms item names/descriptions are not translated by the supplied pack.

**Trigger:** Use the supplied RuScavMods pack with current NewFirearms 2.0 custom nf.* content.

Its item-translation integration patches RshLib.RshItemAdapter.RegisterItem and is enabled only when RshLib.dll/the old type is present. Current NewFirearms registers directly through CUCoreLib. Its supplied NewFirearms translation file additionally keys custom items as newfirearms.glock18, newfirearms.p90mag, etc., while current item IDs use nf.*. NewFirearms’ compatibilityMode only migrates its own Guns/Mags JSON, not another plugin’s translation files. This does not imply all RuScavMods locale strings are broken: unchanged run-setting/course keys have other translation routes.

**Evidence:** [RuScavMods.dll:91](../agentbox/compatibility-audit/decompiled/40-RuScavMods-8b0af18b/RuScavMods.decompiled.cs:91), [RuScavMods.dll:1818](../agentbox/compatibility-audit/decompiled/40-RuScavMods-8b0af18b/RuScavMods.decompiled.cs:1818), [RuScavMods.dll:1850](../agentbox/compatibility-audit/decompiled/40-RuScavMods-8b0af18b/RuScavMods.decompiled.cs:1850), [Plugin.cs:157](../NewFirearms/Plugin.cs:157), [supplied NewFirearms translation](../agentbox/compatibility-audit/unpacked/40-RuScavMods-378-2.1.6.1-2026-10-18T04-49Z-83voqPVi2/RuScavMods/Translations/com.rushellxyz.newfirearms.json:30).
