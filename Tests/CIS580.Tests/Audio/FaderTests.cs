using System;
using CIS580.Audio;

namespace CIS580.Tests.Audio;

public class FaderTests
{
    [Fact]
    public void FadesAtASteadyRate_AndStopsAtTheTarget()
    {
        var fader = new Fader(0f);
        fader.FadeTo(1f, TimeSpan.FromSeconds(2));

        fader.Update(TimeSpan.FromSeconds(1));
        Assert.Equal(0.5f, fader.Level, 0.001f);
        Assert.True(fader.IsFading);

        fader.Update(TimeSpan.FromSeconds(5));
        Assert.Equal(1f, fader.Level);
        Assert.False(fader.IsFading);
    }

    [Fact]
    public void FadesDown()
    {
        var fader = new Fader(1f);
        fader.FadeTo(0.25f, TimeSpan.FromSeconds(1));

        fader.Update(TimeSpan.FromSeconds(0.5));
        Assert.Equal(0.5f, fader.Level, 0.001f);

        fader.Update(TimeSpan.FromSeconds(0.5));
        Assert.Equal(0.25f, fader.Level, 0.001f);
    }

    [Fact]
    public void ZeroDuration_JumpsToTheTarget()
    {
        var fader = new Fader(1f);
        fader.FadeTo(0f, TimeSpan.Zero);
        Assert.Equal(0f, fader.Level);
    }
}
