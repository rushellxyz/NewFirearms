`GunJson.feedType` uses the `FeedKind` enum. JSON accepts either a name or its original numeric value:

| Name | Legacy number | Feed mechanism |
| --- | --- | --- |
| `Magazine` | `1` | Detachable magazine |
| `Tube` | `2` | Internal tube |
| `Barrel` | `3` | Direct barrel loading |
| `Revolver` | `4` | Revolving chambers |
| `Battery` | `5` | Battery magazine |

For example, `"feedType": "Magazine"` and `"feedType": 1` select the same behavior. Names are case-insensitive. Existing numeric configs need no migration; the bundled gun configs now use names. The numeric enum values are explicit and must remain stable.

Unknown names fail JSON loading. Unknown numbers and a missing `feedType` fail the existing config validation. The existing error logging identifies the invalid definition. Saved item state and multiplayer packet layouts are unchanged.

For C# integrations, use `FeedKind` members instead of integer literals; the former `RshGun.BatteryFeed` constant is replaced by `FeedKind.Battery`.

## Open bolt

Set `openBolt: true` on a gun with `feedType: "Magazine"`. Pulling the handle locks the bolt open. Inserting a magazine leaves the bolt in its current position. An accepted trigger pull closes an open bolt, feeds one round and fires if ammunition is available. With an empty or missing magazine, the bolt closes and stays closed.

After each successful shot, the action opens again during the firing cycle. In single fire it stays open, including after the last round. Holding the trigger in full auto attempts the next shot, so an empty magazine closes the bolt and the handle must be pulled again. Releasing the trigger before that attempt leaves the bolt open.

The bundled Uzi starts with the bolt closed. In Together, the host controls the bolt and ammunition through the existing gun snapshots. Clients using the vanilla adapter see a closed-bolt approximation and require the mod to see the full open-bolt cycle.

## Marksman tube loading

Milky and characters with **Always Marksman** can drag a regular magazine or ammo box onto a tube-fed gun in the inventory. The same transfer works through the bark reload action when the gun is in the active hand and the magazine is in the other hand. Each action transfers the top round if the gun accepts that ammo type. An open, empty chamber fills first, followed by available tube slots. The source stays in the inventory. In Together, the host checks the acting player's Marksman status and synchronizes both items after a successful transfer.
