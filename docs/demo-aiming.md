# Demo-style aiming for NewFirearms

Custom `RshGun` weapons use the aiming behavior from `/home/rushell/Documents/CU2`: the arms animator aims from the torso with `Mathf.Lerp(current, target, Time.deltaTime * 8 * armPowerMult)`, without the release's added `bonusRot` term. The gun retains `Item.Stats.slotRotation` in its inventory slot so the animated hand controls its world orientation.

`GunAnimationPatch` targets only the gun-angle interpolation and direct gun world-rotation setter inside `Body.HandleVisuals`, while retaining its existing null-safe GunScript-transform fallback. Native GunScript weapons keep the release's original interpolation parameters and direct aiming. No projectile, recoil impulse, inventory, or multiplayer-authority rules change.

The release-only aiming block is detected from its GunScript transform lookup. In the CU2 demo that block is absent, so the transpiler leaves all instructions unchanged. Validation runs the actual transpiler against both game DLLs (three targeted calls in the release, zero in the demo) and checks nine formula/pose cases including both facing directions, arm strength, and vanilla fallbacks. Live feel and multiplayer visual behavior still require gameplay validation.

Piggyback is an exception to demo-style animated aiming: while Together reports that the non-standing body is riding another player, RshGun keeps native direct gun aiming and steers the simulated ragdoll hand/wrist/arm chain. Dismounting restores demo aiming. Rider lookup uses the body's NetBody component, with the optional Together reference isolated behind NoInlining; missing limbs and non-simulated rigidbodies are skipped. Offline availability was tested without the Together assembly. Live host/client piggyback verification is still required.
