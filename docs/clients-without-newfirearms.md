# Clients without NewFirearms

The host can run NewFirearms while a client runs Together and CUCoreLib without
NewFirearms or GunMinigame. CUCoreLib sends a native `GunProperties` definition;
the client gets the ordinary gun interface and `GunScript`, not the minigame.
Both sides must use mutually compatible CUCoreLib and Together builds.
If the required Together receiver hooks are unavailable, the host disables this
compatibility path with a warning instead of exposing a second gun simulation.

On the host, a disabled `GunScript` mirrors each `RshGun` for Together's standard
item synchronization. It never runs a second gun simulation. Standard client
shot, rack, safety and magazine-ejection requests are routed into `RshGun`.
The host checks the player, held item, consciousness, shot origin and cooldown.
It spends the authoritative ammunition and traces damage itself; native client
hit claims do not determine damage. A single early shot can wait for the host's
cycle to finish, without increasing the fire rate. Native shot relays provide
visuals to other clients; peers with NewFirearms keep the custom visual path.

Supported interactions:

- Shooting with an already loaded gun, including ordinary automatic fire.
- Opening/closing the action, clearing a jam with the rack action, safety, and
  ejecting a magazine. Ejected items are offered to the acting player's body.
- Inserting compatible original-game magazines and rounds using the inventory.
- Loading custom magazines/rounds with the normal **bark** action (B by default,
  using the player's existing binding). Hold the destination gun or magazine in
  the leading hand and the donor magazine/round in the other hand, then bark.
  One accepted bark performs one compatible load on the host. Full, incompatible
  or unready targets consume nothing. Ordinary barking still plays normally.
- The original magazine prefabs expose their authoritative ammunition count
  through `AmmoScript`. Their contents remain owned by `RshMag` on the host.
- Original-game magazine definitions are omitted from CUCoreLib's item snapshot
  so a client without the mod retains its native unload action. Using a magazine
  predicts the count locally and asks the host to remove and give one actual round.
  Host-side magazine definitions are retained; modded peers use their local ones.
- Guns, magazines and ammunition still use the host's NewFirearms definitions
  for damage, capacities and accepted item IDs.

Limits of the current CUCoreLib/native protocol:

- Custom magazines and custom ammunition do not acquire `AmmoScript` on a client
  that lacks the mod. Use the two-hand bark action to load them; native inventory
  dragging still cannot recognize those pairs.
  Custom magazines also have no transferable native unload action; for example,
  `nf.smgmag` must currently be unloaded by the host or a modded player.
- There is no minigame, ammunition selector, custom fire-mode selector, laser or
  flashlight control. Native safety restores the definition's highest firing
  mode. Three-round burst falls back to one shot per native trigger press.
- The native definition uses the first ammo profile for prediction, a default
  compatible magazine sprite and CUCoreLib's supported sprite/parameter fields.
  Actual loaded ammo, damage, jams and ejected items are determined by the host.
  Exact per-magazine art, custom sounds, muzzle rise and special weapon controls
  are not fully represented by the native UI/protocol.
- CUCoreLib builds native gun templates from the pistol prefab. NewFirearms
  hides that prefab's inherited `Laser` child on the host and modded peers,
  preserving its own configured laser. A client running only CUCoreLib needs
  equivalent template cleanup in CUCoreLib; the host cannot change that local
  child's visibility through the current gun-definition protocol.
- This is not support for a client without CUCoreLib or Together.

Update the host plugin and restart the host to use the complete implementation.
When testing against a host patched at runtime, reconnect the client: an item
created from an earlier non-gun template does not automatically gain native gun
components just because its registry definition changed.
If a client already received replacement definitions with missing vanilla
magazine actions, fully restart that client after updating the host. Reconnecting
alone may retain the broken registry entries in memory.

Validation on 2026-11-16: host plus `client1` without NewFirearms, Together 5.5.2
and CUCoreLib 1.1. Live MCP checks and player feedback confirmed the MP5's native
UI, rack/safety, shooting with host-authoritative damage (a spider was killed),
and inventory insertion of `smallmagazine`. A source-linked handler test using
the real LiteNetLib reader passed 37 checks for ownership, malformed packets,
native fallback, cooldown/queue behavior, actor preservation and suppression of
automatic rack acknowledgements/duplicate native visuals. Other weapon
families and optional integrations still need in-game coverage.
The subsequent magazine-action fix was verified live with `smallmagazine` on
the client without NewFirearms and with nine snapshot/Harmony checks. It does
not add a custom-magazine action to CUCoreLib.
