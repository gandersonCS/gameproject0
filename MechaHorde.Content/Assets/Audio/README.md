# Audio

| File | Use |
|---|---|
| `Music/promise.wav` | Menu music |
| `Music/out.mp3` | Gameplay music (built as a SoundEffect; see `Builder.cs`) |
| `Sfx/Click_Standard_05.wav` | Menu selection moved / button hovered |
| `Sfx/Click_Heavy_00.wav` | Menu button chosen |
| `Sfx/Click_Standard_02.wav` | Menu cancelled / back |
| `Sfx/player_join.wav`, `Sfx/player_leave.wav` | A player joined or left the game |
| `Sfx/cg1.wav`, `Sfx/Shots/*`, `Sfx/Launches/*` | Weapon sounds, for the gameplay |

Music is built as `SoundEffect` content (see `Builder.cs`), which lets the music player loop and
cross-fade tracks. Content is loaded by its path relative to `Assets/`, without the extension,
e.g. `Content.Load<SoundEffect>("Audio/Sfx/Shots/rifle")`. Sources and licenses are in the
top-level `ASSETS.md`.
