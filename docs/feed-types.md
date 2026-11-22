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

## Marksman tube loading

Milky and characters with **Always Marksman** can drag a regular magazine or ammo box onto a tube-fed gun in the inventory. The same transfer works through the bark reload action when the gun is in the active hand and the magazine is in the other hand. Each action transfers the top round if the gun accepts that ammo type. An open, empty chamber fills first, followed by available tube slots. The source stays in the inventory. In Together, the host checks the acting player's Marksman status and synchronizes both items after a successful transfer.
