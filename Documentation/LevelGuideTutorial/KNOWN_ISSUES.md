# Known issues and limitations

| ID | Issue | Impact | Needs |
|---|---|---|---|
| K1 | Routes come from physics probes on a 0.5 m grid (no NavMesh). Doors are ignored by name (any collider with "door" in itself or its two parents). | A closed door whose collider is not named "door" blocks the route. The arrows then lead to the closest reachable point, followed by a short straight hint. | Rename the door collider, or extend `GuideGridPath.Ignored`. |
| K2 | Each route search probes at most 2,600 new cells. Cells are cached, so later searches get further. | On a very long first route, the arrows may briefly stop early, then extend within a second or two. | Raise `MaxNewProbesPerSearch` if needed. Each probe costs about two physics queries. |
| K3 | Ground arrows use `Sprites/Default`, which is always included in builds. If it were missing, the trail disables itself with a warning. | None expected. | — |
| K4 | `type:` targets on 3D desk and corkboard objects use renderer bounds projected to the screen. | The spotlight may be slightly larger than the object on tilted cameras. | Point at a more specific child with `world:`. |
| K5 | Lessons that need `hud:prompt` show the spotlight only while the prompt is visible. | The box still explains; the hand appears once the player aims at the NPC. | — |
| K6 | The legacy `TutorialController` (empty `level1_tutorial.json`) is still in the scene and logs that it could not load steps. | Console noise only. | Remove it, or disable its auto-start, when cleaning up. |
| K7 | `rememberFinishedLessons` is off, so the tutorial replays every run of Level 1. | Intended for an intro level. Players can skip it. | Set it to true to remember finished lessons per device. |
| K8 | The guide speaks through the overlay. The in-world Tutorial Guide NPC's conversations (hardcoded in `TutorialNpcController`, speaker "Tutorial Guide") are unchanged. | The in-world NPC is named "Tutorial Guide" while the coach is "Arthur". | Optionally rename the speaker in `TutorialNpcController` to "Arthur" (a gameplay text change, not done). |
| K9 | Movement and look detection reads the player root position and camera yaw. | If a future camera mode moves the root without player input, a walking page could complete by itself. | — |
