using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using CIS580.Input;

namespace CIS580.Tests.Input;

public class BindingCaptureTests
{
    private static readonly InputDevice Pad0 = InputDevice.ForGamePad(0);

    [Fact]
    public void CapturesANewlyPressedKey()
    {
        var input = new FakeInput().Keys(Keys.A).Keys(Keys.A, Keys.J).State;

        Assert.True(BindingCapture.TryCapture(input, InputDevice.KeyboardMouse, out var binding));
        Assert.Equal(new KeyBinding(Keys.J), binding);
    }

    [Fact]
    public void IgnoresKeysThatWereAlreadyHeld()
    {
        var input = new FakeInput().Keys(Keys.A).Keys(Keys.A).State;

        Assert.False(BindingCapture.TryCapture(input, InputDevice.KeyboardMouse, out _));
    }

    [Fact]
    public void CapturesAMouseButton()
    {
        var input = new FakeInput().Mouse().Mouse(left: true).State;

        Assert.True(BindingCapture.TryCapture(input, InputDevice.KeyboardMouse, out var binding));
        Assert.Equal(new MouseButtonBinding(MouseButton.Left), binding);
    }

    [Fact]
    public void CapturesAStickAsAnAxisDirection()
    {
        var input = new FakeInput().Pads(FakeInput.Pad()).Pads(FakeInput.Pad(leftStick: new Vector2(-1, 0))).State;

        Assert.True(BindingCapture.TryCapture(input, Pad0, out var binding));
        Assert.Equal(new GamePadAxisBinding(GamePadAxis.LeftStickX, false), binding);
    }

    [Fact]
    public void CapturesAGamePadButton()
    {
        var input = new FakeInput().Pads(FakeInput.Pad()).Pads(FakeInput.Pad(buttons: Buttons.Y)).State;

        Assert.True(BindingCapture.TryCapture(input, Pad0, out var binding));
        Assert.Equal(new GamePadButtonBinding(Buttons.Y), binding);
    }

    [Fact]
    public void IgnoresOtherDevices()
    {
        var input = new FakeInput().Keys().Keys(Keys.J).State;

        Assert.False(BindingCapture.TryCapture(input, Pad0, out _));
    }
}
