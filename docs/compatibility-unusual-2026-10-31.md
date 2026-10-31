# Unusual findings from the supplied mod archives

Supplement to the [NewFirearms compatibility audit](compatibility-audit-2026-10-31.md), dated 2026-10-31. These observations are separate from confirmed conflicting code paths. They do not imply malicious behavior or runtime reproduction. BepInEx support assemblies and winhttp are excluded from review as requested.

## Archive versions and BepInPlugin versions disagree

These are the exact supplied binaries; the archive title is not a reliable substitute for the plugin’s declared version. Examples:

| Archive title | Declared BepInPlugin version |
| --- | --- |
| ModManager 1.1.12 | 0.2.0 |
| CPUOptimization / archive revision 12 | CPUOptimization2 0.8.36 |
| CasualtyPsychology 0.6.2 | 2.0.6.2 |
| CasualtyVitals 1.5.4 | 1.5.2 |
| Crafting Framework 1.3.0 | 1.2.1 |
| EnhancedTools 2.2.6 | 1.0.0 |
| Milkable Milkies 0.0.4 | 0.0.3 |
| ScavSetLib archive revision 3 | 1.0.1 |
| VforWhiningMPAddon archive revision 6 | 2.0.2 |

This is a packaging/identification observation, not proof of a bad build. The coverage CSV preserves both names and binary hashes.

Evidence: [Alexx__sModMgr.dll:34](../agentbox/compatibility-audit/decompiled/03-Alexx__sModMgr-07b1fffc/Alexx__sModMgr.decompiled.cs:34), [CPUOptimization2.dll:9191](../agentbox/compatibility-audit/decompiled/07-CPUOptimization2-b25139b1/CPUOptimization2.decompiled.cs:9191), [CasualtyPsychology.dll:121](../agentbox/compatibility-audit/decompiled/12-CasualtyPsychology-d089dea6/CasualtyPsychology.decompiled.cs:121), [CasualtyVitals.dll:16330](../agentbox/compatibility-audit/decompiled/13-CasualtyVitals-d1f34534/CasualtyVitals.decompiled.cs:16330), [CraftingFramework.dll:55](../agentbox/compatibility-audit/decompiled/16-CraftingFramework-57ac161f/CraftingFramework.decompiled.cs:55), [EnhancedTools.dll:5102](../agentbox/compatibility-audit/decompiled/21-EnhancedTools-e8f9dbf9/EnhancedTools.decompiled.cs:5102), [MilkiesMilk.dll:659](../agentbox/compatibility-audit/decompiled/29-MilkiesMilk-bf374194/MilkiesMilk.decompiled.cs:659), [ScavSetLib.dll:91](../agentbox/compatibility-audit/decompiled/41-ScavSetLib-c5822431/ScavSetLib.decompiled.cs:91), [VforWhiningMPAddon.dll:58](../agentbox/compatibility-audit/decompiled/48-VforWhiningMPAddon-bf61f846/VforWhiningMPAddon.decompiled.cs:58).

## ModManager also contains a separate old Build Mode plugin

The ModManager archive includes `modloader/BepInEx/plugins/BuildMod.dll`, declaring `com.alexx_.buildmode`, Build Mode 0.0.1. That is not the manager DLL and is not the same binary as the separately downloaded buildmod 1.0.28 (`com.alexx_.buildmod`). Extracting the bundled loader’s plugin directory therefore introduces an extra plugin the archive’s title does not describe. Loader/core DLL internals are excluded from this audit as requested.

Evidence: [archive inventory](../agentbox/compatibility-audit/archives.json), [old BuildMod declaration](../agentbox/compatibility-audit/decompiled/03-BuildMod-492ca08a/BuildMod.decompiled.cs:32), [BuildMod.dll:34](../agentbox/compatibility-audit/decompiled/04-BuildMod-32e41743/BuildMod.decompiled.cs:34).

## Two API archives contain the identical DLL; two sprite replacers share one plugin GUID

Galaxy Gray Technology Company and ICEnecoCustomItemAPI contain byte-identical `ICEnecoCustomItemAPI.dll` (SHA-256 starts `89d10489`). Those are one reviewed assembly with two archive origins. Multiplayer Sprite Replacer 1.2.0 and 1.3.0, on the other hand, contain different binaries but declare the same BepInEx GUID, `noah.casualtiesunknown.multiplayerspritereplacer`. They are alternative versions, not two independent plugins that can both register under that identity. The later one also declares a hard `CasualtiesMP` dependency.

