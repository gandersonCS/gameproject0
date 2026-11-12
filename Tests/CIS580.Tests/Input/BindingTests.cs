using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using CIS580.Input;

namespace CIS580.Tests.Input;

public class BindingTests
{
    private static readonly InputDevice Pad0 = InputDevice.ForGamePad(0);
    private static readonly InputDevice Pad1 = InputDevice.ForGamePad(1);

    [Fact]
    public void KeyBinding_ReadsOnlyTheKeyboard()
    {
        var input = new FakeInput().Keys(Keys.Space).State;
        var binding = new KeyBinding(Keys.Space);

        Assert.Equal(1f, binding.Read(input.Current, InputDevice.KeyboardMouse));
        Assert.Equal(0f, binding.Read(input.Current, Pad0));
    }

    [Fact]
    public void GamePadButtonBinding_ReadsOnlyTheGivenGamePad()
    {
        var input = new FakeInput().Pads(FakeInput.Pad(buttons: Buttons.A), FakeInput.Pad()).State;
        var binding = new GamePadButtonBinding(Buttons.A);

        Assert.Equal(1f, binding.Read(input.Current, Pad0));
        Assert.Equal(0f, binding.Read(input.Current, Pad1));
        Assert.Equal(0f, binding.Read(input.Current, InputDevice.KeyboardMouse));
    }

    [Fact]
    public void GamePadAxisBinding_ReadsOneDirectionOfTheAxis()
    {
        var input = new FakeInput().Pads(FakeInput.Pad(leftStick: new Vector2(-0.6f, 0))).State;

        Assert.Equal(0.6f, new GamePadAxisBinding(GamePadAxis.LeftStickX, false).Read(input.Current, Pad0), 0.001f);
        Assert.Equal(0f, new GamePadAxisBinding(GamePadAxis.LeftStickX, true).Read(input.Current, Pad0));
    }

    [Fact]
    public void GamePadAxisBinding_ReadsTriggers()
    {
        var input = new FakeInput().Pads(FakeInput.Pad(rightTrigger: 0.75f)).State;

        Assert.Equal(0.75f, new GamePadAxisBinding(GamePadAxis.RightTrigger).Read(input.Current, Pad0), 0.01f);
    }

    [Fact]
    public void MouseButtonBinding_ReadsTheMouse()
    {
        var input = new FakeInput().Mouse(right: true).State;

        Assert.Equal(1f, new MouseButtonBinding(MouseButton.Right).Read(input.Current, InputDevice.KeyboardMouse));
        Assert.Equal(0f, new MouseButtonBinding(MouseButton.Left).Read(input.Current, InputDevice.KeyboardMouse));
    }

    [Fact]
    public void BindingsToTheSameInput_AreEqual()
    {
        Assert.Equal<Binding>(new KeyBinding(Keys.A), new KeyBinding(Keys.A));
        Assert.NotEqual<Binding>(new GamePadAxisBinding(GamePadAxis.LeftStickX, true),
            new GamePadAxisBinding(GamePadAxis.LeftStickX, false));
    }
}
