using System.Linq;
using Microsoft.Xna.Framework.Input;
using CIS580.Input;

namespace CIS580.Tests.Input;

public class InputJsonTests
{
    [Fact]
    public void ControlScheme_SurvivesARoundTrip()
    {
        var scheme = StandardSchemes.GamePad()
            .Bind("Fire", new MouseButtonBinding(MouseButton.Left), new KeyBinding(Keys.F));

        var copy = InputJson.DeserializeScheme(InputJson.Serialize(scheme));

        Assert.Equal(scheme.Name, copy.Name);
        Assert.Equal(scheme.Controls.Keys.OrderBy(k => k), copy.Controls.Keys.OrderBy(k => k));
        foreach (var control in scheme.Controls.Keys)
            Assert.Equal(scheme.GetBindings(control), copy.GetBindings(control));
    }

    [Fact]
    public void ControlScheme_IsReadableJson()
    {
        var json = InputJson.Serialize(new ControlScheme("Test").Bind("Jump", new KeyBinding(Keys.Space)));

        Assert.Contains("\"type\": \"key\"", json);
        Assert.Contains("\"key\": \"Space\"", json);
    }

    [Fact]
    public void InputProfile_SurvivesARoundTrip()
    {
        var profile = new InputProfile { InvertLookY = true, DeadZone = 0.3f };

        var copy = InputJson.DeserializeProfile(InputJson.Serialize(profile));

        Assert.True(copy.InvertLookY);
        Assert.False(copy.InvertMoveY);
        Assert.Equal(0.3f, copy.DeadZone);
    }

    [Fact]
    public void Clone_IsIndependentOfTheOriginal()
    {
        var original = StandardSchemes.Keyboard();
        var clone = original.Clone("Custom");

        clone.SetBindings(StandardControls.Pause, new[] { new KeyBinding(Keys.P) });

        Assert.Equal(new KeyBinding(Keys.Escape), Assert.Single(original.GetBindings(StandardControls.Pause)));
        Assert.Equal("Custom", clone.Name);
    }

    [Fact]
    public void FindControlsUsing_FindsConflicts()
    {
        var scheme = StandardSchemes.Keyboard();

        var controls = scheme.FindControlsUsing(new KeyBinding(Keys.W)).ToList();

        Assert.Contains(StandardControls.MoveUp, controls);
        Assert.Contains(StandardControls.UiUp, controls);
    }
}