Evidence: [hash/duplicate manifest](../agentbox/compatibility-audit/assemblies.json), [MultiplayerSpriteReplacer.dll:947](../agentbox/compatibility-audit/decompiled/30-MultiplayerSpriteReplacer-6dcbd871/MultiplayerSpriteReplacer.decompiled.cs:947), [MultiplayerSpriteReplacer.dll:1899](../agentbox/compatibility-audit/decompiled/31-MultiplayerSpriteReplacer-6b5a0a27/MultiplayerSpriteReplacer.decompiled.cs:1899).

## The archive named DoNotUse is an old-GUID multiplayer optimizer

Its DLL is `KrokMPOptimization2.dll`, declaring version 2.2.53 and a hard dependency on `KrokoshaCasualtiesMP`. Current NewFirearms detects `CasualtiesMP`, and the current local Together build uses that newer identity. A setup containing only the newer GUID does not satisfy this archive’s hard dependency, so BepInEx can reject it before gameplay; this is a loader-version mismatch rather than an NF gun-code conflict. Several other supplied plugins still use the old identity as a soft dependency, which by itself is not sufficient evidence that their optional integration fails.

Evidence: [KrokMPOptimization2.dll:12825](../agentbox/compatibility-audit/decompiled/20-KrokMPOptimization2-131d90fd/KrokMPOptimization2.decompiled.cs:12825), [Plugin.cs:42](../NewFirearms/Plugin.cs:42).

## Medical Brutalism has report and speedrun upload clients

The feedback/bug-report code uploads to embedded Discord webhook endpoints. The bug-report branch attaches selected images, the game/BepInEx log and a PlayerPrefs dump, subject to an 8 MiB attachment budget. A separate speedrun submission includes screenshots, challenge timing and a plugin manifest. These paths are attached to report/submission UI flows; no automatic background exfiltration is asserted here. No upload was executed during the audit. Endpoint tokens are deliberately not reproduced in this document.

Evidence: [Alexx__sMedicalBrutalism.dll:14647](../agentbox/compatibility-audit/decompiled/02-Alexx__sMedicalBrutalism-aefb8fd5/Alexx__sMedicalBrutalism.decompiled.cs:14647), [Alexx__sMedicalBrutalism.dll:14671](../agentbox/compatibility-audit/decompiled/02-Alexx__sMedicalBrutalism-aefb8fd5/Alexx__sMedicalBrutalism.decompiled.cs:14671), [Alexx__sMedicalBrutalism.dll:14746](../agentbox/compatibility-audit/decompiled/02-Alexx__sMedicalBrutalism-aefb8fd5/Alexx__sMedicalBrutalism.decompiled.cs:14746), [Alexx__sMedicalBrutalism.dll:44202](../agentbox/compatibility-audit/decompiled/02-Alexx__sMedicalBrutalism-aefb8fd5/Alexx__sMedicalBrutalism.decompiled.cs:44202), [Alexx__sMedicalBrutalism.dll:44219](../agentbox/compatibility-audit/decompiled/02-Alexx__sMedicalBrutalism-aefb8fd5/Alexx__sMedicalBrutalism.decompiled.cs:44219).

## Axivorn NPCs can download and stage their own updates

The NPC assembly includes an update manifest client, WebClient string/file downloads, staging/replacement file operations and a Process.Start path for an external helper. This goes beyond an NPC behavior DLL. The review only inspected the implementation: it did not contact its manifest host, download an update or launch the helper. Prosthetics, CuSaveManager, Custom Structures and QoL also contain update-check/UI code, but their existence alone does not establish a compatibility failure.

Evidence: [AxivornsExpieNPCMod.dll:14621](../agentbox/compatibility-audit/decompiled/06-AxivornsExpieNPCMod-ee7694e2/AxivornsExpieNPCMod.decompiled.cs:14621), [AxivornsExpieNPCMod.dll:14645](../agentbox/compatibility-audit/decompiled/06-AxivornsExpieNPCMod-ee7694e2/AxivornsExpieNPCMod.decompiled.cs:14645), [AxivornsExpieNPCMod.dll:14701](../agentbox/compatibility-audit/decompiled/06-AxivornsExpieNPCMod-ee7694e2/AxivornsExpieNPCMod.decompiled.cs:14701), [AxivornsExpieNPCMod.dll:14928](../agentbox/compatibility-audit/decompiled/06-AxivornsExpieNPCMod-ee7694e2/AxivornsExpieNPCMod.decompiled.cs:14928), [prosthetics.dll:36858](../agentbox/compatibility-audit/decompiled/37-prosthetics-1f440dc0/prosthetics.decompiled.cs:36858), [CuSaveManager.dll:1682](../agentbox/compatibility-audit/decompiled/17-CuSaveManager-c97db507/CuSaveManager.decompiled.cs:1682), [Custom Structures.dll:20669](<../agentbox/compatibility-audit/decompiled/18-Custom Structures-a24826af/Custom Structures.decompiled.cs:20669>), [QoL Unknown.dll:3775](<../agentbox/compatibility-audit/decompiled/38-QoL Unknown-3b697b32/QoL Unknown.decompiled.cs:3775>).

