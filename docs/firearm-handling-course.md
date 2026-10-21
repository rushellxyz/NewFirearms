# Firearm handling course

Select the existing Firearm handling course in the tutorial menu. Its layer requirement still follows `ignoreCourseLock` in the mod settings. Vanilla courses are only accessible in single-player.

## Stages

1. **First aid:** a controlled M1911 shot injures the left leg. The instructor arm hands over a medkit containing tweezers, ripped dressing, a bandage, antiseptic mush, a tourniquet, and an ice pack. Supplies are loaded inside its container before handover. Progress requires both zero shrapnel and less than 0.1 bleeding on that leg. There is no automatic healing or tutorial immortality, and the medkit is a one-time supply. Instructions remain in the tutorial dialogue area and are not repeated on a timer.
2. **Handling:** open the M1911 slide, insert a loaded magazine, close the slide, eject and replace the magazine, then load a loose round into an empty spare. Works with minigame interaction or inventory/legacy controls.
3. **Practice:** destroy three silhouette targets with each assigned weapon: M1911, MP5, and Saiga. Each drill requires its own gun. Then clear three mixed wallbiter/young-thornback waves of two, three, and four enemies. Loaded magazines and loose ammo are replenished during practice. Wounds and other medical consequences carry between drills and waves.
4. **Survival:** return to the sandbox start with a fresh set of all three weapons, eight full magazines and sixty loose rounds per weapon. Training stock and medical supplies are removed. The player receives the course's only full heal immediately before survival starts. No ammunition is replenished; course enemies have no loot drops. Waves grow and accelerate without a wave limit. At most sixteen enemies remain active; new waves replace the oldest survivors when that cap is reached. All wave enemies spawn stunned for five seconds. Death or leaving the course finishes the run; walking outside the old arena bounds does not end it.

Instructions use the tutorial speaker, without an extra HUD. A results panel appears only after survival ends. Leave through the normal game menu. Survival measures unpaused real time, independent of game speed. The personal best is stored locally under `NewFirearms.FirearmHandling.BestSurvivalSeconds` in Unity PlayerPrefs. Results survive returning to the menu. Course-owned objects and temporary tutorial overrides are cleaned up on exit.

## In-game validation checklist

- Start without optional integrations. Repeat with minigame enabled and disabled.
- Confirm first aid does not advance with either bleeding or fragments remaining. Complete both treatments; verify progression, no repeated reminder lines, and no automatic healing.
- Perform each weapon-handling action. Confirm the empty-magazine exercise is reachable through both inventory and minigame controls.
- Shoot practice targets with the assigned gun and a different gun. Only the assigned weapon should damage them. Check target placement, visibility, and the three drill transitions.
- Empty magazines and exhaust loose ammo during practice. Verify new stock appears in the appropriate wearable container or on the ground if storage is full. Check each of the three live waves can be reached and cleared.
- Confirm wounds persist through training and between waves. At survival start, verify the single full heal, then verify medical supplies and previous training stock disappear. Check health is no longer restored and ammo stock never refills, including after enemy deaths.
- Verify newly spawned enemies cannot bite during their five-second stun. Let the population reach sixteen and confirm later waves still arrive.
- Pause during survival; check the timer stops. Change game speed; the score should still follow real time.
- Die and inspect the result overlay. Repeat by leaving through the normal game menu. Walk beyond the former arena bounds and verify the run continues. Confirm the personal best survives a game restart and cannot decrease after a shorter run.
- Exit during each lesson and start another course. Confirm no leftover targets, hostile waves, HUD, or forced medical assistance remains.

Build validation cannot exercise Unity physics, medical minigames, prefab placement, or combat pacing. Those checks require the game.
