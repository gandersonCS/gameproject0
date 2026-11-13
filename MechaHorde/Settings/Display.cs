using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MechaHorde.Settings;

/// <summary>
/// The game's window: fullscreen (borderless, at the desktop's resolution) or a resizable
/// window that fits the player's screen.
/// </summary>
/// <remarks>
/// Nothing in the game assumes a resolution: the level and the menus scale themselves to
/// whatever size the screen or window is, so any size works. F11 switches
/// windowed, as does the Fullscreen option in the options menu.
/// </remarks>
public sealed class Display
{
    /// <summary>
    /// A new window covers this share of the desktop
    /// </summary>
    private const float WindowShareOfDesktop = 0.8f;

    private readonly Game _game;
    private readonly GraphicsDeviceManager _graphics;
    private readonly GameSettings _settings;
    private KeyboardState _previousKeyboard;
    private bool _applying;

    public Display(Game game, GraphicsDeviceManager graphics, GameSettings settings)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        // Borderless fullscreen: switching is instant, and alt-tabbing out works smoothly
        _graphics.HardwareModeSwitch = false;
        _game.Window.AllowUserResizing = true;
        _game.Window.ClientSizeChanged += OnWindowResized;
    }

    /// <summary>
    /// Whether the game fills the screen
    /// </summary>
    public bool IsFullscreen => _settings.Fullscreen;

    /// <summary>
    /// Sets up the window as the player last had it. Call from the game's Initialize.
    /// </summary>
    public void Apply() => SetFullscreen(_settings.Fullscreen);

    /// <summary>
    /// Switches between fullscreen and a window (and remembers the choice in the settings)
    /// </summary>
    public void SetFullscreen(bool fullscreen)
    {
        _settings.Fullscreen = fullscreen;

        var desktop = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        _applying = true;
        if (fullscreen)
        {
            _graphics.PreferredBackBufferWidth = desktop.Width;
            _graphics.PreferredBackBufferHeight = desktop.Height;
            _graphics.IsFullScreen = true;
        }
        else
        {
            // A 16:9 window, as big as fits comfortably on the desktop
            int height = (int)(Math.Min(desktop.Height, desktop.Width * 9 / 16f) * WindowShareOfDesktop);
            _graphics.PreferredBackBufferWidth = height * 16 / 9;
            _graphics.PreferredBackBufferHeight = height;
            _graphics.IsFullScreen = false;
        }
        _graphics.ApplyChanges();
        _applying = false;

        if (!fullscreen)
        {
            // Center the new window on the desktop
            _game.Window.Position = new Point(
                (desktop.Width - _graphics.PreferredBackBufferWidth) / 2,
                (desktop.Height - _graphics.PreferredBackBufferHeight) / 2);
        }
    }

    /// <summary>
    /// Checks for the fullscreen shortcut (F11). Call once per frame.
    /// </summary>
    public void Update()
    {
        var keyboard = Keyboard.GetState();
        bool toggle = Pressed(keyboard, Keys.F11);
        _previousKeyboard = keyboard;

        if (toggle && _game.IsActive)
            SetFullscreen(!_settings.Fullscreen);
    }

    private bool Pressed(KeyboardState keyboard, Keys key) => keyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);

    /// <summary>
    /// When the player resizes the window, draw at its new size
    /// </summary>
    private void OnWindowResized(object sender, EventArgs e)
    {
        if (_applying || _graphics.IsFullScreen)
            return;

        var size = _game.Window.ClientBounds;
        if (size.Width <= 0 || size.Height <= 0)
            return;    // Minimized
        if (size.Width == _graphics.PreferredBackBufferWidth && size.Height == _graphics.PreferredBackBufferHeight)
            return;

        _applying = true;
        _graphics.PreferredBackBufferWidth = size.Width;
        _graphics.PreferredBackBufferHeight = size.Height;
        _graphics.ApplyChanges();
        _applying = false;
    }
}
