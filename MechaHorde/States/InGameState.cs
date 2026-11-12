using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using CIS580.Feedback;
using CIS580.Flow;
using CIS580.Input;
using CIS580.Simulation;
using MechaHorde.Audio;
using MechaHorde.Feedback;
using MechaHorde.Gameplay;
using MechaHorde.Settings;
using MechaHorde.Simulation;
using MechaHorde.Views;

namespace MechaHorde.States;

/// <summary>
/// A game in progress.
/// </summary>
/// <remarks>
/// This state owns the game: the <see cref="GameSession"/> (the simulation), the
/// <see cref="SessionRunner"/> that advances it, the local players feeding it commands, and
/// MechaHorde's <see cref="Gameplay.Battlefield"/> (the level being played). Each frame it reads
/// the players' input, runs the simulation and the level, and watches for players pausing.
/// The views only read the game.
/// </remarks>
public sealed class InGameState : AppState, ILoadable
{
    private readonly IReadOnlyList<InputSource> _inputSources;
    private readonly List<LocalPlayer> _players = new();
    private readonly HashSet<LocalPlayer> _lostDevice = new();

    private ContentManager _content;
    private LevelView _levelView;
    private PausePolicy _pausePolicy;
    private GameAudio _audio;
    private GameSettings _settings;
    private readonly Rumble _rumble = new();
    private readonly HitStop _hitStop = new();

    /// <summary>
    /// The simulation
    /// </summary>
    public GameSession Session { get; private set; }

    /// <summary>
    /// The level being played (MechaHorde's gameplay)
    /// </summary>
    public Battlefield Battlefield { get; private set; }

    /// <summary>
    /// Advances the simulation
    /// </summary>
    public SessionRunner Runner { get; private set; }

    /// <summary>
    /// The players on this machine
    /// </summary>
    public IReadOnlyList<LocalPlayer> Players => _players;

    /// <summary>
    /// The rules for pausing
    /// </summary>
    public PausePolicy PausePolicy => _pausePolicy;

    /// <summary>
    /// Raised for each event the simulation raises (once per event, after the tick it happened
    /// in). Views, audio, and effects subscribe to this to react to what happens in the game.
    /// </summary>
    public event Action<GameEvent> GameEventRaised;

    /// <param name="inputSources">One input source for each local player</param>
    public InGameState(IReadOnlyList<InputSource> inputSources)
    {
        if (inputSources == null || inputSources.Count == 0)
            throw new ArgumentException("A game needs at least one player.", nameof(inputSources));
        _inputSources = inputSources;
    }

    /// <summary>
    /// Loads the game's content and creates the session (run by a LoadingState)
    /// </summary>
    public IEnumerable<float> Load()
    {
        var services = Game.Services;

        // Gameplay content is loaded with its own ContentManager, so it can all be
        // unloaded when the game ends
        _content = new ContentManager(services, "Content");
        Battlefield = new Battlefield(Game);
        Battlefield.LoadContent(_content);
        yield return 0.5f;

        _pausePolicy = services.GetService<PausePolicy>() ?? new PausePolicy();
        _audio = services.GetService<GameAudio>();
        _settings = services.GetService<GameSettings>();

        Session = new GameSession(seed: (ulong)Environment.TickCount64);
        Runner = new SessionRunner(Session);

        var profile = services.GetService<InputProfile>();
        for (int i = 0; i < _inputSources.Count; i++)
        {
            var player = new LocalPlayer(new PlayerId(i + 1), _inputSources[i], profile);
            _players.Add(player);
            Runner.AddPlayer(player.Id, player.Commands);
        }
        yield return 1f;
    }

    protected override void Enter()
    {
        // No menus are open during play
        ListenTo(Array.Empty<InputSource>());

        // The level, with the HUD over it; the HUD reacts to the simulation's events
        _levelView = Show(new LevelView(Battlefield));
        var hud = Show(new HudLayer(_players, Battlefield));
        GameEventRaised += hud.OnGameEvent;

        // So do the sounds and the game feel (shake, rumble, hit-stop)
        var feedback = new GameFeedback(_players, _levelView, _rumble, _hitStop);
        GameEventRaised += feedback.OnGameEvent;
        GameEventRaised += gameEvent => _audio.OnGameEvent(gameEvent, Session.World);
        _audio.PlayGameMusic();
    }

    protected override void Covered()
    {
        // Paused: don't leave gamepads vibrating
        _rumble.StopAll();
    }

    protected override void Uncovered()
    {
        // Back from the pause menu
        ListenTo(Array.Empty<InputSource>());
    }

    protected override void Exit()
    {
        // Stop the game's sounds and vibration (the music changes with the next state)
        _rumble.StopAll();
        _audio?.Audio.Effects.StopAll();

        // The level view keeps drawing (with this content) while it fades out, so only
        // unload the content once it is gone
        void Unload()
        {
            Battlefield?.Unload();
            _content?.Unload();
        }
        if (_levelView != null)
            _levelView.Removed += Unload;
        else
            Unload();
    }

    public override void Update(GameTime gameTime)
    {
        if (!IsTop && _pausePolicy.PausesSimulation)
            return;

        // Read each player's input before running the simulation, so it responds immediately.
        // Players using a menu (e.g. the pause menu, if it doesn't stop the simulation) stand still.
        foreach (var player in _players)
        {
            if (IsTop || !Ui.Sources.Contains(player.Input))
                player.SubmitCommand<GameAction>(Input);
            else
                player.SubmitIdle();
        }

        // A hit-stop freezes the simulation for a moment by swallowing the elapsed time
        var elapsed = _hitStop.Apply(gameTime.ElapsedGameTime);
        Runner.Update(elapsed);

        // MechaHorde's level reads the keyboard and mouse itself, so it only runs while no
        // menu is open over it
        if (IsTop)
            Battlefield.Update(new GameTime(gameTime.TotalGameTime, elapsed), Game.GraphicsDevice.Viewport);

        // Pass on what happened in the simulation, then clear the events for the next frame
        foreach (var gameEvent in Session.Events.Pending)
            GameEventRaised?.Invoke(gameEvent);
        Session.Events.Clear();

        _rumble.Enabled = _settings?.Vibration ?? true;
        _rumble.Update(gameTime);

        if (IsTop)
            CheckForPause();
    }

    private void CheckForPause()
    {
        foreach (var player in _players)
        {
            if (player.Input.WasPressed(StandardControls.Pause, Input) && _pausePolicy.CanPause(player, _players))
            {
                Flow.Push(new PausedState(this, player));
                return;
            }

            // Pause when a player's gamepad is unplugged (only once, not every frame it stays unplugged)
            bool lost = player.Input.HasLostDevice(Input);
            if (lost && _lostDevice.Add(player) && _pausePolicy.PauseWhenDeviceLost)
            {
                Flow.Push(new PausedState(this, player));
                return;
            }
            if (!lost)
                _lostDevice.Remove(player);
        }
    }
}
