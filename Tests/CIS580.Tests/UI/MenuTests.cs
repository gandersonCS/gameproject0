using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using CIS580.Input;
using CIS580.Tests.Input;
using CIS580.UI;
using CIS580.UI.Widgets;

namespace CIS580.Tests.UI;

public class MenuTests
{
    private sealed class RecordingSounds : IUiSounds
    {
        public readonly List<UiSound> Played = new();
        public void Play(UiSound sound) => Played.Add(sound);
    }

    private readonly FakeInput _fake = new();
    private readonly UiInput _ui;
    private readonly RecordingSounds _sounds = new();
    private readonly UiTheme _theme;
    private readonly InputSource _keyboard = new(InputDevice.KeyboardMouse, StandardSchemes.Keyboard());

    public MenuTests()
    {
        _ui = new UiInput(_fake.State);
        _ui.Sources.Add(_keyboard);
        _theme = new UiTheme { Sounds = _sounds };
    }

    /// <summary>
    /// Presses keys for one frame (releasing them on the next) and lets the menu handle them
    /// </summary>
    private void Press(Menu menu, params Keys[] keys)
    {
        _fake.Keys(keys);
        _ui.Update(TimeSpan.FromMilliseconds(16));
        menu.HandleInput(_ui, _theme);

        _fake.Keys();
        _ui.Update(TimeSpan.FromMilliseconds(16));
    }

    [Fact]
    public void UpAndDown_MoveTheSelection_Wrapping()
    {
        var menu = new Menu().Add(new ButtonItem("a", null)).Add(new ButtonItem("b", null)).Add(new ButtonItem("c", null));

        Press(menu, Keys.Down);
        Assert.Equal(1, menu.SelectedIndex);

        Press(menu, Keys.Up);
        Press(menu, Keys.Up);
        Assert.Equal(2, menu.SelectedIndex);
        Assert.Contains(UiSound.Move, _sounds.Played);
    }

    [Fact]
    public void DisabledItems_AreSkipped()
    {
        var menu = new Menu()
            .Add(new ButtonItem("a", null))
            .Add(new ButtonItem("b", null) { IsEnabled = false })
            .Add(new ButtonItem("c", null));

        Press(menu, Keys.Down);

        Assert.Equal(2, menu.SelectedIndex);
    }

    [Fact]
    public void Confirm_ActivatesTheSelectedItem_WithWhoChoseIt()
    {
        InputSource chosenBy = null;
        var menu = new Menu().Add(new ButtonItem("a", source => chosenBy = source));

        Press(menu, Keys.Enter);

        Assert.Same(_keyboard, chosenBy);
        Assert.Contains(UiSound.Confirm, _sounds.Played);
    }

    [Fact]
    public void LeftAndRight_AdjustTheSelectedItem()
    {
        bool sound = true;
        var menu = new Menu().Add(new ToggleItem("Sound", () => sound, v => sound = v));

        Press(menu, Keys.Right);
        Assert.False(sound);
        Assert.Equal("Sound: Off", menu.Selected.Text);
    }

    [Fact]
    public void ChoiceItem_StepsAndWrapsThroughItsChoices()
    {
        string color = "Red";
        var item = new ChoiceItem<string>("Color", new[] { "Red", "Green", "Blue" }, () => color, v => color = v);

        item.Adjust(-1, null);
        Assert.Equal("Blue", color);

        item.Activate(null);
        Assert.Equal("Red", color);
        Assert.Equal("Color: Red", item.Text);
    }

    [Fact]
    public void RangeItem_ClampsWhenAdjusted_AndWrapsWhenActivated()
    {
        int lives = 5;
        var item = new RangeItem("Lives", 1, 5, 1, () => lives, v => lives = v);

        Assert.False(item.Adjust(1, null));     // Already at the maximum
        Assert.Equal(5, lives);

        item.Activate(null);
        Assert.Equal(1, lives);
    }

    [Fact]
    public void DescribeBindings_ListsTheBoundInputs()
    {
        Assert.Equal("Enter, Space", _ui.DescribeBindings(UiControl.Confirm));
        Assert.Equal("Esc, Backspace", _ui.DescribeBindings(UiControl.Cancel));
    }
}