## CPUOptimization2 retains a development-machine debug-log path

SpawnCensus.Ndjson appends structured debugging messages with a fixed session ID to `c:\Users\Owner\Documents\casualties-together-source\.cursor\debug-91c41d.log`. Write failures are swallowed. This is a concrete development-artifact residue; the path is not evidence of an outgoing network request or proof of who authored the code.

Evidence: [CPUOptimization2.dll:20271](../agentbox/compatibility-audit/decompiled/07-CPUOptimization2-b25139b1/CPUOptimization2.decompiled.cs:20271).

## The two optimization mods use substantially different storage strategies

CPUOptimization2 captures foreign components using a hand-written scalar field whitelist and can destroy/recreate objects; this is the cause of B01. In the DoNotUse/KrokMPOptimization2 build, the active BagContents.SleepItem path usually deactivates the actual item in its container. There are additionally parked-object, snapshot and saveable-component paths. Reading only its GunScript/AmmoScript snapshot helpers would incorrectly imply that every container operation loses NF data. The inactive-object design is still a substantial change to gameplay semantics, but the helpers’ mere absence of RshGun-specific code was not counted as a demonstrated data-loss finding.

Evidence: [CPUOptimization2.dll:2282](../agentbox/compatibility-audit/decompiled/07-CPUOptimization2-b25139b1/CPUOptimization2.decompiled.cs:2282), [CPUOptimization2.dll:2877](../agentbox/compatibility-audit/decompiled/07-CPUOptimization2-b25139b1/CPUOptimization2.decompiled.cs:2877), [KrokMPOptimization2.dll:15017](../agentbox/compatibility-audit/decompiled/20-KrokMPOptimization2-131d90fd/KrokMPOptimization2.decompiled.cs:15017), [KrokMPOptimization2.dll:15682](../agentbox/compatibility-audit/decompiled/20-KrokMPOptimization2-131d90fd/KrokMPOptimization2.decompiled.cs:15682), [KrokMPOptimization2.dll:16136](../agentbox/compatibility-audit/decompiled/20-KrokMPOptimization2-131d90fd/KrokMPOptimization2.decompiled.cs:16136).

## LEWD’s single DLL contains three BepInEx plugins and a native gun system

`LewdStatus.dll` declares separate Russian-localization, Armory and main-plugin entries. Its Armory defines its own weapons, resource templates, calibers, magazine conversion, reload animation, NPC equipment, ammo HUD and input bindings. It is not only a visual/status-content pack; those native GunScript/AmmoScript assumptions account for multiple findings in the main report. Its default surrender action is Z, independently of the reload key’s bind-conflict resolver.

Evidence: [LewdStatus.dll:410](../agentbox/compatibility-audit/decompiled/28-LewdStatus-a14a617c/LewdStatus.decompiled.cs:410), [LewdStatus.dll:1631](../agentbox/compatibility-audit/decompiled/28-LewdStatus-a14a617c/LewdStatus.decompiled.cs:1631), [LewdStatus.dll:27592](../agentbox/compatibility-audit/decompiled/28-LewdStatus-a14a617c/LewdStatus.decompiled.cs:27592), [LewdStatus.dll:1939](../agentbox/compatibility-audit/decompiled/28-LewdStatus-a14a617c/LewdStatus.decompiled.cs:1939), [LewdStatus.dll:2555](../agentbox/compatibility-audit/decompiled/28-LewdStatus-a14a617c/LewdStatus.decompiled.cs:2555), [LewdStatus.dll:1862](../agentbox/compatibility-audit/decompiled/28-LewdStatus-a14a617c/LewdStatus.decompiled.cs:1862), [LewdStatus.dll:5543](../agentbox/compatibility-audit/decompiled/28-LewdStatus-a14a617c/LewdStatus.decompiled.cs:5543).

## Galaxy items include negative encumbrance and extreme medical values

