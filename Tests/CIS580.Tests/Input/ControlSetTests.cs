using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework.Input;
using CIS580.Input;
using CIS580.Settings;

namespace CIS580.Tests.Input;

public class ControlSetTests
{
    private static readonly string[] Movement =
        { StandardControls.MoveUp, StandardControls.MoveDown, StandardControls.MoveLeft, StandardControls.MoveRight };

    private static ControlSet NewSet() => new(new[] { StandardSchemes.Keyboard(), StandardSchemes.GamePad() });

    private static KeyBinding Key(Keys key) => new(key);

    [Fact]
    public void Rebind_ReplacesTheControlsBindings()
    {
        var scheme = StandardSchemes.Keyboard();
        scheme.Rebind(StandardControls.MoveUp, Key(Keys.I), Movement);
        Assert.Equal(new[] { Key(Keys.I) }, scheme.GetBindings(StandardControls.MoveUp));
    }

    [Fact]
    public void Rebind_TakesTheInputFromAnotherControlInTheGroup()
    {
        var scheme = StandardSchemes.Keyboard();     // MoveDown is S and Down

        var displaced = scheme.Rebind(StandardControls.MoveUp, Key(Keys.S), Movement);

        Assert.Equal(new[] { StandardControls.MoveDown }, displaced);
        Assert.Equal(new[] { Key(Keys.Down) }, scheme.GetBindings(StandardControls.MoveDown));
    }

    [Fact]
    public void Rebind_SwapsWhenTheOtherControlWouldBeLeftUnbound()
    {
        var scheme = StandardSchemes.KeyboardLeft();     // MoveUp is W, MoveDown is S

        scheme.Rebind(StandardControls.MoveUp, Key(Keys.S), Movement);

        Assert.Equal(new[] { Key(Keys.S) }, scheme.GetBindings(StandardControls.MoveUp));
        Assert.Equal(new[] { Key(Keys.W) }, scheme.GetBindings(StandardControls.MoveDown));
    }

    [Fact]
    public void Rebind_LeavesControlsOutsideTheGroupAlone()
    {
        var scheme = StandardSchemes.KeyboardLeft();     // UiUp is also W

        scheme.Rebind(StandardControls.MoveUp, Key(Keys.I), Movement);

        Assert.Equal(new[] { Key(Keys.W) }, scheme.GetBindings(StandardControls.UiUp));
    }

    [Fact]
    public void Schemes_AreCopies_KeptForTheLifeOfTheSet()
    {
        var defaults = StandardSchemes.Keyboard();
        var set = new ControlSet(new[] { defaults });
        var scheme = set["Keyboard"];

        scheme.Rebind(StandardControls.Pause, Key(Keys.P), new[] { StandardControls.Pause });
        set.ResetToDefaults(scheme);

        Assert.NotSame(defaults, scheme);
        Assert.Same(scheme, set["Keyboard"]);     // Input sources holding the scheme see the reset
        Assert.Equal(new[] { Key(Keys.Escape) }, scheme.GetBindings(StandardControls.Pause));
    }

    [Fact]
    public void GetChanges_ListsOnlyRebindings()
    {
        var set = NewSet();
        Assert.Empty(set.GetChanges());

        set["Keyboard"].Rebind(StandardControls.Pause, Key(Keys.P), new[] { StandardControls.Pause });

        var change = Assert.Single(set.GetChanges());
        Assert.Equal("Keyboard", change.Name);
        Assert.Equal(new[] { StandardControls.Pause }, change.Controls.Keys);
    }

    [Fact]
    public void Changes_SurviveSavingAndLoading()
    {
        var set = NewSet();
        set["Keyboard"].Rebind(StandardControls.Pause, Key(Keys.P), new[] { StandardControls.Pause });
        set["Gamepad"].Rebind(StandardControls.MoveUp, new GamePadAxisBinding(GamePadAxis.RightStickY), Movement);

        string json = JsonSerializer.Serialize(set.GetChanges(), SettingsStore.DefaultOptions);
        var loaded = NewSet();
        loaded.ApplyChanges(JsonSerializer.Deserialize<List<ControlScheme>>(json, SettingsStore.DefaultOptions));

        Assert.Equal(new[] { Key(Keys.P) }, loaded["Keyboard"].GetBindings(StandardControls.Pause));
        Assert.Equal(new Binding[] { new GamePadAxisBinding(GamePadAxis.RightStickY) },
            loaded["Gamepad"].GetBindings(StandardControls.MoveUp));
        Assert.Equal(set.GetChanges().Count, loaded.GetChanges().Count);
        Assert.DoesNotContain("displayName", json);     // Worked out from the binding, so not saved
    }

    [Fact]
    public void ApplyChanges_IgnoresUnknownSchemes()
    {
        var set = NewSet();
        set.ApplyChanges(new[] { new ControlScheme("Joystick").Bind("Fire", Key(Keys.F)) });
        Assert.Empty(set.GetChanges());
    }
}
