using Microsoft.Xna.Framework.Input;
using CIS580.Input;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace CIS580.Tests.Input;

public class CommandBuilderTests
{
    private enum TestAction { Jump, Fire }

    private static readonly ControlScheme KeyboardScheme = StandardSchemes.Keyboard()
        .Bind(nameof(TestAction.Jump), new KeyBinding(Keys.Space));

    private static readonly InputSource Keyboard = new(InputDevice.KeyboardMouse, KeyboardScheme);
    private static readonly InputSource GamePad = new(InputDevice.ForGamePad(0), StandardSchemes.GamePad());

    [Fact]
    public void Keys_MoveInScreenDirections()
    {
        var input = new FakeInput().Keys(Keys.D, Keys.S).State;      // Right and down

        var move = CommandBuilder.Build<TestAction>(Keyboard, new InputProfile(), input).Move;

        Assert.True(move.X > 0);
        Assert.True(move.Y > 0);
    }

    [Fact]
    public void DiagonalMovement_IsNoFasterThanStraight()
    {
        var input = new FakeInput().Keys(Keys.D, Keys.W).State;

        var move = CommandBuilder.Build<TestAction>(Keyboard, new InputProfile(), input).Move;

        Assert.Equal(1f, move.Length(), 0.001f);
    }

    [Fact]
    public void StickUp_MovesUpTheScreen()
    {
        var input = new FakeInput().Pads(FakeInput.Pad(leftStick: new XnaVector2(0, 1))).State;

        var move = CommandBuilder.Build<TestAction>(GamePad, new InputProfile(), input).Move;

        Assert.Equal(-1f, move.Y, 0.001f);
    }

    [Fact]
    public void SmallStickMovements_AreIgnored()
    {
        var input = new FakeInput().Pads(FakeInput.Pad(leftStick: new XnaVector2(0.1f, 0.1f))).State;

        var move = CommandBuilder.Build<TestAction>(GamePad, new InputProfile { DeadZone = 0.2f }, input).Move;

        Assert.Equal(System.Numerics.Vector2.Zero, move);
    }

    [Fact]
    public void StickMovement_StartsFromZeroAtTheDeadZone()
    {
        var input = new FakeInput().Pads(FakeInput.Pad(leftStick: new XnaVector2(0.6f, 0))).State;

        var move = CommandBuilder.Build<TestAction>(GamePad, new InputProfile { DeadZone = 0.2f }, input).Move;

        Assert.Equal(0.5f, move.X, 0.001f);   // Halfway between the dead zone (0.2) and full (1.0)
    }

    [Fact]
    public void InvertLookY_FlipsOnlyLooking()
    {
        var stick = new XnaVector2(0, 1);
        var input = new FakeInput().Pads(FakeInput.Pad(leftStick: stick, rightStick: stick)).State;

        var command = CommandBuilder.Build<TestAction>(GamePad, new InputProfile { InvertLookY = true }, input);

        Assert.Equal(1f, command.Look.Y, 0.001f);      // Inverted: up looks down
        Assert.Equal(-1f, command.Move.Y, 0.001f);     // Not inverted
    }

    [Fact]
    public void InvertMoveY_FlipsMovement()
    {
        var input = new FakeInput().Keys(Keys.W).State;

        var command = CommandBuilder.Build<TestAction>(Keyboard, new InputProfile { InvertMoveY = true }, input);

        Assert.Equal(1f, command.Move.Y, 0.001f);
    }

    [Fact]
    public void Actions_AreReadFromControlsNamedAfterThem()
    {
        var input = new FakeInput().Keys(Keys.Space).State;

        var actions = CommandBuilder.Build<TestAction>(Keyboard, new InputProfile(), input).Actions;

        Assert.True(actions.Contains(TestAction.Jump));
        Assert.False(actions.Contains(TestAction.Fire));     // Unbound
    }
}
