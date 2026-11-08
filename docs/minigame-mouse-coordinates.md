Run `nf_coordinate` in the game console to toggle the mouse-coordinate panel. Hold a gun with an active minigame and move the mouse to the desired point. The panel appears at the upper left of the canvas and does not intercept clicks. Run the same command again to hide it.

The first X/Y pair is in screen-width/screen-height fractions relative to the gun's center. Use it for `casingXPosition/YPosition`, `magazineXPosition/YPosition`, `coverXPosition/YPosition`, `barrelXPosition/YPosition`, or `rackRotationCenterXPosition/YPosition`.

The second pair is in minigame-window fractions. Use it for `inertiaHandXPosition/YPosition` and `inertiaBarrelXPosition/YPosition`. Positive X points right and positive Y points up in the gun's local space. Values are printed with decimal points so they can be copied into JSON independently of system locale.

For a linear slider, `slider X` uses screen-width fractions for the slider positions and transition thresholds. For `rackByRotation`, the panel instead shows the mouse angle around the slider's pivot in degrees. Rotating drag gestures track angle changes from the current handle pose; the displayed angle is the cursor's absolute direction around that pivot.

Coordinates account for the gun's actual translation and rotation after inertia and for the canvas projection. The panel hides when the minigame is closed or hidden. `nf_visualize` remains available independently to show configured reference points.
