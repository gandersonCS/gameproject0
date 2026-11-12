using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CIS580.Input;
using CIS580.UI.Widgets;

namespace CIS580.UI;

/// <summary>
/// A layer showing a titled menu. Derive from this and add items to <see cref="Menu"/>.
/// </summary>
/// <example>
/// <code>
/// public class MainMenuLayer : MenuLayer
/// {
///     public MainMenuLayer() : base("Main Menu")
///     {
///         Menu.Add(new ButtonItem("Play", source => StartGame(source)))
///             .Add(new ButtonItem("Exit", OnCancel));
///     }
/// }
/// </code>
/// </example>
public abstract class MenuLayer : Layer
{
    /// <summary>
    /// The menu's items
    /// </summary>
    protected Menu Menu { get; } = new();

    /// <summary>
    /// The title shown above the menu
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Whether to darken the layers beneath the menu (popup menus always do). Useful for a
    /// full-screen menu shown over a backdrop such as the game world.
    /// </summary>
    public bool DimBackground { get; set; }

    /// <summary>
    /// Whether the menu scrolls when it has more items than fit on screen (true by default).
    /// Set to false to control <see cref="Widgets.Menu.MaxVisibleItems"/> yourself.
    /// </summary>
    public bool FitToScreen { get; set; } = true;

    protected MenuLayer(string title)
    {
        Title = title;
    }

    protected internal override void Load()
    {
        Transition.OnDuration = Theme.MenuTransition;
        Transition.OffDuration = Theme.MenuTransition;
    }

    public override void HandleInput(GameTime gameTime)
    {
        Menu.HandleInput(Ui, Theme);

        if (Ui.WasPressed(UiControl.Cancel, out var source))
        {
            Theme.Sounds.Play(UiSound.Cancel);
            OnCancel(source);
        }
    }

    /// <summary>
    /// Called when the player cancels the menu. By default, closes it.
    /// </summary>
    /// <param name="source">Who cancelled</param>
    protected virtual void OnCancel(InputSource source) => Close();

    public override void Update(GameTime gameTime)
    {
        Menu.Update(gameTime, HasFocus);
    }

    public override void Draw(GameTime gameTime)
    {
        var viewport = Game.GraphicsDevice.Viewport;
        float centerX = viewport.Width / 2f;

        DrawBackdrop(gameTime);

        // Long menus scroll, rather than running off the bottom of the screen
        if (FitToScreen)
        {
            float space = viewport.Height - Theme.MenuTop - Theme.TextFont.LineSpacing;
            Menu.MaxVisibleItems = Math.Max(1, (int)(space / Theme.TextFont.LineSpacing));
        }

        SpriteBatch.Begin();

        Menu.Draw(SpriteBatch, Theme, new Vector2(centerX, Theme.MenuTop), Transition, gameTime);

        // The title slides down into place as the menu transitions on
        var font = Theme.TitleFont;
        var titlePosition = new Vector2(centerX, Theme.TitleTop - Transition.Offset * 100);
        var titleOrigin = font.MeasureString(Title) / 2;
        SpriteBatch.DrawString(font, Title, titlePosition, Theme.TitleColor * Transition.Alpha, 0,
            titleOrigin, Theme.TitleScale, SpriteEffects.None, 0);

        SpriteBatch.End();
    }

    /// <summary>
    /// Draws behind the menu. By default, popup menus (and those with <see cref="DimBackground"/>
    /// set) dim the layers beneath them.
    /// </summary>
    protected virtual void DrawBackdrop(GameTime gameTime)
    {
        if (IsPopup || DimBackground)
            Stack.FillScreen(Color.Black * (Theme.DimOpacity * Transition.Alpha));
    }
}
