# Recorded crowds

Upcourt embeds the same four real CC0 crowd recordings used by Downfield, retrieved from [its audio directory](https://github.com/dstaggs-15/downfieldftbl/tree/main/audio). Original source notes follow. Court effects are short, original synthesized transients; routine jumps, cuts, passes and movements no longer add electronic glides or an automatic squeak loop.

# Crowd recordings

The crowd uses real recordings released under **CC0 1.0**. The recordings retain that dedication; the game's MIT license covers its own code, drawings and synthesized effects.

| Game file | Original recording and creator | Retrieved copy |
| --- | --- | --- |
| stadium.wav | [Stadium Crowd](https://freesound.org/people/stomachache/sounds/274516/) by stomachache | [stadium-bed.mp3](https://github.com/tgilbert14/old-pueblo/blob/0ad18e87abc1a340a31e91ddc5af9d651444d782/assets/audio/stadium-bed.mp3), an approximately seven-second circular edit |
| cheer.wav | [College Football Game.wav](https://freesound.org/people/timmyg62/sounds/328623/) by timmyg62 | [crowd-swell.mp3](https://github.com/tgilbert14/old-pueblo/blob/0ad18e87abc1a340a31e91ddc5af9d651444d782/assets/audio/crowd-swell.mp3), original 39.2–46.8 seconds |
| applause.wav | [BaseballGameApplause.wav](https://freesound.org/people/eeease/sounds/354056/) by eeease; title used in the retrieved source credits | [cheer_2.mp3](https://github.com/davidfwatson/game-simulator/blob/7c12bf73d3aab1085ec67ccb0daa86af32458f0c/audio/sfx/cheer_2.mp3), original 5–10.5 seconds |
| groan.wav | [Crowd Groans At Bad Joke 01](https://freesound.org/people/ShangusBurger/sounds/764144/) by ShangusBurger / Shane Vincent, GameSoundCon 2024 walla session | [groan_1.mp3](https://github.com/davidfwatson/game-simulator/blob/7c12bf73d3aab1085ec67ccb0daa86af32458f0c/audio/sfx/groan_1.mp3), original 0–2.5 seconds |

The retrieved projects document the CC0 origins in their [old-pueblo credits](https://github.com/tgilbert14/old-pueblo/blob/0ad18e87abc1a340a31e91ddc5af9d651444d782/assets/audio/README.md) and [game-simulator credits](https://github.com/davidfwatson/game-simulator/blob/7c12bf73d3aab1085ec67ccb0daa86af32458f0c/audio/sfx/SOURCES.md). Only these four recordings were imported. No other game code or audio was copied.

Downfield edits: decoded with Windows Media Foundation, mixed to mono, high-pass filtered at 140 Hz, band-limited and resampled to 22,050 Hz / 16-bit PCM. Approximate RMS targets: 0.018 background, 0.16 cheer, 0.12 applause and 0.14 groan, with peak limiting. CrowdRecordings.cs trims reactions, adds fades and joins the ambient loop. WAV files are embedded in the EXE; playback needs no network or extra codecs.

[CC0 1.0 dedication and terms](https://creativecommons.org/publicdomain/zero/1.0/). Credits do not imply endorsement by the recordists or retrieved projects.

The stadium bed is low-pass filtered and blended into a quiet 35-second evolving loop to remove piercing whistle tones. Source licenses and attribution remain the same.

