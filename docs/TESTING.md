# Verification

Verified locally on Windows on October 23, 2026 using the installed x64 .NET Framework compiler. Build and tests completed with exit code zero.

## Results

- **413 engine assertions, zero failures.**
- Fifteen menu/rendering paths passed.
- The UI gameplay flow passed: keyboard movement → hold/release shot meter → ball flight → pause → saved match reload.
- Real-time CPU-versus-CPU match reached an untied final.
- Six consecutive seasons completed their regular schedules, series playoffs, contract processing and drafts while retaining legal club rosters/payrolls and valid lineup identities.
- A perfect three-point contest produced 30 points from 25 shots.
- Player scoring arithmetic reconciled with team totals in played and accelerated games.
- Corrupt primary XML recovered from the backup; saved live match state resumed deterministically for the tested update.
- Fresh installation and reinstallation produced the exact expected game bytes.
- Release verification confirmed the portable and embedded installer payload match the tested build, the game contains its license, production excludes the test class, and the ZIP contains exactly the intended five entries.
- The final menu and court screenshots were generated from the actual application renderer and visually inspected. They are not mockups.

## Reproduce

```powershell
.\build.ps1
.\build\Upocourt.Tests.exe --ui-test
.\build\Upocourt.Tests.exe --screenshot docs\menu.png
.\build\Upocourt.Tests.exe --screenshot docs\court.png court
.\build-installer.ps1
.\verify-release.ps1
```

`build.ps1` performs the engine suite automatically. Tests redirect persistence into unique temporary directories, so they do not overwrite the player's career. UI automation creates its own temporary profile as well. Installer probes use explicit temporary destinations and do not create desktop shortcuts.

The production EXE is compiled separately from the test EXE. Test mode, UI probes, screenshot commands and the `Upocourt.Tests` class are absent from the playable build. Screenshots intentionally use the test executable; their art and drawing code is the same production renderer.

GitHub Actions builds Windows artifacts on pushes and pull requests. A remote workflow result is separate from these local results; inspect the Actions tab for its actual status after upload.

## Coverage and limits

The tests cover schedule pairings/home balance, IDs, starter uniqueness, cap failures, signings/bench placement/releases, trade deadlines, training limits, staff caps, shot location/timing, clocks, timeout restrictions, substitutions, exhibition isolation, duplicate settlement, league-wide round advancement, playoffs, archives, drafts, repeated seasons, real-time AI termination, contest scoring, saves, controller dead-zone math, stock play shapes, rendering and a UI-to-save flow.

They do not replace extended human gameplay or physical Xbox/controller and audio-hardware testing. The UI input flow exercises managed form input state; it does not claim an external hardware keystroke injection test. Simulated games use an accelerated possession model rather than replaying every live action.

REA static analysis succeeded on the locally rebuilt Downfield source assembly. REA process capture could not run because the adapter explicitly does not support native Windows PTY cleanup verification. Normal Windows execution provided the runtime results above; this is documented in the retained evidence.

## Initial release SHA-256

| Artifact | SHA-256 |
|---|---|
| `play me/Upocourt.exe` | `FBD2FC301DC4A7C5DA2D6BBB65ABF2EBC0686E0C5369F65F1A3F4EE9B70AD24D` |
| `play me/Install Upocourt.exe` | `4E754AA2A43368E86814980CB9BC1E1889A3C534F8AAFB286DB34981B808656B` |
| `play me/Upocourt-Windows.zip` | `B92568843C180613CB215DD775A4B02426C17A76A5CBB4AE9C690FA6F696DE2F` |

Rebuilding can change compiler timestamps and ZIP metadata, so future builds may have different hashes. `verify-release.ps1` checks the artifacts against the current local build instead of hard-coding this historical table.
