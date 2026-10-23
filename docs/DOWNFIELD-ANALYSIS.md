# Downfield analysis and the Upocourt adaptation

## Scope and identity

Source examined: [`dstaggs-15/downfieldftbl`](https://github.com/dstaggs-15/downfieldftbl), main at commit **61b5029745b27d6319ae94cdda6ebe4cb9b988ee**, reviewed on October 23, 2026. The complete tree was inventoried and all 71 production C# files were retrieved. README, franchise guide, build script and relevant implementation files were examined in detail.

The user requested REA. A local review assembly was compiled from those production sources with the existing Windows .NET Framework compiler, then inspected with REA's managed-artifact and selected-member tools. The assembly contains no bundled football crowd resources and was built for static review. **It is not the shipped Downfield executable.** The GitHub connector rejected the shipped binary as non-UTF-8, and direct shell networking was unavailable. No claim of binary equivalence or execution of the original game follows from this build.

REA observed a valid x64 PE/CLI assembly, CIL implementation, metadata version `v4.0.30319`, assembly version `2.3.0.0`, 140 type definitions and 908 method definitions. Selected member inspection covered `Downfield.Simulation`, `Downfield.Franchise`, `Downfield.Input` and `Downfield.GameForm`: 429 fields, 599 methods, 7,421 call-edge records and 10,594 field-access records. These are metadata/CIL observations, not observed player behavior or decompiled original source.

Evidence and its limitations are retained under [research](../research/README.md). Source conclusions below come from the repository; the REA record independently anchors the structure of the local source build.

## What defines Downfield

Downfield's core is quick, offense-controlled arcade football layered over an extensive persistent club simulation. Its original art is generated from small colored rectangles, with a pixel font and a low-resolution drawing surface rather than external sprite sheets. The interface, stadium and office share the same colors, shapes and typography.

The distinguishing feature is the relationship between matches and the club: personnel attributes affect play; results feed development, injuries, money, fans and the coach's career; then those management decisions affect the next match. A basketball version needs that loop, rather than only a renamed field and scoreboard.

### Live football

[`Simulation.cs`](https://github.com/dstaggs-15/downfieldftbl/blob/61b5029745b27d6319ae94cdda6ebe4cb9b988ee/src/Simulation.cs) separates presnap, live, flight, loose-ball, dead-ball, opponent simulation, conversion, kicking, halftime and game-over states. Player control changes with ball state. A set of partial simulation classes adds catches, impacts, blocking, weather, emergency quarterbacks and receiver commands.

The README explicitly describes offense-only play and simulated opponent drives. Quarters are 60 seconds. Offensive calls include runs, routes, option plays and tricks. There are 48 stock calls and up to 40 saved custom calls. Football Bowling adapts scoring opportunities into ten-frame bowling.

### Club and league

[`Franchise.cs`](https://github.com/dstaggs-15/downfieldftbl/blob/61b5029745b27d6319ae94cdda6ebe4cb9b988ee/src/Franchise.cs) stores permanent team/player IDs, five management dimensions around the roster, salaries, contract years, age, potential, XP, morale and injuries. Twelve teams play an eleven-round single round robin. Six qualify for the postseason, with two byes.

Signing fees consume cash while annual salaries consume cap space. Signings enter the reserves. Starting slots, renewals, reserve releases, staff, training and facilities have explicit preconditions. A current fixture must be unplayed before a result can settle it.

[`TradeEvaluation.cs`](https://github.com/dstaggs-15/downfieldftbl/blob/61b5029745b27d6319ae94cdda6ebe4cb9b988ee/src/TradeEvaluation.cs) adds more than an OVR comparison: age, potential, years, salary value, health, positional needs, rebuilding/contending plans and both payrolls affect acceptance. It reviews a transaction before mutating rosters.

[`LeagueDevelopment.cs`](https://github.com/dstaggs-15/downfieldftbl/blob/61b5029745b27d6319ae94cdda6ebe4cb9b988ee/src/LeagueDevelopment.cs) gives CPU clubs separate rosters, cash, recruitment, trades, staff and development. Aging, contract expiration, retirement and a draft maintain the league between seasons. Its source explicitly avoids scaling CPU clubs to the user's roster.

[`ClubBusiness.cs`](https://github.com/dstaggs-15/downfieldftbl/blob/61b5029745b27d6319ae94cdda6ebe4cb9b988ee/src/ClubBusiness.cs) and [`Career.cs`](https://github.com/dstaggs-15/downfieldftbl/blob/61b5029745b27d6319ae94cdda6ebe4cb9b988ee/src/Career.cs) connect results to the club economy, fan sentiment, jobs, coach records, awards and archived seasons. The guide also describes owner confidence and dismissal. Historic names and identities matter after relocation or changing clubs.

### Input, presentation and packaging

[`Input.cs`](https://github.com/dstaggs-15/downfieldftbl/blob/61b5029745b27d6319ae94cdda6ebe4cb9b988ee/src/Input.cs) polls XInput with provider fallback and a dead zone. Keyboard and pad actions are interpreted by context. Device activity determines prompts. Mouse clicks also select menus.

[`PixelArt.cs`](https://github.com/dstaggs-15/downfieldftbl/blob/61b5029745b27d6319ae94cdda6ebe4cb9b988ee/src/PixelArt.cs) contains the bitmap glyph dictionary and cached 24×30 procedural player sprites. The outline is `(26,30,48)`, with deterministic skin tones and rectangular uniform layers. Animation changes limb offsets and poses. [`GameForm.cs`](https://github.com/dstaggs-15/downfieldftbl/blob/61b5029745b27d6319ae94cdda6ebe4cb9b988ee/src/GameForm.cs) renders onto a 640×400 frame, logically draws at 1280×800 and enlarges with nearest-neighbor filtering. [`ModeMenu.cs`](https://github.com/dstaggs-15/downfieldftbl/blob/61b5029745b27d6319ae94cdda6ebe4cb9b988ee/src/ModeMenu.cs) uses three illustrated cards, with a pixel office in the franchise card. [`StadiumArt.cs`](https://github.com/dstaggs-15/downfieldftbl/blob/61b5029745b27d6319ae94cdda6ebe4cb9b988ee/src/StadiumArt.cs) builds spectators from tiny head/shirt rectangles and hashed color choices.

The native Windows build invokes the installed .NET Framework C# compiler directly. It produces separate game and test EXEs, then a per-user installer with the game embedded. The source build excludes test helpers from the playable binary.

## Basketball translation

| Downfield foundation | Upocourt implementation |
|---|---|
| Native WinForms / procedural art | Same native stack; original font, adapted player sprites, matching canvas/palette |
| Offense-only football | Playable five-on-five offense and defense |
| Presnap / live / pass / dead phases | Inbound / live / pass / shot / rebound / free throw / period / final |
| Quarter clock and possession | Configurable quarters, 24-second and backcourt clocks, overtime |
| Receiver button passing | Numbered position passing, automatic teammate selection and lobs |
| Catch timing | Shot timing with skill, range, fatigue and contest feedback |
| Jukes and impacts | Crossover, pump fake, screen, steal, block and rebound positioning |
| Coverage and pursuit | Man matchups, 2–3 zone, pressure and pass-lane interception |
| Playbook and drawn routes | 24 distinct basketball waypoint layouts and 40 saved custom plays |
| Football Bowling | Five-rack three-point contest with money balls and saved best |
| Eleven-week football schedule | 22-game basketball home-and-away schedule |
| Six postseason teams / two byes | Same qualifying structure, best-of-three series and best-of-five final |
| Position-specific football starters | Five eligible starting positions, secondary positions and actual bench identities |
| Cap / signings / trades | $140M cap, 15-player maximum, cash fees and reviewed transactions |
| Staff / facilities / training | Eight basketball attributes, training XP, coaching and medical/arena tracks |
| Simulated CPU drives and club development | Accelerated basketball possessions, independent clubs and multi-season development |
| Three-round football draft | Two rounds, 36 prospects, reverse-record order and rookie contracts |
| Coach, business and history | Coach W/L/titles, jobs, owner confidence, tickets, fans, relocation and archived awards |
| Auto-saved career, no live resumption | Atomic backed-up career saves **including live matches** |

Upocourt is a basketball adaptation, not a football source fork with text substitutions. Football mechanics are not carried into the match engine. Art/font reuse is intentional; ball physics, court rules, playoff series and roster models are basketball-specific code.

## Invariants preserved and strengthened

1. **Commit each result once.** Only the current unplayed fixture accepts a final, untied result. Re-entering a final screen cannot repeat finances or production.
2. **Keep exhibition separate.** Matches clone player data and produce independent box scores. League settlement is explicit and requires a fixture identity.
3. **Use permanent IDs.** Signings, trades, drafts, lineups, live actors and historic records use stable player/team IDs.
4. **Review before mutation.** Cap, roster, cash and trade checks precede changes. Failed transactions preserve ownership and funds.
5. **Reconcile statistics.** Team points equal the sum of player points; each player's points equal `2*FGM + 3PM + FTM`. Makes do not exceed attempts. Shooting fouls handle missed attempts separately.
6. **Finish basketball with a winner.** Played and accelerated games resolve ties with further overtime/possessions. Tied results fail validation.
7. **Retain the actual live state.** Saving includes ball owner, position, clocks, actors, fatigue, fouls, shot flight and deterministic random-call count.
8. **Archive once.** A season's history is keyed by its season number. Historic club names survive later branding changes.
9. **Keep CPU clubs independent.** Their rosters, payrolls, plans, transactions and player development remain separate from the user.
10. **Protect suspended games.** League roster/economic changes and replacement games are blocked while a live match exists.

## Verification and remaining boundaries

The implementation was compiled on Windows with the installed compiler. Automated verification includes real-time AI game completion, player/team box reconciliation, transactions, full league/postseason progression, six consecutive offseason/draft lifecycles, backed-up saves, resumed state, contest scoring and 15 menu render paths. A UI flow drives movement, hold/release shooting, ball flight, pause and saving through the form's keyboard input path.

REA's Windows PTY capture adapter explicitly reported `capability_unavailable`: it cannot verify descendant cleanup on native Windows. No installation or platform reconfiguration was attempted. Runtime tests therefore ran through the normal Windows executor, not a successful REA process capture. This limit is retained separately from successful REA static inspection.

The original shipped Downfield EXE was not inspected or run; the local rebuilt assembly was inspected statically. Hardware controller feel and audio output were not physically evaluated. Upocourt intentionally uses an arcade boundary model and a separate accelerated possession simulator. It has no online or local-versus mode, no football Bowling, and no claim to implement every professional basketball rule. These are explicit product boundaries, not negative findings inferred from missing strings.

The original investigation questions—architecture, art identity, live-game structure, franchise progression, persistence, input, distribution and basketball mapping—are answered by the retrieved sources and implementation. Verification of the identity/behavior of the shipped original binary remains unresolved because binary retrieval was unavailable.
