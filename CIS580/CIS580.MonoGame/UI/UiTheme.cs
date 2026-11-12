using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CIS580.UI;

/// <summary>
/// The sounds the user interface makes
/// </summary>
public enum UiSound
{
    /// <summary>The menu selection moved</summary>
    Move,
    /// <summary>A menu item was chosen, or a value changed</summary>
    Confirm,
    /// <summary>A menu was cancelled</summary>
    Cancel,
}

/// <summary>
/// Plays user interface sounds. The audio system provides an implementation.
/// </summary>
public interface IUiSounds
{
    void Play(UiSound sound);
}

/// <summary>
/// The look (and sound) of the user interface: fonts, colors, spacing, and timing.
/// </summary>
/// <remarks>
/// Menus and other widgets take their appearance from the theme rather than hard-coding it,
/// so a game can restyle its whole interface in one place. The game creates its theme when
/// content is loaded (see the game's GameTheme class).
/// </remarks>
public sealed class UiTheme
{
    /// <summary>
    /// The font for menu and prompt titles
    /// </summary>
    public SpriteFont TitleFont { get; init; }

    /// <summary>
    /// The font for menu items and other text
    /// </summary>
    public SpriteFont TextFont { get; init; }

    /// <summary>
    /// A texture stretched behind panels such as prompts (a plain color is used if null)
    /// </summary>
    public Texture2D PanelTexture { get; init; }

    public Color TitleColor { get; init; } = new(192, 192, 192);
    public float TitleScale { get; init; } = 1.25f;
    public Color TextColor { get; init; } = Color.White;
    public Color SelectedColor { get; init; } = Color.Yellow;
    public Color DisabledColor { get; init; } = Color.Gray;
    public Color PanelColor { get; init; } = new(64, 80, 110);

    /// <summary>
    /// How much a popup darkens the layers beneath it (0 to 1)
    /// </summary>
    public float DimOpacity { get; init; } = 2f / 3f;

    /// <summary>
    /// The space between a panel's edge and its text
    /// </summary>
    public Vector2 PanelPadding { get; init; } = new(32, 16);

    /// <summary>
    /// The distance from the top of the screen to the center of a menu's title
    /// </summary>
    public float TitleTop { get; init; } = 80;

    /// <summary>
    /// The distance from the top of the screen to a menu's first item
    /// </summary>
    public float MenuTop { get; init; } = 175;

    /// <summary>
    /// How long menus take to transition on and off
    /// </summary>
    public TimeSpan MenuTransition { get; init; } = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// How long popups take to transition on and off
    /// </summary>
    public TimeSpan PopupTransition { get; init; } = TimeSpan.FromSeconds(0.2);

    /// <summary>
    /// Plays UI sounds; replace with the audio system's implementation
    /// </summary>
    public IUiSounds Sounds { get; set; } = new SilentUiSounds();

    private sealed class SilentUiSounds : IUiSounds
    {
        public void Play(UiSound sound) { }
    }
}
