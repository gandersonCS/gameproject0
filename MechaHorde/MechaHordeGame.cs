using System;
using System.Linq;
using Microsoft.Xna.Framework;
using CIS580.Audio;
using CIS580.Flow;
using CIS580.Input;
using CIS580.Settings;
using CIS580.UI;
using MechaHorde.Audio;
using MechaHorde.Gameplay;
using MechaHorde.Input;
using MechaHorde.Settings;
using MechaHorde.States;
using MechaHorde.UI;

namespace MechaHorde;

/// <summary>
/// The main game class for MechaHorde. This is deliberately very simple: it creates the shared
/// services, and the app's flow (its high-level states) and layer stack (its user interface)
/// do the rest.
/// </summary>
/// <remarks>
/// Each frame runs in this order:
/// <list type="number">
/// <item>read the input devices</item>
/// <item>update the app's flow: change state if requested, then update the states (which runs the game)</item>
/// <item>update the user interface layers</item>
/// <item>update the audio (applying any changed settings)</item>
/// <item>draw the layers</item>
/// </list>
/// </remarks>
public class MechaHordeGame : Game
{
    private GraphicsDeviceManager _graphics;
    private readonly InputState _input;
    private readonly UiInput _uiInput;
    private readonly LayerStack _layers;
    private readonly AppFlow _flow;
    private readonly AudioManager _audio;
    private GameAudio _gameAudio;
    private readonly UserSettings _settings;
    private readonly Display _display;

    /// <param name="args">
    /// Command-line arguments. "--play" skips the menus and starts a game immediately,
    /// which is handy while working on gameplay.
    /// </param>
    public MechaHordeGame(string[] args)
    {
        _graphics = new GraphicsDeviceManager(this);
        // Until the display is set up (in Initialize): everything scales to whatever size the
        // screen turns out to be, so this is only a starting point
        _graphics.PreferredBackBufferWidth = Constants.GAME_WIDTH;
        _graphics.PreferredBackBufferHeight = Constants.GAME_HEIGHT;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "MechaHorde";

        // Services shared with the rest of the game
        _input = new InputState();
        Services.AddService(_input);

        _uiInput = new UiInput(_input);
        Services.AddService(_uiInput);

        // The player's settings and controls, as they last left them
        _settings = UserSettings.Load(new SettingsStore(SettingsStore.DefaultDirectory("MechaHorde")));
#if DEBUG
        Console.WriteLine($"[Settings] {_settings.Store.Directory}"
            + (_settings.Store.LastError != null ? $" (couldn't load: {_settings.Store.LastError.Message})" : ""));
#endif
        Services.AddService(_settings);
        Services.AddService(_settings.Game);
        Services.AddService(_settings.Input);        // The local players' input preferences (e.g. invert Y)
        Services.AddService(_settings.Controls);
        Services.AddService(new PausePolicy());      // Change its settings to change who can pause, etc.

        // Fullscreen or a window, as the player last had it (F11 switches)
        _display = new Display(this, _graphics, _settings.Game);
        Services.AddService(_display);

        _audio = new AudioManager();
        Services.AddService(_audio);

        // The layer stack retrieves the input services, so it must be created after them.
        // It loads the theme (including the menus' sounds) when its content loads.
        _layers = new LayerStack(this, content => GameTheme.Create(content, _audio));
        Components.Add(_layers);

        _flow = new AppFlow(this, _layers, _uiInput);
#if DEBUG
        _flow.StateChanged += change => Console.WriteLine($"[AppFlow] {change}");
#endif

        if (args.Contains("--play"))
            _flow.Change(new LoadingState(new InGameState(new[] { _settings.Controls.SinglePlayer(0) })));
        else
            _flow.Change(new MainMenuState());
    }

    protected override void Initialize()
    {
        _display.Apply();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        // The game's music and sound effects
        _gameAudio = new GameAudio(_audio, Content, Services.GetService<GameSettings>());
        Services.AddService(_gameAudio);
    }

    protected override void Update(GameTime gameTime)
    {
        // Read the input devices once per frame, before anything uses them
        _input.Update();
        _uiInput.Update(gameTime.ElapsedGameTime);
        _display.Update();

        _flow.Update(gameTime);

        base.Update(gameTime);    // Updates the LayerStack component

        _gameAudio.Update(gameTime);
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        // Settings are saved when the options menu closes; this catches anything changed since
        _settings.Save();
        base.OnExiting(sender, args);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        base.Draw(gameTime);    // The real drawing happens inside the LayerStack component
    }
}
