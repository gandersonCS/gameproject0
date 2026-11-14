using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using CIS580.Audio;
using MechaHorde.Gameplay;
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

    // The level's sounds (see Listen). All are on the Effects bus, so they pause with the game.
    private readonly SoundCue _rifle;
    private readonly SoundCue _hitLight;        // Bullets on the small robots
    private readonly SoundCue _hitMedium;       // ...on the walking gunners
    private readonly SoundCue _hitArmor;        // ...glancing off the heavy mech's armor
    private readonly SoundCue _explosion;
    private readonly SoundCue _groundPound;     // The heavy mech arriving, and falling
    private readonly SoundCue _playerStep;
    private readonly SoundCue _playerLand;
    private readonly SoundCue _robotStep;
    private readonly SoundCue _mechStep;
    private readonly SoundCue _waveStart;
    private readonly SoundCue _warCry;

    private readonly SoundCue _pickupScrap;     // Collecting Scraplight
    private readonly SoundCue _pickupSpark;

    // Loaded and ready for things the game doesn't have yet (see the methods at the end)
    private readonly SoundCue _rifleHeavy;
    private readonly SoundCue _enemyLaser;
    private readonly SoundCue _enemyBurst;
    private readonly SoundCue _playerHurt;

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

        // The level's sounds. Every file is normalized to the same peak level, so these
        // volumes are the mix: tune them here. Sources are in ASSETS.md.
        SoundEffect Sfx(string name) => content.Load<SoundEffect>("Audio/Sfx/" + name);

        _rifle = new SoundCue(audio.Effects, Sfx("Weapons/rifle_shot_1"))
            { Volume = 0.55f, PitchVariation = 0.06f, VolumeVariation = 0.1f, MaxInstances = 4 };
        _hitLight = new SoundCue(audio.Effects, Sfx("Impacts/clink_1"), Sfx("Impacts/clink_2"), Sfx("Impacts/clink_3"))
            { Volume = 0.5f, PitchVariation = 0.12f, MaxInstances = 4, Cooldown = TimeSpan.FromMilliseconds(30) };
        _hitMedium = new SoundCue(audio.Effects, Sfx("Impacts/thud_1"), Sfx("Impacts/thud_2"))
            { Volume = 0.6f, PitchVariation = 0.08f, MaxInstances = 3, Cooldown = TimeSpan.FromMilliseconds(40) };
        _hitArmor = new SoundCue(audio.Effects, Sfx("Impacts/clang_heavy_1"), Sfx("Impacts/clang_heavy_2"))
            { Volume = 0.4f, PitchVariation = 0.08f, MaxInstances = 2, Cooldown = TimeSpan.FromMilliseconds(120) };
        _explosion = new SoundCue(audio.Effects, Sfx("Robots/mechanical_explosion"))
            { Volume = 0.7f, PitchVariation = 0.1f, MaxInstances = 5, Cooldown = TimeSpan.FromMilliseconds(30) };
        _groundPound = new SoundCue(audio.Effects, Sfx("Robots/ground_pound"))
            { Volume = 0.7f, MaxInstances = 2, Cooldown = TimeSpan.FromMilliseconds(250) };
        _playerStep = new SoundCue(audio.Effects, Sfx("Player/step_1"), Sfx("Player/step_2"), Sfx("Player/step_3"), Sfx("Player/step_4"))
            { Volume = 0.3f, PitchVariation = 0.06f, VolumeVariation = 0.2f, MaxInstances = 2 };
        _playerLand = new SoundCue(audio.Effects, Sfx("Impacts/thud_1"), Sfx("Impacts/thud_2"))
            { Volume = 0.35f, PitchVariation = 0.05f, MaxInstances = 1 };
        // A horde stomping at once would drown everything, so robot steps are quiet and few
        _robotStep = new SoundCue(audio.Effects, Sfx("Player/step_1"), Sfx("Player/step_2"), Sfx("Player/step_3"), Sfx("Player/step_4"))
            { Volume = 0.14f, PitchVariation = 0.1f, MaxInstances = 3, Cooldown = TimeSpan.FromMilliseconds(70) };
        _mechStep = new SoundCue(audio.Effects, Sfx("Player/step_1"), Sfx("Player/step_2"), Sfx("Player/step_3"), Sfx("Player/step_4"))
            { Volume = 0.45f, PitchVariation = 0.05f, MaxInstances = 2, Cooldown = TimeSpan.FromMilliseconds(150) };
        _waveStart = new SoundCue(audio.Effects, Sfx("Waves/wave_gong")) { Volume = 0.6f, MaxInstances = 1 };
        _warCry = new SoundCue(audio.Effects, Sfx("Waves/warcry")) { Volume = 0.6f, MaxInstances = 1 };

        _rifleHeavy = new SoundCue(audio.Effects, Sfx("Weapons/rifle_shot_2")) { Volume = 0.7f, PitchVariation = 0.04f, MaxInstances = 3 };
        _enemyLaser = new SoundCue(audio.Effects, Sfx("Weapons/laser_rifle")) { Volume = 0.35f, PitchVariation = 0.1f, MaxInstances = 3 };
        _enemyBurst = new SoundCue(audio.Effects, Sfx("Weapons/burst_fire")) { Volume = 0.4f, PitchVariation = 0.08f, MaxInstances = 3 };
        _playerHurt = new SoundCue(audio.Effects, Sfx("Player/hurt")) { Volume = 0.7f, PitchVariation = 0.05f, MaxInstances = 1, Cooldown = TimeSpan.FromMilliseconds(150) };
        _pickupScrap = new SoundCue(audio.Effects, Sfx("Player/pickup"))
            { Volume = 0.4f, PitchVariation = 0.08f, MaxInstances = 3, Cooldown = TimeSpan.FromMilliseconds(45) };
        _pickupSpark = new SoundCue(audio.Effects, Sfx("Player/shield")) { Volume = 0.55f, MaxInstances = 1 };
    }

    /// <summary>
    /// Plays the level's sounds as things happen in it: the gun, hits, explosions, footsteps
    /// and the waves. Call once, when the level is shown.
    /// </summary>
    /// <param name="battlefield">The level being played</param>
    public void Listen(Battlefield battlefield)
    {
        var player = battlefield.Player;
        var spawner = battlefield.Spawner;

        // Plays a cue as heard from where something is in the level
        void PlayAt(SoundCue cue, float x, float volume = 1f, float pitch = 0f)
        {
            var (hearVolume, pan) = battlefield.Hear(x);
            cue.Play(volume * hearVolume, pan, pitch);
        }

        battlefield.PlayerShot += () => PlayAt(_rifle, player.position.X);
        player.Footstep += () => PlayAt(_playerStep, player.position.X, pitch: 0.1f);
        player.Landed += () => PlayAt(_playerLand, player.position.X);

        spawner.WaveStarted += wave =>
        {
            _waveStart.Play(pitch: -0.2f);
            // A war cry to rally for every fifth wave
            if (wave % 5 == 0)
                _warCry.Play(pitch: -0.1f);
        };

        // The heavy mech announces itself with a distant thump as it drops in off the screen
        spawner.EnemySpawned += enemy =>
        {
            if (enemy.EnemyType == Enemy.Type.heavy)
                PlayAt(_groundPound, enemy.Position.X, pitch: -0.2f);
        };

        // Bullets that don't finish a robot off ring off it (destroyed ones explode instead)
        spawner.EnemyDamaged += enemy =>
        {
            if (!enemy.IsAlive)
                return;
            switch (enemy.EnemyType)
            {
                case Enemy.Type.heavy: PlayAt(_hitArmor, enemy.Position.X); break;
                case Enemy.Type.medium: PlayAt(_hitMedium, enemy.Position.X); break;
                default: PlayAt(_hitLight, enemy.Position.X, pitch: 0.1f); break;
            }
        };

        // Bigger robots go up with a bigger, deeper bang
        spawner.EnemyDestroyed += enemy =>
        {
            float x = enemy.Position.X;
            switch (enemy.EnemyType)
            {
                case Enemy.Type.drone: PlayAt(_explosion, x, 0.75f, pitch: 0.25f); break;
                case Enemy.Type.ranged: PlayAt(_explosion, x, 0.85f, pitch: 0.1f); break;
                case Enemy.Type.medium: PlayAt(_explosion, x, 0.9f, pitch: -0.1f); break;
                default:
                    PlayAt(_explosion, x, 1f, pitch: -0.4f);
                    PlayAt(_groundPound, x, 0.8f, pitch: -0.3f);   // It crashes to the ground
                    break;
            }
        };

        // Collecting Scraplight: a bright ping for scrap, a shimmering hum for the rare sparks
        battlefield.Scraplight.Changed += (scrap, sparks) =>
        {
            if (sparks > 0)
                PlayAt(_pickupSpark, player.position.X, pitch: 0.2f);
            else if (scrap > 0)
                PlayAt(_pickupScrap, player.position.X, pitch: 0.3f);
        };

        spawner.EnemyStepped += enemy =>
        {
            if (enemy.EnemyType == Enemy.Type.heavy)
                PlayAt(_mechStep, enemy.Position.X, pitch: -0.6f);
            else
                PlayAt(_robotStep, enemy.Position.X, pitch: -0.3f);
        };
    }

    // Sounds for features the game doesn't have yet: call these when you add them

    /// <summary>A heavier or charged player shot (an alt fire or an upgraded gun)</summary>
    public void PlayHeavyShot(float pan = 0f) => _rifleHeavy.Play(pan: pan);

    /// <summary>A ranged robot firing its laser</summary>
    public void PlayEnemyLaser(float volume = 1f, float pan = 0f) => _enemyLaser.Play(volume, pan, -0.15f);

    /// <summary>A gunner or turret firing a burst</summary>
    public void PlayEnemyBurst(float volume = 1f, float pan = 0f) => _enemyBurst.Play(volume, pan, -0.1f);

    /// <summary>The player taking damage</summary>
    public void PlayPlayerHurt() => _playerHurt.Play();

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
