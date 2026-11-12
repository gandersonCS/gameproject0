using System;
using Microsoft.Xna.Framework.Input;
using CIS580.Input;

namespace CIS580.Tests.Input;

public class UiInputTests
{
    private static readonly TimeSpan Frame = TimeSpan.FromMilliseconds(50);

    private readonly FakeInput _fake = new();
    private readonly UiInput _ui;
    private readonly InputSource _left = new(InputDevice.KeyboardMouse, StandardSchemes.KeyboardLeft());
    private readonly InputSource _right = new(InputDevice.KeyboardMouse, StandardSchemes.KeyboardRight());

    public UiInputTests()
    {
        _ui = new UiInput(_fake.State)
        {
            RepeatDelay = TimeSpan.FromMilliseconds(400),
            RepeatInterval = TimeSpan.FromMilliseconds(100),
        };
        _ui.Sources.Add(_left);
        _ui.Sources.Add(_right);
    }

    private void Frame_(params Keys[] keys)
    {
        _fake.Keys(keys);
        _ui.Update(Frame);
    }

    [Fact]
    public void APress_FiresOnce_AndReportsWhoPressedIt()
    {
        Frame_(Keys.Enter);     // Right player's confirm

        Assert.True(_ui.WasPressed(UiControl.Confirm, out var source));
        Assert.Same(_right, source);

        Frame_(Keys.Enter);
        Assert.False(_ui.WasPressed(UiControl.Confirm));
    }

    [Fact]
    public void HoldingADirection_RepeatsAfterTheDelay()
    {
        int fired = 0;
        // Hold down for 1 second: fires at 0, then at 400ms, 500ms, ... 1000ms
        for (int i = 0; i <= 20; i++)
        {
            Frame_(Keys.S);
            if (_ui.WasPressed(UiControl.Down)) fired++;
        }

        Assert.Equal(1 + 7, fired);
    }

    [Fact]
    public void HoldingConfirm_DoesNotRepeat()
    {
        int fired = 0;
        for (int i = 0; i <= 20; i++)
        {
            Frame_(Keys.Space);
            if (_ui.WasPressed(UiControl.Confirm)) fired++;
        }

        Assert.Equal(1, fired);
    }

    [Fact]
    public void ASourceNotListenedTo_IsIgnored()
    {
        _ui.Sources.Remove(_right);

        Frame_(Keys.Enter);

        Assert.False(_ui.WasPressed(UiControl.Confirm));
    }

    [Fact]
    public void AControlHeldBeforeListening_DoesNotFireOrRepeat()
    {
        _ui.Sources.Clear();
        Frame_(Keys.S);
        _ui.Sources.Add(_left);

        for (int i = 0; i < 20; i++)
        {
            Frame_(Keys.S);
            Assert.False(_ui.WasPressed(UiControl.Down));
        }
    }
}
