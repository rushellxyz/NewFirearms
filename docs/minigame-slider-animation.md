Idle slider transitions use a short, frame-rate-independent interpolation instead of snapping between poses. Linear sliders and rotating sliders approach their new idle pose smoothly; Mosyn-style sliders also animate the handle lifting/lowering. A transition covers about 95% of its distance in 0.1 seconds and snaps the final tiny remainder to the exact configured pose.

Dragging a linear slider remains directly controlled by the pointer. Empty-magazine stops still constrain manual travel at `rackedToUnracked`. Opening animation may travel from the previously closed pose to that stop; the stop prevents dragging forward through it. `blockSlideWithEmptyMagazine` still blocks interaction while allowing the visual opening transition.

Automated rack and hand animations use smooth acceleration/deceleration over their existing durations. The back slider, chamber preview, and `backLayerFollowsSlider` layer follow the animated pose. Switching guns initializes the new gun directly in its own pose.

Animation does not change fire rate, magazine contents, or multiplayer packet layouts. Automatic cycling remains controlled by the gun/host; a manual charging-handle return requests closing only when its animated position crosses the existing closing threshold.
