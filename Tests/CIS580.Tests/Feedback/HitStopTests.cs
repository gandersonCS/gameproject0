using System;
using CIS580.Feedback;

namespace CIS580.Tests.Feedback;

public class HitStopTests
{
    private static TimeSpan Ms(double ms) => TimeSpan.FromMilliseconds(ms);

    [Fact]
    public void WhenInactive_TimePassesUnchanged()
    {
        Assert.Equal(Ms(16), new HitStop().Apply(Ms(16)));
    }

    [Fact]
    public void SwallowsTime_UntilTheFreezeIsUsedUp()
    {
        var hitStop = new HitStop();
        hitStop.Start(Ms(40));

        Assert.Equal(TimeSpan.Zero, hitStop.Apply(Ms(16)));
        Assert.Equal(TimeSpan.Zero, hitStop.Apply(Ms(16)));
        Assert.Equal(Ms(8), hitStop.Apply(Ms(16)));    // Only 8ms of freeze was left
        Assert.False(hitStop.IsActive);
    }

    [Fact]
    public void OverlappingStops_TheLongerWins()
    {
        var hitStop = new HitStop();
        hitStop.Start(Ms(100));
        hitStop.Start(Ms(50));
        Assert.Equal(Ms(100), hitStop.Remaining);
    }
}
