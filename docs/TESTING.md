# Verification

Verified locally on Windows on October 23, 2026 using the installed x64 .NET Framework compiler. Build and tests completed with exit code zero.

## Results

- **492 engine assertions, zero failures.**
- Seventeen menu/rendering paths passed, including the controller diagram and Xbox binding editor.
- The UI gameplay flow passed: keyboard movement → hold/release shot meter → ball flight → pause → saved match reload.
- The arcade UI flow passed: jump → spin → step-back → called cut → punch → dunk through their actual key bindings.
- The controls UI flow passed: pause diagram → controller capture → conflict swap → shoot/release using the remapped controller state → keyboard capture → save/reload → return to pause. Hardware-free controller state injection is confined to the test build.
- Camera checks verified smoothing, upcourt travel, reversal/lateral tracking and action framing; backcourt, frontcourt and dunk screenshots were inspected.
- Arcade checks covered dunk/alley scoring, jump height, move cooldowns, steals/reach-in fouls, one block credit per jump, both referee outcomes, two shots plus retained possession, second-flagrant ejection with a real reserve, and non-shot loose-ball statistics.
- Offline audio checks verified sustained stereo crowd, overlapping voices, bounded PCM, focus/mute behavior, pause ducking, and independent crowd/effects controls. Physical audio output still needs human listening tests.
- All four embedded crowd WAVs match the SHA-256 hashes of the sourced Downfield recordings. The decoder returns the real sample data, the ambience is a 35-second recorded blend, and routine move/squeak cues no longer generate tones. These checks establish source and signal integrity, not a subjective listening review; Options includes a crowd preview for human review.
- Real-time CPU-versus-CPU match reached an untied final.
- Six consecutive seasons completed their regular schedules, series playoffs, contract processing and drafts while retaining legal club rosters/payrolls and valid lineup identities.
- A perfect three-point contest produced 30 points from 25 shots.
- Player scoring arithmetic reconciled with team totals in played and accelerated games.
- Corrupt primary XML recovered from the backup; saved live match state resumed deterministically for the tested update.
- The corrected-name build recovered an existing first-release career, saved it in the Upcourt folder, and retained the original copy.
- Fresh installation and reinstallation produced the exact expected game bytes.
- Release verification confirmed the portable and embedded installer payload match the tested build, the game contains its license, production excludes the test class, and the ZIP contains exactly the intended five entries.
- The final menu and court screenshots were generated from the actual application renderer and visually inspected. They are not mockups.

## Reproduce

```powershell
.\build.ps1
.\build\Upcourt.Tests.exe --ui-test
.\build\Upcourt.Tests.exe --screenshot docs\menu.png
.\build\Upcourt.Tests.exe --screenshot docs\court.png court
.\build-installer.ps1
.\verify-release.ps1
```

`build.ps1` performs the engine suite automatically. Tests redirect persistence into unique temporary directories, so they do not overwrite the player's career. UI automation creates its own temporary profile as well. Installer probes use explicit temporary destinations and do not create desktop shortcuts.

The production EXE is compiled separately from the test EXE. Test mode, UI probes, screenshot commands and the `Upcourt.Tests` class are absent from the playable build. Screenshots intentionally use the test executable; their art and drawing code is the same production renderer.

GitHub Actions builds Windows artifacts on pushes and pull requests. A remote workflow result is separate from these local results; inspect the Actions tab for its actual status after upload.

## Coverage and limits

The tests cover schedule pairings/home balance, IDs, starter uniqueness, cap failures, signings/bench placement/releases, trade deadlines, training limits, staff caps, shot location/timing, clocks, timeout restrictions, substitutions, exhibition isolation, duplicate settlement, league-wide round advancement, playoffs, archives, drafts, repeated seasons, real-time AI termination, contest scoring, saves, controller dead-zone math, stock play shapes, rendering and a UI-to-save flow.

They do not replace extended human gameplay or physical Xbox/controller and audio-hardware testing. The UI input flow exercises managed form input state; it does not claim an external hardware keystroke injection test. Simulated games use an accelerated possession model rather than replaying every live action.

REA static analysis succeeded on the locally rebuilt Downfield source assembly. REA process capture could not run because the adapter explicitly does not support native Windows PTY cleanup verification. Normal Windows execution provided the runtime results above; this is documented in the retained evidence.

## Recorded-crowd and controls release SHA-256

| Artifact | SHA-256 |
|---|---|
| `play me/Upcourt.exe` | `0ADAE54CB68B46060BF103E42C152C5CE8A2919F865E58488DF423900CB63B7A` |
| `play me/Install Upcourt.exe` | `2323D3F50D2F296483F27E3ACCF5FA08DAA12D8E6847368EF0AB5819E9ECB6D4` |
| `play me/Upcourt-Windows.zip` | `379F4E3843D36B9B5CFB07D48FB1730A192C2542D8DD1EFC88A152CF939CF886` |

Rebuilding can change compiler timestamps and ZIP metadata, so future builds may have different hashes. `verify-release.ps1` checks the artifacts against the current local build instead of hard-coding this historical table.
