using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using CIS580.Input;
using CIS580.UI;
using CIS580.UI.Widgets;

namespace MechaHorde.UI;

/// <summary>
/// A menu in MechaHorde's style: its items are sci-fi panel buttons in a column down the left
/// of the screen, under a gold title in the top left. The selected (or hovered) button grows,
/// lights up, and gets a colored edge.
/// </summary>
/// <remarks>
/// <para>
/// This only changes how a <see cref="MenuLayer"/> looks and adds the mouse: everything else
/// (keyboard and gamepad navigation, sounds, submenus, prompts, transitions) is the
/// architecture's. Moving the mouse over a button selects it, and clicking it chooses it.
/// For items with a value (e.g. a volume), the left button adjusts the value up and the right
/// button adjusts it down.
/// </para>
/// <para>
/// Everything is laid out in UI units (a screen 1080 units tall, see
/// <see cref="LayerStack.ReferenceHeight"/>), so the menu takes up the same share of the
/// screen at any resolution. Buttons are as big as <see cref="MaxButtonHeight"/>, and shrink
/// (down to <see cref="MinButtonHeight"/>) so long menus still fit; longer menus scroll.
/// </para>
/// </remarks>
public abstract class MechaMenuLayer : MenuLayer
{
    // The layout, in UI units (1080 tall)
    private const float LeftMargin = 64;
    private const float TitleY = 40;
    private const float FirstButtonY = 250;
    private const float BottomMargin = 60;
    private const float MaxButtonHeight = 100;
    private const float MinButtonHeight = 64;
    /// <summary>The gap between buttons, as a fraction of a button's height</summary>
    private const float GapFraction = 0.24f;
    private const float MinButtonWidth = 460;
    /// <summary>How far the text sits in from the button's left edge, as a fraction of its height</summary>
    private const float TextInset = 0.45f;
    /// <summary>The text is drawn at full size on the tallest buttons, and smaller on smaller ones</summary>
    private const float TextHeightForFullSize = 100;
    private const float SelectedGrow = 1.08f;
    private const float SelectedNudge = 24;
    private const float AccentWidth = 10;

    private Texture2D _button;
    private int _firstVisible;

    protected MechaMenuLayer(string title) : base(title)
    {
    }

    /// <summary>
    /// The title's color
    /// </summary>
    protected Color TitleColor { get; set; } = Color.Gold;

    protected override void Load()
    {
        base.Load();
        _button = Game.Content.Load<Texture2D>("Textures/Menu/futureui1");
    }

    public override void HandleInput(GameTime gameTime)
    {
        HandleMouse();
        base.HandleInput(gameTime);
    }

    /// <summary>
    /// Hovering selects a button; clicking chooses it
    /// </summary>
    private void HandleMouse()
    {
        // Only whoever is using the keyboard and mouse may use the mouse (e.g. not when a
        // gamepad player paused the game)
        var source = Ui.Sources.FirstOrDefault(s => s.UsesDevice(InputDevice.KeyboardMouse));
        if (source == null)
            return;

        var mouse = Input.Current.Mouse;
        var previous = Input.Previous.Mouse;
        int hovered = ItemAt(Stack.ScreenToUi(mouse.Position));

        // Select what the mouse moves onto (only when it moves, so the keyboard still works
        // while the pointer happens to rest on a button)
        if (mouse.Position != previous.Position && hovered >= 0 && hovered != Menu.SelectedIndex
            && Menu.Items[hovered].IsEnabled)
        {
            Menu.SelectedIndex = hovered;
            Theme.Sounds.Play(UiSound.Move);
        }

        if (hovered < 0 || !Menu.Items[hovered].IsEnabled)
            return;

        var item = Menu.Items[hovered];
        if (mouse.LeftButton == ButtonState.Pressed && previous.LeftButton == ButtonState.Released)
        {
            Menu.SelectedIndex = hovered;
            Theme.Sounds.Play(UiSound.Confirm);
            item.Activate(source);
        }
        else if (mouse.RightButton == ButtonState.Pressed && previous.RightButton == ButtonState.Released
            && item.Adjust(-1, source))
        {
            Theme.Sounds.Play(UiSound.Move);
        }
    }

