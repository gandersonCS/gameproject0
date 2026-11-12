using Microsoft.Xna.Framework.Input;
using CIS580.Input;

namespace CIS580.Tests.Input;

public class InputSourceTests
{
    private static readonly InputSource LeftPlayer = new(InputDevice.KeyboardMouse, StandardSchemes.KeyboardLeft());
    private static readonly InputSource RightPlayer = new(InputDevice.KeyboardMouse, StandardSchemes.KeyboardRight());

    [Fact]
    public void PlayersSharingTheKeyboard_AreIndependent()
    {
        var input = new FakeInput().Keys(Keys.A).State;     // Left player moves left

        Assert.True(LeftPlayer.IsDown(StandardControls.MoveLeft, input));
        Assert.False(RightPlayer.IsDown(StandardControls.MoveLeft, input));
    }

    [Fact]
    public void WasPressed_IsTrueOnlyOnTheFirstFrame()
    {
        var fake = new FakeInput();

        fake.Keys(Keys.Space);
        Assert.True(LeftPlayer.WasPressed(StandardControls.UiConfirm, fake.State));

        fake.Keys(Keys.Space);
        Assert.False(LeftPlayer.WasPressed(StandardControls.UiConfirm, fake.State));
        Assert.True(LeftPlayer.IsDown(StandardControls.UiConfirm, fake.State));

        fake.Keys();
        Assert.True(LeftPlayer.WasReleased(StandardControls.UiConfirm, fake.State));
    }

    [Fact]
    public void ASourceWithSeveralDevices_ReadsTheStrongest()
    {
        var source = new InputSource("Both",
            new InputSource.Assignment(InputDevice.KeyboardMouse, StandardSchemes.Keyboard()),
            new InputSource.Assignment(InputDevice.ForGamePad(0), StandardSchemes.GamePad()));
        var input = new FakeInput().Pads(FakeInput.Pad(buttons: Buttons.Start)).State;

        Assert.True(source.IsDown(StandardControls.Pause, input));
    }

    [Fact]
    public void HasLostDevice_WhenItsGamePadIsUnplugged()
    {
        var source = new InputSource(InputDevice.ForGamePad(0), StandardSchemes.GamePad());
        var fake = new FakeInput();

        fake.Keys();                            // No gamepad ever connected: not "lost"
        Assert.False(source.HasLostDevice(fake.State));

        fake.Pads(FakeInput.Pad());              // Connected
        Assert.False(source.HasLostDevice(fake.State));

        fake.Keys();                            // Unplugged
        Assert.True(source.HasLostDevice(fake.State));
    }
}
