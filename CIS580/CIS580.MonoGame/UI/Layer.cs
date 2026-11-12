using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CIS580.Input;

namespace CIS580.UI;

/// <summary>
/// How a layer takes part in input
/// </summary>
public enum InputMode
{
    /// <summary>
    /// The layer receives input, and layers beneath it do not (e.g. a menu)
    /// </summary>
    Capture,

    /// <summary>
    /// The layer receives input, and so does the next layer beneath it
    /// </summary>
    PassThrough,

    /// <summary>
    /// The layer never receives input (e.g. a HUD or background)
    /// </summary>
    None,
}

/// <summary>
/// One layer of the user interface, such as a background, a menu, a HUD, or a popup.
/// Layers are stacked in a <see cref="LayerStack"/>, and drawn bottom to top.
/// </summary>
/// <remarks>
/// <para>
/// By default a layer covers the layers beneath it: when it comes on screen, they transition
/// off (and come back when it closes). A <see cref="IsPopup">popup</see> instead appears over the
/// layers beneath, leaving them visible, and a <see cref="IsBackdrop">backdrop</see> is never
/// covered.
/// </para>
/// <para>
/// Layers are the view of the game: they present state and turn input into requests, but the
/// state of the game itself belongs to the simulation (and, for menus and pausing, the app's flow).
/// </para>
/// </remarks>
public abstract class Layer
{
    // Layers this layer opened (e.g. a menu's prompt), which close along with it
    private readonly List<Layer> _children = new();

    /// <summary>
    /// The stack this layer is in
    /// </summary>
    public LayerStack Stack { get; internal set; }

    /// <summary>
    /// The layer's on and off screen animation
    /// </summary>
    public Transition Transition { get; } = new();

    /// <summary>
    /// If true, layers beneath stay visible behind this one (e.g. a pause menu over the game)
    /// </summary>
    public bool IsPopup { get; protected set; }

    /// <summary>
    /// If true, this layer stays visible even when covered (e.g. a background behind menus)
    /// </summary>
    public bool IsBackdrop { get; protected set; }

    /// <summary>
    /// How this layer takes part in input
    /// </summary>
    public InputMode InputMode { get; protected set; } = InputMode.Capture;

    /// <summary>
    /// True once the layer has been asked to close; it is removed when it finishes transitioning off
    /// </summary>
    public bool IsClosing { get; internal set; }

    /// <summary>
    /// True if a layer above is covering this one
    /// </summary>
    public bool IsCovered { get; internal set; }

    /// <summary>
    /// True if this layer is receiving input this frame
    /// </summary>
    public bool HasFocus { get; internal set; }

    /// <summary>
    /// The game this layer belongs to
    /// </summary>
    protected Game Game => Stack.Game;

    /// <summary>
    /// The look of the user interface
    /// </summary>
    protected UiTheme Theme => Stack.Theme;

    /// <summary>
    /// Menu navigation input
    /// </summary>
    protected UiInput Ui => Stack.UiInput;

    /// <summary>
    /// The state of the input devices
    /// </summary>
    protected InputState Input => Stack.Input;

    /// <summary>
    /// A SpriteBatch shared by all layers
    /// </summary>
    protected SpriteBatch SpriteBatch => Stack.SpriteBatch;

    /// <summary>
    /// Called when the layer is added to the stack, once graphics are available. Load content here.
    /// </summary>
    protected internal virtual void Load() { }

    /// <summary>
    /// Called when the layer is removed from the stack. Unload content here.
    /// </summary>
    protected internal virtual void Unload() { }

    /// <summary>
    /// Updates the layer. Called every frame, whether or not the layer has focus or is visible.
    /// </summary>
    public virtual void Update(GameTime gameTime) { }

    /// <summary>
    /// Handles input. Only called on frames when the layer has focus.
    /// </summary>
    public virtual void HandleInput(GameTime gameTime) { }

    /// <summary>
    /// Draws the layer. Only called when the layer is at least partly on screen.
    /// </summary>
    public virtual void Draw(GameTime gameTime) { }

    /// <summary>
    /// Raised once the layer has finished closing and been removed from the stack
    /// (e.g. to unload content the layer was drawing with)
    /// </summary>
    public event Action Removed;

    /// <summary>
    /// Closes this layer, and any layers it opened: they transition off, then are removed from the stack
    /// </summary>
    public void Close() => Stack?.Close(this);

    /// <summary>
    /// Opens a layer on top of the stack that belongs to this one (e.g. a menu's submenu or
    /// prompt), so it closes when this layer does
    /// </summary>
    protected T Open<T>(T child) where T : Layer
    {
        _children.Add(child);
        Stack.Push(child);
        return child;
    }

    /// <summary>
    /// Draws a message in a panel in the middle of the screen, dimming the layers beneath
    /// (as prompts and other popups do)
    /// </summary>
    protected void DrawMessagePanel(string text)
    {
        Stack.FillScreen(Color.Black * (Theme.DimOpacity * Transition.Alpha));

        var viewport = Game.GraphicsDevice.Viewport;
        var font = Theme.TextFont;
        var textSize = font.MeasureString(text);
        var textPosition = (new Vector2(viewport.Width, viewport.Height) - textSize) / 2;

        var padding = Theme.PanelPadding;
        var panel = new Rectangle(
            (int)(textPosition.X - padding.X), (int)(textPosition.Y - padding.Y),
            (int)(textSize.X + padding.X * 2), (int)(textSize.Y + padding.Y * 2));

        SpriteBatch.Begin();
        if (Theme.PanelTexture != null)
            SpriteBatch.Draw(Theme.PanelTexture, panel, Color.White * Transition.Alpha);
        else
            SpriteBatch.Draw(Stack.BlankTexture, panel, Theme.PanelColor * Transition.Alpha);
        SpriteBatch.DrawString(font, text, textPosition, Theme.TextColor * Transition.Alpha);
        SpriteBatch.End();
    }

    internal void MarkClosing()
    {
        IsClosing = true;
        foreach (var child in _children)
            child.MarkClosing();
        _children.Clear();
    }

    internal void RaiseRemoved() => Removed?.Invoke();
}
