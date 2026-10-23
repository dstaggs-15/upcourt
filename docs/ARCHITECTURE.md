# Source architecture

Upcourt uses C# and Windows Forms with System.Drawing. The build deliberately follows Downfield's dependency-free Windows layout. Production and test entry points are separated by `UPCOURT_TESTS`; `Tests.cs` is excluded from the playable EXE.

| File | Responsibility |
|---|---|
| `Models.cs` | Persistent DTOs: skills, contracts, statistics, clubs, fixtures, series, drafts, plays and settings |
| `Match.cs` | Basketball state transitions, input actions, player movement, timing, rules, AI, substitutions and accelerated simulation |
| `Arcade.cs` | Jump height, spins, step-backs, cuts, alley-oop chains, power dunks, heat, knockback, referee visibility and flagrant/ejection accounting |
| `CourtCamera.cs` | Smooth world-space ball/velocity tracking, perspective relative to camera position and shot zoom |
| `BasketballPlaybook.cs` | Twenty-four named five-player route layouts |
| `League.cs` | Schedule, caps, transactions, development, CPU clubs, results, business, postseason, careers and offseason |
| `Persistence.cs` | Schema checks, XML serialization, atomic replacement and backup recovery |
| `Input.cs` | Keyboard edge state and XInput fallback/dead-zone polling |
| `ControlBindings.cs`, `ControlsUI.cs` | Saved keyboard/controller mappings, combo priority, capture, conflict swaps, resets and the live pause controller diagram |
| `Audio.cs`, `CrowdRecordings.cs`, `StadiumMixer.cs`, `ArcadeSounds.cs`, `WaveOutput.cs` | One continuous stereo PCM stream with real recorded crowd bed/reactions, short court effects and Downfield's adapted native Windows output buffers |
| `GameForm.cs` | Mode/navigation orchestration, action menus, settings, input routing and save cadence |
| `Rendering.cs` | Logical 1280×800 pixel scenes on a 640×400 backing frame; perspective arena, HUD and menus |
| `PixelArt.cs` | Original Downfield glyphs and basketball adaptation of its procedural sprite renderer |
| `PlayDesigner.cs` | Five-position route editor, custom play lifecycle and immediate use |
| `Program.cs` | Native game entry point and test-only commands |
| `Tests.cs` | Engine, league, persistence and UI integration checks |
| `ArcadeTests.cs` | Moving camera, moves/finishes, steals/blocks, both referee outcomes, ejection substitutions, compatibility and offline stereo-mixer checks |
| `ControlsAudioTests.cs` | Real recording hashes, silent routine-move cues, mapping capture/dispatch, swaps/resets, persistence and pause-menu UI flow |
| `installer/Installer.cs` | Per-user payload installer and desktop shortcut |

## Match lifecycle

`League.MakeMatch` clones the selected clubs' athletes, applies staff bonuses and assigns real starters/reserves. A match holds independent `Actor.Box` statistics. It does not mutate the career while the ball is moving.

`Match.Update` advances one bounded time step. Ball and game state live in serializable fields rather than opaque runtime handles. The random generator is reconstructed from a seed and call count for each roll, making a save/reload preserve subsequent draws without trying to serialize `System.Random` internals.

The live phases are inbound, live, pass, shot, loose, freethrow, break and final. Ball flight can survive the period horn. Free-throw completion precedes period transitions. Made baskets, turnovers and rebounds establish the next possession with the appropriate shot-clock reset.

Arcade state is part of the saved match and actors: jump/stun/action timers, move cooldowns, flagrant counts, ejections, heat/fire/combo arrays, referee position, called cuts, alley target and retained penalty possession. Transient sound events are excluded from XML to prevent replaying old sounds after resume. Old fifteen-key control arrays expand to the new defaults while retaining all existing customized keys.

An airborne shot retains its originating height and ends at rim height. Non-shot loose balls are marked separately so a punch-induced fumble does not create a fictional rebound. A caught punch uses the ordinary free-throw scoring path, then explicitly retains the victim's possession. Ejected actors are excluded from automatic lineup, bench rotation and simulated selection.

The camera works in court coordinates and applies the same projection to floor lines, hoops, actors, ball and shadows. It smooths toward the ball with a small velocity lead and adjusts zoom for airborne shots. Screen UI and crowd rows stay fixed. Court rendering is clipped to the play viewport; big-hit shake affects world geometry without shaking menus or scores.

The mixer combines ambience and short voices under a lock, renders bounded stereo PCM and feeds three reusable native Windows buffers on a background thread. Effects never replace the crowd bed. Master/crowd/effects gains are separate; focus loss silences output and pause ducks ambience. Following audio feedback, the synthetic crowd was replaced with the exact four CC0 WAV assets used by Downfield. Its quiet 35-second filtered/blended bed and faded recorded reactions are adapted in `CrowdRecordings.cs`. Audio resources are compiled into both the game and test assembly. Tests check their SHA-256 identities to prevent synthetic fallback. Routine jump/cut/pass/squeak cues are silent; remaining court effects are short fixed-frequency transients.

Controller gameplay uses saved action masks rather than hard-coded button branches. A matching LT/RT combination takes priority over its unmodified face/shoulder action, and release detection preserves hold/release shooting. Menu A/B, Menu pause and View mute remain fixed. Capture waits for selection buttons to be released and suppresses menu activation until a newly bound input is released. Keyboard conflicts swap across gameplay and utility keys. Diagram legends and live hints read the same settings as dispatch.

When a match ends, the form invokes `League.Finish` only if a league fixture exists. That method verifies identity/current round/unplayed state/untied final, settles the user fixture and simulates the other fixtures, then advances the league. `Match.ResultCommitted` also prevents repeated UI-side submission. The league's fixture guard remains the authoritative duplicate-result protection.

## Long-term state

`SaveData` contains the league, current match, settings, custom play library and contest record. Clubs have immutable IDs and mutable names/colors/business fields. Athletes have immutable IDs, mutable club ownership and separate season/career statistics. A club's lineup stores IDs, not list offsets. Historic season names are copied when archiving.

The schedule uses a circle-method twelve-team round robin and a reversed second leg. Postseason series point to higher/lower seeds and required wins. Each playoff round creates only the next games needed by incomplete series. Once all finals games resolve, archival marks the offseason. Contract/aging/draft processing transitions to a new schedule without deleting the coach or history.

XML writes are flushed to a temporary file, then `File.Replace` retains a backup. Load validates core version/identity/fixture invariants and tries the backup if the primary is invalid. Settings are included in the same snapshot so controls, gameplay preferences and a suspended match remain consistent.

## Art preservation

`PixelFont` is taken directly from Downfield. `PixelPlayers` retains its sprite size, caching, outline, skin palette, numbered jerseys and limb/pose geometry. Bare heads, exposed arms and lower legs replace football equipment. Rendering uses the original nearest-neighbor canvas and original menu/crowd palette. No generated raster assets or external art packages are required.

## Extension points

New offensive actions belong in `Match` and can use `Play.Style` and route points. Additional league capabilities should use stable IDs and review preconditions before mutating a club. New save fields should have safe defaults; incompatible model changes must increment and migrate the version rather than silently treating old data as current. Preserve the result-accounting and point-reconciliation tests when modifying rules.

Accelerated simulation is intentionally a distinct possession model. It creates coherent points, attempts, rebounds, assists and defensive events but does not replay every real-time movement. Compare aggregate distributions when tuning it rather than expecting a deterministic one-to-one recreation of a human match.
