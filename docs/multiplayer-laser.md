# Laser synchronization

Laser switches send gun action 9 with the desired enabled state. The host validates the player and the existing reach/obstruction rules, applies the state only to weapons configured with `hasLaser`, and sends a reliable gun snapshot. Repeated requests for the same state do not toggle the laser back. Clients apply the flag to both `RshGun.laserEnabled` and the `LaserPointer` component, so the minigame indicator and world beam agree.

The flag is appended to the existing gun snapshot and included in periodic synchronization. Saved laser state remains supported. Older snapshots without the flag keep the saved/default state; older clients can ignore the extra snapshot byte. Client-controlled laser switching requires an updated host because action 9 is new.

The beam is drawn in world coordinates along the same facing-dependent direction used for its raycast, avoiding reversed beams on mirrored weapons. Each frame starts with the maximum beam range, so hits against only ignored colliders cannot leave a stale beam endpoint.