    public override void Draw(GameTime gameTime)
    {
        DrawBackdrop(gameTime);

        var font = Theme.TextFont;
        float alpha = Transition.Alpha;
        float slide = Transition.State == TransitionState.Exiting ? Transition.Offset * 600 : -Transition.Offset * 300;
        float height = ButtonHeight();
        float textScale = Math.Min(1f, height / TextHeightForFullSize);

        SpriteBatch.Begin(transformMatrix: UiTransform);

        // The title, in the top left
        SpriteBatch.DrawString(Theme.TitleFont, Title, new Vector2(LeftMargin + slide, TitleY), TitleColor * alpha);

        int visible = VisibleCount();
        ScrollToSelection(visible);
        for (int i = _firstVisible; i < _firstVisible + visible; i++)
        {
            var item = Menu.Items[i];
            var bounds = ButtonBounds(i);
            bool selected = HasFocus && i == Menu.SelectedIndex && item.IsEnabled;

            // The selected button grows (about its middle, vertically) and moves out a little
            float grow = selected ? SelectedGrow : 1f;
            var size = new Vector2(bounds.Width * grow, bounds.Height * grow);
            var position = new Vector2(bounds.X + slide + (selected ? SelectedNudge : 0),
                bounds.Y - (size.Y - bounds.Height) / 2);
            var panel = new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y);

            // The panel art is translucent black; the selected one is drawn more solidly
            SpriteBatch.Draw(_button, panel, Color.White * (alpha * (selected ? 1f : 0.85f)));
            if (selected)
            {
                SpriteBatch.Draw(Stack.BlankTexture,
                    new Rectangle(panel.X - (int)AccentWidth - 6, panel.Y + panel.Height / 8, (int)AccentWidth, panel.Height * 3 / 4),
                    Theme.SelectedColor * alpha);
            }

            var textColor = !item.IsEnabled ? Theme.DisabledColor : selected ? Theme.SelectedColor : Theme.TextColor;
            float scale = textScale * (selected ? SelectedGrow : 1f);
            var textPosition = new Vector2(position.X + size.Y * TextInset, position.Y + size.Y / 2);
            SpriteBatch.DrawString(font, item.Text, textPosition, textColor * alpha, 0f,
                new Vector2(0, font.LineSpacing / 2f), scale, SpriteEffects.None, 0f);
        }

        // Markers when there are more buttons than fit on screen
        float markerScale = textScale * 0.7f;
        if (_firstVisible > 0)
            SpriteBatch.DrawString(font, "^ more", new Vector2(LeftMargin + slide, FirstButtonY - font.LineSpacing * markerScale - 8),
                Theme.DisabledColor * alpha, 0f, Vector2.Zero, markerScale, SpriteEffects.None, 0f);
        if (_firstVisible + visible < Menu.Items.Count)
            SpriteBatch.DrawString(font, "v more", new Vector2(LeftMargin + slide, ButtonY(visible, height) - height * GapFraction + 8),
                Theme.DisabledColor * alpha, 0f, Vector2.Zero, markerScale, SpriteEffects.None, 0f);

        SpriteBatch.End();
    }

    /// <summary>
    /// How tall the buttons are: as big as possible while the whole menu fits on screen,
    /// but never smaller than <see cref="MinButtonHeight"/> (long menus scroll instead)
    /// </summary>
    private float ButtonHeight()
    {
        int count = Math.Max(1, Menu.Items.Count);
        float space = UiSize.Y - FirstButtonY - BottomMargin;
        float fit = space / (count * (1 + GapFraction) - GapFraction);
        return Math.Clamp(fit, MinButtonHeight, MaxButtonHeight);
    }

    /// <summary>
    /// Where an item's button is (unselected), in UI units, or an empty rectangle if it is
    /// scrolled out of view
    /// </summary>
    private Rectangle ButtonBounds(int index)
    {
        int row = index - _firstVisible;
        if (row < 0 || row >= VisibleCount())
            return Rectangle.Empty;

        // Buttons stretch to fit long text (e.g. "Music volume: 7")
        float height = ButtonHeight();
        float textScale = Math.Min(1f, height / TextHeightForFullSize);
        float textWidth = Theme.TextFont.MeasureString(Menu.Items[index].Text).X * textScale;
        float width = Math.Max(MinButtonWidth, textWidth + height * TextInset * 2.5f);
        return new Rectangle((int)LeftMargin, (int)ButtonY(row, height), (int)width, (int)height);
    }

    private static float ButtonY(int row, float height) => FirstButtonY + row * height * (1 + GapFraction);

    /// <summary>
    /// The index of the item whose button is at a point (in UI units), or -1 if none
    /// </summary>
    private int ItemAt(Vector2 point)
    {
        if (_button == null)
            return -1;
        for (int i = _firstVisible; i < Math.Min(Menu.Items.Count, _firstVisible + VisibleCount()); i++)
        {
            var bounds = ButtonBounds(i);
            // Include the gap below each button, so there are no dead spots between them
            bounds.Height += (int)(bounds.Height * GapFraction);
            if (bounds.Contains(point))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// How many buttons fit on screen (long menus, like the controls, scroll)
    /// </summary>
    private int VisibleCount()
    {
        float height = ButtonHeight();
        float space = UiSize.Y - FirstButtonY - BottomMargin + height * GapFraction;
        // (The small extra stops rounding error dropping a button that exactly fits)
        return Math.Clamp((int)(space / (height * (1 + GapFraction)) + 0.01f), 1, Math.Max(1, Menu.Items.Count));
    }

    private void ScrollToSelection(int visible)
    {
        if (Menu.SelectedIndex < _firstVisible)
            _firstVisible = Menu.SelectedIndex;
        else if (Menu.SelectedIndex >= _firstVisible + visible)
            _firstVisible = Menu.SelectedIndex - visible + 1;
        _firstVisible = Math.Clamp(_firstVisible, 0, Math.Max(0, Menu.Items.Count - visible));
    }
}
