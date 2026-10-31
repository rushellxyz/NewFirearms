Explosive ammo uses `FirearmExplosionParams`, which extends the game's `ExplosionParams`.
All existing explosion fields remain available. Two new fields control flight:

```json
"explosionParams": {
    "projectileVelocity": 40,
    "minimumTravelDistance": 5,
    "range": 12,
    "structuralDamage": 500
}
```

`projectileVelocity` is initial speed in world units per second (default 40). A dynamic `Rigidbody2D` provides movement and the game's 2D gravity, with zero drag. `minimumTravelDistance` measures distance along the flight path (default 0); it does not cause an automatic explosion when reached. An armed projectile explodes on impact. An earlier impact consumes it without an explosion. Flight expires after 30 seconds without exploding.

Explosive shots now travel instead of using the bullet's instant raycast. Swept collision checks cover the rigidbody’s actual movement each physics step to prevent fast projectiles passing through thin walls. The shooter is excluded from direct collisions; the resulting explosion still uses normal game damage rules. The inherited `velocity` field controls explosion force and remains separate from projectile speed.

In multiplayer the host controls flight, impact and explosion creation. A separate visual channel sends projectile positions and velocities to clients; clients never resolve projectile damage. The host's `settings.json` field `projectileSyncRate` sets the snapshot interval in seconds (default 0.1, finite and greater than zero), scheduled with `InvokeRepeating`. Spawn and removal messages are immediate. Replicas expire after 30 seconds without an update, so longer snapshot intervals do not prematurely remove them. Periodic snapshots also show projectiles to players who join during flight. Together's existing explosion synchronization distributes explosion effects and damage. Ordinary bullet packets are unchanged.

Validation to perform in game: fire horizontally and upward to check drop; impact a nearby wall inside the minimum distance and verify no explosion; hit a farther wall and verify one explosion; fire several differently configured explosive rounds and verify their explosion positions remain independent. Repeat from a client and observe flight and explosions from the host and another client.