The Galaxy belt’s JSON sets `encumberanceMult` to `-1` and both container weight limits to `9999`. Its Galaxy bandage config specifies `bandageConditionConsumption: 1000` and multiple healing/bleeding/pain-related values of `80000`. These are supplied content values, not measured treatment outcomes. They affect the balance assumptions of any medical/firearm test using those items, without by themselves demonstrating a NewFirearms code conflict.

Evidence: [galaxybelt/item.json](../agentbox/compatibility-audit/unpacked/23-Galaxy-Gray-Technology-Company-70-2.6-2026-08-29T15-01Z-5ZrIboe4J/Galaxy%20Gray%20Technology%20Company/Items/galaxybelt/item.json), [galaxybandage/item.json](../agentbox/compatibility-audit/unpacked/23-Galaxy-Gray-Technology-Company-70-2.6-2026-08-29T15-01Z-5ZrIboe4J/Galaxy%20Gray%20Technology%20Company/Items/galaxybandage/item.json).

## KrokMP Chinese Supplement stores Windows separators literally in its ZIP

All four archive entries use backslashes in their names. The Linux extractor preserved these as literal filename characters, including `KrokMPChineseSupplement\KrokMPChineseSupplement.dll`; the manifest and evidence paths retain that spelling. That is a cross-platform packaging oddity, not an assertion that normal Windows extraction breaks it. The assembly installs text/locale hooks at runtime rather than through the ordinary HarmonyPatch attributes, so an attribute-only patch inventory would miss its behavior.

Evidence: [archive inventory](../agentbox/compatibility-audit/archives.json), [KrokMPChineseSupplement\KrokMPChineseSupplement.dll:59](../agentbox/compatibility-audit/decompiled/27-KrokMPChineseSupplement\KrokMPChineseSupplement-3131852b/KrokMPChineseSupplement\KrokMPChineseSupplement.decompiled.cs:59).

## One of the 50 archives is translation data only

`wiki-translation-official-v1.8.3 ...zip` contains one Chinese JSON translation file and no managed assembly. It was reviewed as data, not silently omitted as a decompilation failure. There were no nested ZIP/RAR/7z archives in the extracted payloads.

Evidence: [coverage CSV](compatibility-coverage-2026-10-31.csv), [archive inventory](../agentbox/compatibility-audit/archives.json).

## CasualtiesExtra/Stable has a debug course takeover

The archive titled Stable declares `CasualtiesExtra` version 1.0. With its `debug` option explicitly set to the string `True`, its PreRun.Start hook immediately enters tutorial and its TutorialHandler.Start hook starts SandboxCourse, hides course selection and clears forceWalk. This prevents ordinary course selection under that debug configuration, including the NewFirearms course. It is an opt-in debug override rather than a default collision between course registrations.

Evidence: [CasualtiesExtra.dll:261](../agentbox/compatibility-audit/decompiled/44-CasualtiesExtra-35f32d1c/CasualtiesExtra.decompiled.cs:261), [CasualtiesExtra.dll:272](../agentbox/compatibility-audit/decompiled/44-CasualtiesExtra-35f32d1c/CasualtiesExtra.decompiled.cs:272), [CasualtiesExtra.dll:285](../agentbox/compatibility-audit/decompiled/44-CasualtiesExtra-35f32d1c/CasualtiesExtra.decompiled.cs:285).

## Large mods include independent simulation and persistence subsystems

Whole Meal implements a separate physical stomach/pocket stage, attack context, lining damage, shared multiplayer sessions and item spawn tracking. Prosthetics includes a deliberate NewFirearms reflection bridge rather than only patching native GunScript. Medical Brutalism adds its own courses, run records, extra physiological systems and submission UI. Their decompiled source sizes are approximately 29k, 50k and 44k lines respectively; file size and overlapping patch targets were not used as proof of incompatibility.

Evidence: [WholeMeal.dll:9883](../agentbox/compatibility-audit/decompiled/49-WholeMeal-d45514f6/WholeMeal.decompiled.cs:9883), [WholeMeal.dll:9764](../agentbox/compatibility-audit/decompiled/49-WholeMeal-d45514f6/WholeMeal.decompiled.cs:9764), [prosthetics.dll:36206](../agentbox/compatibility-audit/decompiled/37-prosthetics-1f440dc0/prosthetics.decompiled.cs:36206), [Alexx__sMedicalBrutalism.dll:28629](../agentbox/compatibility-audit/decompiled/02-Alexx__sMedicalBrutalism-aefb8fd5/Alexx__sMedicalBrutalism.decompiled.cs:28629).
