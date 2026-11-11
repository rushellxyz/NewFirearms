Idle slider transitions use a short, frame-rate-independent interpolation instead of snapping between poses. Linear sliders and rotating sliders approach their new idle pose smoothly; Mosyn-style sliders also animate the handle lifting/lowering. A transition covers about 95% of its distance in 0.1 seconds and snaps the final tiny remainder to the exact configured pose.

Dragging a linear slider remains directly controlled by the pointer. Empty-magazine stops still constrain manual travel at `rackedToUnracked`. Opening animation may travel from the previously closed pose to that stop; the stop prevents dragging forward through it. `blockSlideWithEmptyMagazine` still blocks interaction while allowing the visual opening transition.

Automated rack and hand animations use smooth acceleration/deceleration over their existing durations. The back slider, chamber preview, and `backLayerFollowsSlider` layer follow the animated pose. Switching guns initializes the new gun directly in its own pose.

Animation does not change fire rate, magazine contents, or multiplayer packet layouts. Automatic cycling remains controlled by the gun/host; a manual charging-handle return requests closing only when its animated position crosses the existing closing threshold.

Set `minigame.ejectMagazineOnFullRack` to `true` to eject the magazine when the slider reaches its fully rearward endpoint (`sliderMinimumPosition`). It defaults to `false`. This applies to manual pulls, marksman hotkeys, rotating bolts, and Mosin-style bolts; ordinary idle poses and firing animations do not eject magazines. The request is sent once while the slider stays at the rear, using the normal magazine-removal action and multiplayer authorization.

With `ejectMagazineOnFullRack` enabled, the marksman rack hotkey stops short of the full rear endpoint and keeps the magazine inserted. The unload hotkey grips the bolt and pulls it fully rearward to eject the magazine instead of reaching for the magazine itself. If the action is already open, unloading leaves it open. Without the option, both hotkey animations keep their existing behavior.
