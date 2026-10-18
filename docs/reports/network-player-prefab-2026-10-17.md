# Network player prefab and movement foundation

## Implemented

- `HarborlightNetworkSession` reads development launch flags for a dedicated server or direct client. The server binds to `127.0.0.1:7777` by default; `-harborlight-listen <IPv4>` opts into another bind address.
- `HarborlightNetworkPlayer` uses NGO owner-checked server RPCs for movement input, rejects malformed/out-of-range input, limits accepted updates, and stops movement when input goes stale.
- The server moves the player's `CharacterController`. NGO's server-authoritative `NetworkTransform` replicates position and rotation; a server-written network variable synchronizes the resident's locomotion blend value.
- A world interaction request is checked against the server's own nearby interactables and player distance, then its action is replicated back as an acknowledgment. It does not yet mutate job, economy, vehicle, or durable player state.
- The generated prefab reuses `Characters/HarborlightResident`, an original Harborlight humanoid asset with idle/walk animation, and has a nonzero NGO prefab identity.

## Validation evidence

Unity 6000.5.4f1 ran in low-priority batch mode with `HarborlightNetworkPlayerSetup.BuildNetworkPlayerPrefab`. The process exited `0`, compiled runtime and editor scripts with no C# errors, generated `Assets/Resources/Network/HarborlightNetworkPlayer.prefab`, and confirmed its NGO `GlobalObjectIdHash` as `2562172647`. The generated prefab contains a `CharacterController`, `NetworkObject`, server-authoritative `NetworkTransform`, Harborlight movement/network scripts, and the resident model prefab.

The source log for this run was `/tmp/Harborlight-Network-Prefab.log`; the command was:

```sh
nice -n 10 "/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit \
  -projectPath "/Users/admin/Desktop/RP game/harborlight-city" \
  -executeMethod Harborlight.Editor.HarborlightNetworkPlayerSetup.BuildNetworkPlayerPrefab \
  -logFile /tmp/Harborlight-Network-Prefab.log
```

## Still unverified

- No dedicated server or standalone client build has been produced from this change.
- No client has connected; movement, animation, interaction acknowledgment, disconnect, and reconnect have not been observed at runtime.
- There is no online session UI, account authentication, input prediction, durable online character state, multiplayer vehicle ownership, or server-authoritative mission/economy effect.
- The default loopback session is a development path, not an internet-ready or production-secure service.
