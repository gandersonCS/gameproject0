using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using CIS580.Input;

namespace CIS580.Tests.Input;

/// <summary>
/// Builds device states for tests, and feeds them to an InputState frame by frame
/// </summary>
internal sealed class FakeInput
{
    public InputState State { get; } = new();

    /// <summary>
    /// Advances one frame with the given keys held (and nothing else)
    /// </summary>
    public FakeInput Keys(params Keys[] keys)
    {
        State.Update(new KeyboardState(keys), default);
        return this;
    }

    /// <summary>
    /// Advances one frame with the given gamepad states (index 0, 1, ...)
    /// </summary>
    public FakeInput Pads(params GamePadState[] pads)
    {
        State.Update(default, default, pads);
        return this;
    }

    /// <summary>
    /// Advances one frame with the given mouse buttons held
    /// </summary>
    public FakeInput Mouse(bool left = false, bool right = false)
    {
        var mouse = new MouseState(0, 0, 0,
            left ? ButtonState.Pressed : ButtonState.Released, ButtonState.Released,
            right ? ButtonState.Pressed : ButtonState.Released, ButtonState.Released, ButtonState.Released);
        State.Update(default, mouse);
        return this;
    }

    /// <summary>
    /// A connected gamepad's state
    /// </summary>
    public static GamePadState Pad(Vector2 leftStick = default, Vector2 rightStick = default,
        float leftTrigger = 0, float rightTrigger = 0, params Buttons[] buttons)
    {
        var allButtons = buttons.Aggregate((Buttons)0, (all, b) => all | b);
        return new GamePadState(new GamePadThumbSticks(leftStick, rightStick),
            new GamePadTriggers(leftTrigger, rightTrigger), new GamePadButtons(allButtons), new GamePadDPad());
    }
}
