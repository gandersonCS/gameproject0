using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using CIS580.Input;

namespace CIS580.UI;

/// <summary>
/// A stack of user interface layers, drawn bottom to top, with input going to the top.
/// </summary>
/// <remarks>
/// Each frame, the stack works down from the top layer to decide:
/// <list type="bullet">
/// <item>which layers are covered: everything beneath a layer that isn't a popup (except backdrops)</item>
/// <item>which layers have focus: the topmost layer that accepts input, and the layers beneath it
/// for as long as they pass input through</item>
/// </list>
/// Layers then transition toward visible or hidden, layers that finished closing are removed,
/// and each layer is updated, given input if it has focus, and drawn if it is on screen.
/// The game must register <see cref="InputState"/> and <see cref="UiInput"/> services before
/// creating the stack.
/// </remarks>
public sealed class LayerStack : DrawableGameComponent
{
    private readonly List<Layer> _layers = new();
    private readonly List<Layer> _snapshot = new();
    private readonly Func<ContentManager, UiTheme> _createTheme;

    private Layer[] _pendingReplacement;
    private bool _isLoaded;

    /// <summary>
    /// The layers, bottom to top
    /// </summary>
    public IReadOnlyList<Layer> Layers => _layers;

    /// <summary>
    /// The look of the user interface
    /// </summary>
    public UiTheme Theme { get; private set; }

    /// <summary>
    /// The state of the input devices
    /// </summary>
    public InputState Input { get; }

    /// <summary>
    /// Menu navigation input
    /// </summary>
    public UiInput UiInput { get; }

    /// <summary>
    /// A SpriteBatch shared by all layers
    /// </summary>
    public SpriteBatch SpriteBatch { get; private set; }

    /// <summary>
    /// A 1x1 white texture, for drawing solid rectangles
    /// </summary>
    public Texture2D BlankTexture { get; private set; }

    /// <summary>
    /// Creates a layer stack
    /// </summary>
    /// <param name="game">The game</param>
    /// <param name="createTheme">Creates the UI theme once content can be loaded</param>
    public LayerStack(Game game, Func<ContentManager, UiTheme> createTheme) : base(game)
    {
        _createTheme = createTheme ?? throw new ArgumentNullException(nameof(createTheme));
        Input = game.Services.GetService<InputState>()
            ?? throw new InvalidOperationException("Register an InputState service before creating the LayerStack.");
        UiInput = game.Services.GetService<UiInput>()
            ?? throw new InvalidOperationException("Register a UiInput service before creating the LayerStack.");
    }

    protected override void LoadContent()
    {
        SpriteBatch = new SpriteBatch(GraphicsDevice);
        BlankTexture = new Texture2D(GraphicsDevice, 1, 1);
        BlankTexture.SetData(new[] { Color.White });
        Theme = _createTheme(Game.Content);

        _isLoaded = true;
        foreach (var layer in _layers)
            layer.Load();
    }

    protected override void UnloadContent()
    {
        foreach (var layer in _layers)
            layer.Unload();
    }

    /// <summary>
    /// Adds a layer to the top of the stack
    /// </summary>
    public void Push(Layer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        if (layer.Stack != null)
            throw new InvalidOperationException("The layer is already in a stack.");

        layer.Stack = this;
        layer.IsClosing = false;
        _layers.Add(layer);
        if (_isLoaded)
            layer.Load();
    }

    /// <summary>
    /// Closes a layer, and any layers it opened: they transition off, then are removed
    /// </summary>
    public void Close(Layer layer)
    {
        if (layer.Stack == this)
            layer.MarkClosing();
    }

    /// <summary>
    /// Closes every layer, then, once they have all transitioned off, pushes the given layers.
    /// Use this to move between major parts of the game, such as from the menus into play.
    /// </summary>
    public void ReplaceAll(params Layer[] layers)
    {
        foreach (var layer in _layers)
            layer.IsClosing = true;
        _pendingReplacement = layers;
    }

    /// <summary>
    /// Fills the screen with a color (e.g. translucent black, to dim the layers beneath a popup)
    /// </summary>
    public void FillScreen(Color color)
    {
        SpriteBatch.Begin();
        SpriteBatch.Draw(BlankTexture, GraphicsDevice.Viewport.Bounds, color);
        SpriteBatch.End();
    }

    public override void Update(GameTime gameTime)
    {
        UpdateLayerStates(_layers, gameTime.ElapsedGameTime, Game.IsActive);

        RemoveClosedLayers();

        if (_pendingReplacement != null && _layers.Count == 0)
        {
            var replacement = _pendingReplacement;
            _pendingReplacement = null;
            foreach (var layer in replacement)
                Push(layer);

            // Loading the new layers may have taken a while; don't try to catch up on that time
            Game.ResetElapsedTime();
        }

        // Update and handle input from the top down, using a copy of the list in case
        // layers are added or closed along the way
        _snapshot.Clear();
        _snapshot.AddRange(_layers);
        for (int i = _snapshot.Count - 1; i >= 0; i--)
        {
            var layer = _snapshot[i];
            layer.Update(gameTime);
            if (layer.HasFocus && !layer.IsClosing)
                layer.HandleInput(gameTime);
        }
    }

    public override void Draw(GameTime gameTime)
    {
        _snapshot.Clear();
        _snapshot.AddRange(_layers);
        foreach (var layer in _snapshot)
        {
            if (layer.Transition.Position > 0f)
                layer.Draw(gameTime);
        }
    }

    /// <summary>
    /// Decides which layers are covered and which have focus, from the top down, and advances
    /// their transitions accordingly.
    /// </summary>
    /// <param name="layers">The layers, bottom to top</param>
    /// <param name="elapsed">The time since the last update</param>
    /// <param name="windowActive">Whether the game window has focus; if not, no layer does</param>
    internal static void UpdateLayerStates(IReadOnlyList<Layer> layers, TimeSpan elapsed, bool windowActive)
    {
        bool covered = false;
        bool focusAvailable = windowActive;

        for (int i = layers.Count - 1; i >= 0; i--)
        {
            var layer = layers[i];

            layer.IsCovered = covered && !layer.IsBackdrop;
            layer.Transition.Update(elapsed, !layer.IsClosing && !layer.IsCovered);

            layer.HasFocus = focusAvailable && !layer.IsClosing && !layer.IsCovered
                             && layer.InputMode != InputMode.None;
            if (layer.HasFocus && layer.InputMode == InputMode.Capture)
                focusAvailable = false;

            if (!layer.IsPopup && !layer.IsClosing)
                covered = true;
        }
    }

    private void RemoveClosedLayers()
    {
        for (int i = _layers.Count - 1; i >= 0; i--)
        {
            var layer = _layers[i];
            if (layer.IsClosing && layer.Transition.State == TransitionState.Hidden)
            {
                _layers.RemoveAt(i);
                if (_isLoaded)
                    layer.Unload();
                layer.Stack = null;
                layer.RaiseRemoved();
            }
        }
    }
}
