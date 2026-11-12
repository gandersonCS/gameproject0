# Audio

[← Overview](README.md)

The audio system plays sound effects and music, with separate player-adjustable volumes, music
that cross-fades between tracks, and sounds that vary a little each time so repetition isn't
tiring. The library code is in `CIS580.MonoGame/Audio/`. The game's sounds are in
[`MechaHorde/Audio/GameAudio.cs`](../MechaHorde/Audio/GameAudio.cs) and
[`MechaHorde/UI/GameTheme.cs`](../MechaHorde/UI/GameTheme.cs).

The key rule: **the simulation never plays sounds.** It raises [events](simulation.md#eventqueuetevent),
and the client decides what they sound like. Sound randomness (which variation, what pitch) uses
`System.Random`, never the simulation's random numbers, so playing a sound can't change the game.

## `AudioManager` and buses

[`AudioManager`](../CIS580/CIS580.MonoGame/Audio/AudioManager.cs) is a service created in the
game's constructor. It owns a tree of [`AudioBus`](../CIS580/CIS580.MonoGame/Audio/AudioBus.cs)es,
like the channels on a mixing desk:

```
Master
 ├── Music     background music (MusicPlayer)
 ├── Effects   gameplay sounds — paused along with the game
 └── UI        menu sounds — keep playing while the game is paused
```

A sound's actual volume is its own volume × its bus's volume × each parent's volume, up to
`Master`. Changes apply immediately, even to sounds already playing.

| `AudioBus` member | Purpose |
|---|---|
| `Volume` | 0–1, this bus's own level |
| `Muted` | Silences the bus and everything under it, keeping `Volume` |
| `Paused` | Pauses sounds on the bus (they resume where they left off). New sounds don't start while paused. |
| `EffectiveVolume`, `IsPaused` | The result after parent buses are taken into account |
| `StopAll()` | Stops every sound on the bus |

`AudioManager.CreateBus(name, parent)` adds more buses (e.g. "Voice" or "Ambience").
`AudioManager.Update(elapsed)` must be called once per frame; `GameAudio.Update` does this.

## `SoundCue`

A [`SoundCue`](../CIS580/CIS580.MonoGame/Audio/SoundCue.cs) is a sound the game plays by
*purpose* ("jump", "coin") rather than by file, with rules for how it plays:

| Property | Default | Purpose |
|---|---|---|
| `Variations` | (constructor) | One or more `SoundEffect`s; each play picks one at random, never the same one twice in a row |
| `Volume` | 1 | The cue's base volume |
| `VolumeVariation` | 0 | Up to this fraction quieter each play (0.1 = up to 10%) |
| `PitchVariation` | 0 | Up to this many octaves up or down each play (0.05 is subtle) |
| `MaxInstances` | 4 | The most copies playing at once (0 = no limit) |
| `StealOldest` | true | At the limit, stop the oldest copy (true) or skip the new play (false) |
| `Cooldown` | 0 | The shortest time between plays |

```csharp
var hit = new SoundCue(audio.Effects,
    content.Load<SoundEffect>("Audio/Sfx/hit1"),
    content.Load<SoundEffect>("Audio/Sfx/hit2"),
    content.Load<SoundEffect>("Audio/Sfx/hit3"))
{
    PitchVariation = 0.08f,
    VolumeVariation = 0.15f,
    MaxInstances = 3,
};

hit.Play();                          // defaults
hit.Play(volume: 0.5f, pan: -0.4f);  // quieter, a bit to the left
```

`Play` returns `false` if the sound was skipped because of the cooldown, the instance limit
(with `StealOldest = false`), a paused bus, or a silent bus. Skipped sounds are **dropped, not
queued**: a sound triggered while the game is paused is never heard.

The bus owns and disposes each `SoundEffectInstance` when it finishes, so cues never leak
instances.

## `MusicPlayer`

[`MusicPlayer`](../CIS580/CIS580.MonoGame/Audio/MusicPlayer.cs) (`audio.MusicPlayer`) plays one
looping track on the Music bus, cross-fading when the track changes:

| Method | Purpose |
|---|---|
| `Play(track, fade)` | Cross-fades to `track` (default 1.5 s). If the track is already playing, nothing happens, so states can call it whenever they start. |
| `Stop(fade)` | Fades out |
| `Duck(level, fade)` / `Unduck(fade)` | Turns the music down temporarily (the pause menu ducks to 30%) |

Tracks are ordinary `SoundEffect`s, not MonoGame `Song`s, because `MediaPlayer` can only play
one song at a time and so can't cross-fade. That's why the content builder builds music from
`.wav` files as sound effects (see [Content](content.md)). WAV music is large, so keep loops
short.

[`Fader`](../CIS580/CIS580.MonoGame/Audio/Fader.cs) is the small struct behind the fades: a
level that moves toward a target at a steady rate. It's useful for your own fades too.

## UI sounds

Menus play sounds through the theme's `IUiSounds` (`UiSound.Move`, `Confirm`, `Cancel`), so the
UI library doesn't depend on the audio library. [`UiSoundSet`](../CIS580/CIS580.MonoGame/Audio/UiSoundSet.cs)
implements it with three cues, which `GameTheme` creates on the **UI bus**:

```csharp
Sounds = new UiSoundSet
{
    Move = new SoundCue(audio.Ui, content.Load<SoundEffect>("Audio/Sfx/ui_move")) { MaxInstances = 2 },
    Confirm = new SoundCue(audio.Ui, content.Load<SoundEffect>("Audio/Sfx/ui_confirm")),
    Cancel = new SoundCue(audio.Ui, content.Load<SoundEffect>("Audio/Sfx/ui_back")),
},
```

Leave a cue `null` for silence. Your own layers can play these too:
`Theme.Sounds.Play(UiSound.Confirm)`.

## This game's audio

[`GameAudio`](../MechaHorde/Audio/GameAudio.cs) is registered as a service in
`LoadContent`. It:

- loads the menu and game music, and offers `PlayMenuMusic()` / `PlayGameMusic()`, called by
  `MainMenuState` and `InGameState`
- loads the gameplay cues (on the **Effects** bus) and plays them in `OnGameEvent`, a `switch`
  over the simulation's events. `PlayerJoined` is panned toward the side of the world the player
  spawned on.
- applies the player's settings to the buses **every frame** in `Update`, so changes in the
  options menu are heard immediately:

| Setting | Bus |
|---|---|
| `SoundEnabled` | `Master.Muted` |
| `MusicVolume` (0–10) | `Music.Volume` |
| `EffectsVolume` (0–10) | `Effects.Volume` **and** `Ui.Volume` |

Pausing is handled by `PausedState`. It ducks the music and, if the simulation stops, pauses the
Effects bus; menu sounds on the UI bus still play. `InGameState.Exit` stops all effects so
nothing carries over into the menus.

## Extending audio

### A sound for a new event

1. Add the `.wav` to `MechaHorde.Content/Assets/Audio/Sfx/` and list it in
   [`ASSETS.md`](../ASSETS.md).
2. Create a cue in the `GameAudio` constructor:
   ```csharp
   _coin = new SoundCue(audio.Effects, content.Load<SoundEffect>("Audio/Sfx/coin"))
   {
       PitchVariation = 0.1f,
       Cooldown = TimeSpan.FromMilliseconds(30),   // many coins at once = one sound, not a roar
   };
   ```
3. Add a case to `OnGameEvent`:
   ```csharp
   case CoinCollected coin:
       _coin.Play(pan: PanFor(coin.Position));
       break;
   ```

### Different music per level

Load the tracks in `GameAudio` (or in `InGameState.Load` with the state's own content manager)
and call `Audio.MusicPlayer.Play(track)`. The player cross-fades automatically.

### A new volume setting

To give voices their own volume:

1. `Voice = audio.CreateBus("Voice")` in `GameAudio`, and play voice cues on it.
2. Add `VoiceVolume` to [`GameSettings`](settings.md) and a `RangeItem` to `OptionsLayer`.
3. Apply it in `GameAudio.Update`: `Voice.Volume = _settings.VoiceVolume / (float)GameSettings.MaxVolume;`

### Positional sound

`SoundCue.Play` takes a `pan` from -1 (left) to 1 (right). `GameAudio.PanFor` maps a world X
position to a gentle pan (it stays within ±0.6, because hard-panned sounds are uncomfortable on
headphones). For distance, also scale `volume` down with distance from the listener (e.g. the
camera's position). In split screen, the closest player's camera is a reasonable listener.

### Testing

Audio classes can be tested with real `SoundEffect`s. The library's tests create short silent
buffers, and skip themselves when the machine has no audio device. See
[`Tests/CIS580.Tests/Audio`](../Tests/CIS580.Tests/Audio).
