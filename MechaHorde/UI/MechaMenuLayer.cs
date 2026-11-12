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
/// of the screen, under a gold title in the top left. The selected (or hovered) button glows
/// blue and grows.
/// </summary>
/// <remarks>
/// This only changes how a <see cref="MenuLayer"/> looks and adds the mouse: everything else
/// (keyboard and gamepad navigation, sounds, submenus, prompts, transitions) is the
/// architecture's. Moving the mouse over a button selects it, and clicking it chooses it.
/// For items with a value (e.g. a volume), the left button adjusts the value up and the right
/// button adjusts it down.
/// </remarks>
public abstract class MechaMenuLayer : MenuLayer
{
    // The layout of the original MechaHorde main menu
    private const float LeftMargin = 50;
    private const float FirstButtonY = 150;
    private const float ButtonSpacing = 75;
    private const float ButtonScale = 0.2f;
    private static readonly Vector2 SelectedScale = new(1.25f, 1.1f);   // 0.25 x 0.22, relative to 0.2
    private static readonly Vector2 TextOffset = new(25, 15);
    private const float TextPadding = 50;

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
        int hovered = ItemAt(new Point(mouse.X, mouse.Y));

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
        float slide = Transition.State == TransitionState.Exiting ? Transition.Offset * 512 : -Transition.Offset * 256;

        SpriteBatch.Begin();

        // The title, in the top left
        SpriteBatch.DrawString(Theme.TitleFont, Title, new Vector2(2 + slide, 2), TitleColor * alpha);

        int visible = VisibleCount();
        ScrollToSelection(visible);
        for (int i = _firstVisible; i < _firstVisible + visible; i++)
        {
            var item = Menu.Items[i];
            var bounds = ButtonBounds(i);
            bool selected = HasFocus && i == Menu.SelectedIndex && item.IsEnabled;

            var color = !item.IsEnabled ? Theme.DisabledColor : selected ? Theme.SelectedColor : Color.White;
            var size = selected ? new Vector2(bounds.Width * SelectedScale.X, bounds.Height * SelectedScale.Y)
                                : new Vector2(bounds.Width, bounds.Height);
            var position = new Vector2(bounds.X + slide, bounds.Y);

            SpriteBatch.Draw(_button, new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y),
                color * alpha);
            SpriteBatch.DrawString(font, item.Text, position + TextOffset,
                (item.IsEnabled ? Theme.TextColor : Theme.DisabledColor) * alpha);
        }

        // Markers when there are more buttons than fit on screen
        if (_firstVisible > 0)
            SpriteBatch.DrawString(font, "^ more", new Vector2(LeftMargin + slide, FirstButtonY - font.LineSpacing - 4),
                Theme.DisabledColor * alpha);
        if (_firstVisible + visible < Menu.Items.Count)
            SpriteBatch.DrawString(font, "v more", new Vector2(LeftMargin + slide, ButtonY(visible)),
                Theme.DisabledColor * alpha);

        SpriteBatch.End();
    }

    /// <summary>
    /// Where an item's button is (unselected), or an empty rectangle if it is scrolled out of view
    /// </summary>
    private Rectangle ButtonBounds(int index)
    {
        int row = index - _firstVisible;
        if (row < 0 || row >= VisibleCount())
            return Rectangle.Empty;

        // Buttons stretch to fit long text (e.g. "Music volume: 7")
        float width = Math.Max(_button.Width * ButtonScale,
            Theme.TextFont.MeasureString(Menu.Items[index].Text).X + TextPadding);
        return new Rectangle((int)LeftMargin, (int)ButtonY(row), (int)width, (int)(_button.Height * ButtonScale));
    }

    private static float ButtonY(int row) => FirstButtonY + row * ButtonSpacing;

    /// <summary>
    /// The index of the item whose button is at a point on screen, or -1 if none
    /// </summary>
    private int ItemAt(Point point)
    {
        if (_button == null)
            return -1;
        for (int i = _firstVisible; i < Math.Min(Menu.Items.Count, _firstVisible + VisibleCount()); i++)
        {
            if (ButtonBounds(i).Contains(point))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// How many buttons fit on screen (long menus, like the controls, scroll)
    /// </summary>
    private int VisibleCount()
    {
        float space = Game.GraphicsDevice.Viewport.Height - FirstButtonY - ButtonSpacing;
        return Math.Clamp((int)(space / ButtonSpacing) + 1, 1, Math.Max(1, Menu.Items.Count));
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
