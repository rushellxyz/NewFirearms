# Gun minigame inertia

All guns shown in the minigame use a rigid two-anchor model: the supporting hand/grip and the muzzle. They share one translation and rotation; their separation never changes, so sprites, magazines, hit areas and casing spawn positions follow the same intact weapon.

Movement enters through the grip. Changes in body velocity produce lag during acceleration, braking, jumping, landing and external impacts. Relative hand-limb velocity and body angular velocity include physics motion during ragdoll. Footsteps add grip impulses. Crouching shifts the hand's resting support smoothly using the game's crouch amount, instead of calling the shooting recoil effect. Ammunition/magazine insertion also acts at the grip.

Pain tremor uses the same per-frame noise sample as the cursor hand: `averagePain`, resistance skill reduction, and the `minigamehandshake` run setting all apply. That sample also shifts the gun's hand-support target, so the rigid weapon shakes and tilts through its existing inertia instead of moving only one sprite layer. With no pain or the run setting at zero, no tremor is added and existing motion settles normally. A hand attached to the fore-end follows the weapon's tremor without slipping from its grab point.

Grabbing a fore-end/bolt attaches the visible hand strictly to the clicked point. The local grip position and relative hand rotation are preserved as the weapon and part move or rotate; the hand does not slip off when pulling at a stop. Its free-motion velocity is cleared on capture. While held, a separate smoothed cursor target drives the gesture and pulling force, independently of the visible hand. Releasing restores free movement from the hand's actual attached position and restores its normal orientation, without jumping to the cursor. The held hand remains visible when the pointer leaves the window.

Dragging a bolt creates a separate, temporary spring between the actual grabbed point and that cursor target. Forces at that point produce both translation and torque on the whole rigid gun. Sliding movement adds a small handling impulse, while stretching the grab continues to apply force even at the travel limits or with a locked Mosyn handle. Pulling in the other direction reverses the reaction. Releasing the bolt, opening another UI, hiding the minigame, or switching guns removes the spring and lets the normal supports settle the weapon.

Linear and rotating bolt gestures are evaluated in the moving gun's local space, so inertia does not turn into unintended pointer motion in screen coordinates. Rotating gestures use the separate cursor target and track from the current resting angle; their grabbed point follows the rotating handle. Mosyn handling includes vertical lift/lowering as well as horizontal travel. Existing reversed endpoint configs and rack-state thresholds remain supported. Final gun inertia and then the attached hand pose are applied in LateUpdate, after the hand target and bolt pose have updated.

Firing enters through the muzzle, using the existing ammo `knockback` and `muzzleRise`. That impulse creates translation and torque, with the hand's restoring force compensating afterward. Item weight affects recoil response; strength and consciousness affect grip support. There is no change to world firing physics, damage, aiming or multiplayer authority. Existing host/client shot notifications call the same `AddRecoil` API.

The solver integrates at steps no larger than 1/240 second, with damped springs returning the weapon to its resting pose. Translation and angle are bounded for extreme inputs. Pause freezes integration; closing the window, switching guns or respawning clears stale motion. Teleports and long stalls reset movement instead of injecting a large impact.

Existing configs use defaults. Optional minigame fields locate the two anchors on a sprite:

```json
"inertiaHandXPosition": -0.3,
"inertiaHandYPosition": -0.05,
"inertiaBarrelXPosition": 0.2,
"inertiaBarrelYPosition": 0.1
```

X is a fraction of minigame window width and Y a fraction of its height, measured from the center. Values range from -0.5 to 0.5; the anchors must be at least 0.05 apart in these normalized coordinates. They are not multiplied into the JSON runtime object, so reopening the window does not rescale them. `nf_visualize` marks both anchors for adjustment to individual weapon art. API-only adapters without an Item use unit mass.

The former `xInertia`/`rotInertia` API fields were removed. Call `AddHandlingImpulse(horizontal, vertical)` for hand-side disturbances or `AddRecoil(knockback, muzzleRise)` for muzzle-side impulses. The bundled plugin and minigame must be updated together.

Validation uses the production solver and Unity vector math outside the game: directional responses at each anchor, fixed anchor separation, return to rest, heavy/light recoil, braking, bounded sustained fire, pose reconstruction, sustained bolt overpull/release, point-contact torque, extreme pulls and comparison at 30/60/144 FPS. Live playtesting is still needed to tune the feel and individual anchor positions.
