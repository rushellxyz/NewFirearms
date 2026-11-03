# Client gun positions

Client shoot requests (gun action 0) append the gun's world X/Y position. With `strictSync` enabled, the host keeps its own gun position and existing reach/obstruction checks. With it disabled, the host uses the reported gun position plus its usual `barrelOffset` to start the raycast or explosive projectile. Tracers use that same shot origin.

Only the origin comes from the client. Direction, spread, damage, ammunition, cooldown, and firing remain host-controlled, and the requesting player must still hold the gun. Non-finite positions are rejected.

During burst/automatic fire, the client refreshes the gun position at most every 0.05 seconds using action 8. These updates only change the origin of an already active firing sequence owned by that player; they cannot fire a shot, restart a burst, or extend it. The stored position clears when firing stops or a new trigger request starts. No position updates are needed in strict mode.

Older shoot requests without coordinates fall back to the host's position. The new automatic/burst position-update action requires the updated mod on the host and client.

Build validation and source-linked firing checks with stub gameplay cover strict/loose selection, muzzle offset, moving client origins, burst length, owner matching, and update throttling. Live multiplayer verification remains required.
