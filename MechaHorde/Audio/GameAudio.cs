using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using CIS580.Audio;
using MechaHorde.Settings;
using MechaHorde.Simulation;

namespace MechaHorde.Audio;

/// <summary>
/// This game's sounds and music, and what plays them.
/// </summary>
/// <remarks>
/// <para>
/// The simulation never plays sounds itself (it doesn't even know sound exists). Instead it
/// raises events, and <see cref="OnGameEvent"/> decides what each one sounds like. To add a
/// sound for a new event, load a cue below and add a case for the event.
/// </para>
/// <para>
/// The player's settings (sound on or off, and the volumes) are applied to the audio buses
/// every frame, so changes in the options menu take effect immediately.
/// </para>
/// </remarks>
public sealed class GameAudio
{
    private readonly GameSettings _settings;
    private readonly SoundCue _playerJoined;
    private readonly SoundCue _playerLeft;

    /// <param name="audio">The audio manager</param>
    /// <param name="content">Loads the sounds; they stay loaded while the game runs</param>
    /// <param name="settings">The player's settings</param>
    public GameAudio(AudioManager audio, ContentManager content, GameSettings settings)
    {
        Audio = audio ?? throw new ArgumentNullException(nameof(audio));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        // Each song is cut into an intro, played once, and a loop that repeats (see the
        // music README for where the cuts are). The pause music is one long loop.
        MenuMusic = new MusicTrack(
            intro: content.Load<SoundEffect>("Audio/Music/uprising_intro"),
            loop: content.Load<SoundEffect>("Audio/Music/uprising_loop"));
        GameMusic = new MusicTrack(
            intro: content.Load<SoundEffect>("Audio/Music/calamity_intro"),
            loop: content.Load<SoundEffect>("Audio/Music/calamity_loop"));
        PauseMusic = new MusicTrack(content.Load<SoundEffect>("Audio/Music/emergent_loop"));

        // Gameplay sounds go on the Effects bus, which is paused along with the game
        _playerJoined = new SoundCue(audio.Effects, content.Load<SoundEffect>("Audio/Sfx/player_join"))
        {
            PitchVariation = 0.05f,
        };
        _playerLeft = new SoundCue(audio.Effects, content.Load<SoundEffect>("Audio/Sfx/player_leave"));
    }

    /// <summary>
    /// The audio manager
    /// </summary>
    public AudioManager Audio { get; }

    /// <summary>
    /// The music for the menus: "Uprising", from its drum opening, then looping the build-up
    /// and the quiet break after it (the heroic ending is cut)
    /// </summary>
    public MusicTrack MenuMusic { get; }

    /// <summary>
    /// The music during play: "Calamity", from its build-up, then looping from the kick-off
    /// </summary>
    public MusicTrack GameMusic { get; }

    /// <summary>
    /// The music on the pause menu: "Emergent"
    /// </summary>
    public MusicTrack PauseMusic { get; }

    /// <summary>
    /// Cross-fades to the menu music (if it isn't already playing). The fade is short so the
    /// opening drum hit isn't faded.
    /// </summary>
    public void PlayMenuMusic() => Audio.MusicPlayer.Play(MenuMusic, TimeSpan.FromSeconds(0.4));

    /// <summary>
    /// Cross-fades to the gameplay music (if it isn't already playing)
    /// </summary>
    public void PlayGameMusic() => Audio.MusicPlayer.Play(GameMusic);

    /// <summary>
    /// Fades the gameplay music out, holding its place, and plays the pause music
    /// </summary>
    public void StartPauseMusic() => Audio.MusicPlayer.Interrupt(PauseMusic, TimeSpan.FromSeconds(0.6));

    /// <summary>
    /// Fades the pause music out, and the gameplay music back in from where it was
    /// </summary>
    public void EndPauseMusic() => Audio.MusicPlayer.EndInterruption(TimeSpan.FromSeconds(0.6));

    /// <summary>
    /// Plays the sound (if any) for something that happened in the game
    /// </summary>
    /// <param name="gameEvent">What happened</param>
    /// <param name="world">The world it happened in, e.g. to find where a sound comes from</param>
    public void OnGameEvent(GameEvent gameEvent, World world)
    {
        switch (gameEvent)
        {
            case PlayerJoined joined:
                var avatar = world.FindPlayer(joined.Player);
                _playerJoined.Play(pan: avatar != null ? PanFor(avatar.Position) : 0f);
                break;
            case PlayerLeft:
                _playerLeft.Play();
                break;
        }
    }

    /// <summary>
    /// Applies the settings to the audio buses, and updates the audio. Call once per frame.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        Audio.Master.Muted = !_settings.SoundEnabled;
        Audio.Music.Volume = _settings.MusicVolume / (float)GameSettings.MaxVolume;
        Audio.Effects.Volume = _settings.EffectsVolume / (float)GameSettings.MaxVolume;
        Audio.Ui.Volume = Audio.Effects.Volume;

        Audio.Update(gameTime.ElapsedGameTime);
    }

    /// <summary>
    /// Pans a sound toward the side of the world it comes from (gently: hard left or right
    /// sounds odd on headphones)
    /// </summary>
    private static float PanFor(Vector2 position)
        => 0.6f * Math.Clamp(position.X / World.Width * 2f - 1f, -1f, 1f);
}
