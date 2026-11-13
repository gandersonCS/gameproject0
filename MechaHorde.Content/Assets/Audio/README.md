# Audio

| File | Use |
|---|---|
| `Music/uprising_intro.ogg`, `Music/uprising_loop.ogg` | Menu music |
| `Music/calamity_intro.ogg`, `Music/calamity_loop.ogg` | Gameplay music |
| `Music/emergent_loop.ogg` | Pause menu music |
| `Music/promise.wav`, `Music/out.mp3` | The old menu and gameplay music (no longer played) |
| `Sfx/Click_Standard_05.wav` | Menu selection moved / button hovered |
| `Sfx/Click_Heavy_00.wav` | Menu button chosen |
| `Sfx/Click_Standard_02.wav` | Menu cancelled / back |
| `Sfx/player_join.wav`, `Sfx/player_leave.wav` | A player joined or left the game |
| `Sfx/cg1.wav`, `Sfx/Shots/*`, `Sfx/Launches/*` | Weapon sounds, for the gameplay |

Music is built as `SoundEffect` content (see `Builder.cs`), which lets the music player loop and
cross-fade tracks. Content is loaded by its path relative to `Assets/`, without the extension,
e.g. `Content.Load<SoundEffect>("Audio/Music/calamity_loop")`. Sources and licenses are in the
top-level `ASSETS.md`.

## Intros and loops

Each song is cut into an `_intro`, played once, and a `_loop`, which repeats (see `MusicTrack`).
The loop starts exactly where the intro ends, and its last 15 ms blend into the audio just before
its start, so it repeats without a seam. Both ends of each loop are on the beat, nudged onto a
quiet zero crossing. The cuts, in the original files (44.1 kHz):

| Song | Intro | Loop | Notes |
|---|---|---|---|
| Uprising | 0:00.50 – 0:09.959 | 0:09.959 – 2:06.959 (117.000 s) | The intro is the drums-only opening. The loop runs from the drum hit before the strings come in, through the build-up and the quiet break after the drop, and ends just before the heroic section at 2:07, which is cut. |
| Calamity | 0:00.05 – 0:14.812 | 0:14.812 – 3:15.738 (180.925 s) | The intro is the fade-in build-up. The loop starts at the kick-off and ends on the downbeat just before the song starts fading out, so each repeat goes straight back into the kick-off. |
| Emergent | — | 0:01.40 – 3:48.75 | The whole song, minus the silence at each end. It starts from the beginning each time the game is paused. |

The intro hands over to the loop on the frame nearest the intro's end, so the changeover can be a
few milliseconds late. Each one is cut just before a big drum hit, where that can't be heard.
