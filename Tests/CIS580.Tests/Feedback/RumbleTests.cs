using System;
using System.Collections.Generic;
using CIS580.Feedback;
using CIS580.Input;
using CIS580.Simulation;

namespace CIS580.Tests.Feedback;

public class RumbleTests
{
    private readonly List<(int Pad, float Low, float High)> _calls = new();
    private Rumble NewRumble() => new((pad, low, high) => _calls.Add((pad, low, high)));

    private static TimeSpan Ms(double ms) => TimeSpan.FromMilliseconds(ms);

    [Fact]
    public void Rumbles_ThenStopsByItself()
    {
        var rumble = NewRumble();
        rumble.Play(1, 0.5f, 0.25f, Ms(100));

        rumble.Update(Ms(16));
        Assert.Equal((1, 0.5f, 0.25f), _calls[^1]);

        rumble.Update(Ms(100));
        Assert.Equal((1, 0f, 0f), _calls[^1]);
    }

    [Fact]
    public void OverlappingRumbles_TheStrongestWins()
    {
        var rumble = NewRumble();
        rumble.Play(0, 0.2f, 0.9f, Ms(100));
        rumble.Play(0, 0.7f, 0.1f, Ms(100));

        rumble.Update(Ms(16));

        Assert.Equal((0, 0.7f, 0.9f), _calls[^1]);
    }

    [Fact]
    public void OnlyTellsTheGamePadWhenSomethingChanges()
    {
        var rumble = NewRumble();
        rumble.Play(0, 0.5f, 0.5f, Ms(1000));

        rumble.Update(Ms(16));
        rumble.Update(Ms(16));
        rumble.Update(Ms(16));

        Assert.Single(_calls);
    }

    [Fact]
    public void Disabled_DoesNothing()
    {
        var rumble = NewRumble();
        rumble.Enabled = false;
        rumble.Play(0, 1f, 1f, Ms(100));
        rumble.Update(Ms(16));
        Assert.Empty(_calls);
    }

    [Fact]
    public void StopAll_StopsTheMotors()
    {
        var rumble = NewRumble();
        rumble.Play(2, 1f, 1f, Ms(1000));
        rumble.Update(Ms(16));

        rumble.StopAll();

        Assert.Equal((2, 0f, 0f), _calls[^1]);
    }

    [Fact]
    public void APlayer_RumblesTheirGamePad_ButNotTheKeyboard()
    {
        var rumble = NewRumble();
        var padPlayer = new LocalPlayer(new PlayerId(1), new InputSource(InputDevice.ForGamePad(3), StandardSchemes.GamePad()), new InputProfile());
        var keyPlayer = new LocalPlayer(new PlayerId(2), new InputSource(InputDevice.KeyboardMouse, StandardSchemes.Keyboard()), new InputProfile());

        rumble.Play(padPlayer, 1f, 1f, Ms(100));
        rumble.Play(keyPlayer, 1f, 1f, Ms(100));
        rumble.Update(Ms(16));

        Assert.Equal((3, 1f, 1f), Assert.Single(_calls));
    }
}
