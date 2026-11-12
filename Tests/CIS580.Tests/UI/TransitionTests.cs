using System;
using CIS580.UI;

namespace CIS580.Tests.UI;

public class TransitionTests
{
    private static Transition Create() => new()
    {
        OnDuration = TimeSpan.FromSeconds(1),
        OffDuration = TimeSpan.FromSeconds(0.5),
    };

    [Fact]
    public void StartsHidden()
    {
        var transition = Create();

        Assert.Equal(TransitionState.Hidden, transition.State);
        Assert.Equal(0f, transition.Position);
        Assert.Equal(1f, transition.Offset);
    }

    [Fact]
    public void EntersOverItsOnDuration()
    {
        var transition = Create();

        transition.Update(TimeSpan.FromSeconds(0.5), visible: true);
        Assert.Equal(TransitionState.Entering, transition.State);
        Assert.Equal(0.5f, transition.Position, 0.001f);

        transition.Update(TimeSpan.FromSeconds(0.5), visible: true);
        Assert.Equal(TransitionState.Visible, transition.State);
        Assert.Equal(0f, transition.Offset);
    }

    [Fact]
    public void ExitsOverItsOffDuration()
    {
        var transition = Create();
        transition.Update(TimeSpan.FromSeconds(1), visible: true);

        transition.Update(TimeSpan.FromSeconds(0.25), visible: false);
        Assert.Equal(TransitionState.Exiting, transition.State);
        Assert.Equal(0.5f, transition.Position, 0.001f);

        transition.Update(TimeSpan.FromSeconds(0.25), visible: false);
        Assert.Equal(TransitionState.Hidden, transition.State);
    }

    [Fact]
    public void AZeroDuration_IsInstant()
    {
        var transition = new Transition { OnDuration = TimeSpan.Zero };

        transition.Update(TimeSpan.Zero, visible: true);

        Assert.Equal(TransitionState.Visible, transition.State);
    }
}
