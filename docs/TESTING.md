# Verification

Verified locally on Windows on October 23, 2026 using the installed x64 .NET Framework compiler. Build and tests completed with exit code zero.

## Results

- **463 engine assertions, zero failures.**
- Fifteen menu/rendering paths passed.
- The UI gameplay flow passed: keyboard movement → hold/release shot meter → ball flight → pause → saved match reload.
- The arcade UI flow passed: jump → spin → step-back → called cut → punch → dunk through their actual key bindings.
- Camera checks verified smoothing, upcourt travel, reversal/lateral tracking and action framing; backcourt, frontcourt and dunk screenshots were inspected.
- Arcade checks covered dunk/alley scoring, jump height, move cooldowns, steals/reach-in fouls, one block credit per jump, both referee outcomes, two shots plus retained possession, second-flagrant ejection with a real reserve, and non-shot loose-ball statistics.
- Offline audio checks verified sustained stereo crowd, overlapping voices, bounded PCM, focus/mute behavior, pause ducking, and independent crowd/effects controls. Physical audio output still needs human listening tests.
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

## Hyper arcade release SHA-256

| Artifact | SHA-256 |
|---|---|
| `play me/Upcourt.exe` | `BAEC6A94ABFB2CBEC3901389BDC72175CF511FFB73E73CFA30DD254418C1C07C` |
| `play me/Install Upcourt.exe` | `33B411EA864177BCA80D52865E1BD35FCBF2FA7B295AD5EB7D8BB7B0897A3203` |
| `play me/Upcourt-Windows.zip` | `7C7E8C5D64D786BE7DFA5CC7B3E236B6224F2E8F0C1D33798667DE64D5D259FE` |

Rebuilding can change compiler timestamps and ZIP metadata, so future builds may have different hashes. `verify-release.ps1` checks the artifacts against the current local build instead of hard-coding this historical table.
