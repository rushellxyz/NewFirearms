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
