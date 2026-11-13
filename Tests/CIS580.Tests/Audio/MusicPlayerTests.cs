using System;
using CIS580.Audio;

namespace CIS580.Tests.Audio;

public class MusicPlayerTests
{
    private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);

    [Fact]
    public void Play_FadesTheTrackIn()
    {
        var audio = new AudioManager();
        var track = TestSounds.Silent();

        audio.MusicPlayer.Play(track, fade: Second);
        Assert.Same(track, audio.MusicPlayer.Current);
        Assert.Equal(0f, audio.MusicPlayer.Level);

        audio.Update(TimeSpan.FromSeconds(0.5));
        Assert.Equal(0.5f, audio.MusicPlayer.Level, 0.001f);

        audio.Update(Second);
        Assert.Equal(1f, audio.MusicPlayer.Level, 0.001f);
        audio.StopAll();
    }

    [Fact]
    public void PlayingTheSameTrack_DoesNotRestartIt()
    {
        var audio = new AudioManager();
        var track = TestSounds.Silent();
        audio.MusicPlayer.Play(track, fade: TimeSpan.Zero);

        audio.MusicPlayer.Play(track);

        Assert.Equal(1f, audio.MusicPlayer.Level);
        audio.StopAll();
    }

    [Fact]
    public void PlayingAnotherTrack_CrossFades()
    {
        var audio = new AudioManager();
        var first = TestSounds.Silent();
        var second = TestSounds.Silent();
        audio.MusicPlayer.Play(first, fade: TimeSpan.Zero);

        audio.MusicPlayer.Play(second, fade: Second);
        audio.Update(TimeSpan.FromSeconds(0.25));

        Assert.Same(second, audio.MusicPlayer.Current);
        Assert.Equal(0.25f, audio.MusicPlayer.Level, 0.001f);
        audio.StopAll();
    }

    [Fact]
    public void Duck_TurnsTheMusicDown_AndUnduckBringsItBack()
    {
        var audio = new AudioManager();
        audio.MusicPlayer.Play(TestSounds.Silent(), fade: TimeSpan.Zero);

        audio.MusicPlayer.Duck(0.25f, fade: TimeSpan.Zero);
        audio.Update(TimeSpan.FromMilliseconds(16));
        Assert.True(audio.MusicPlayer.IsDucked);
        Assert.Equal(0.25f, audio.MusicPlayer.Level, 0.001f);

        audio.MusicPlayer.Unduck(fade: TimeSpan.Zero);
        audio.Update(TimeSpan.FromMilliseconds(16));
        Assert.False(audio.MusicPlayer.IsDucked);
        Assert.Equal(1f, audio.MusicPlayer.Level, 0.001f);
        audio.StopAll();
    }

    [Fact]
    public void Stop_FadesOut()
    {
        var audio = new AudioManager();
        audio.MusicPlayer.Play(TestSounds.Silent(), fade: TimeSpan.Zero);

        audio.MusicPlayer.Stop(fade: Second);

        Assert.Null(audio.MusicPlayer.Current);
        audio.Update(Second);
        audio.StopAll();
    }

    [Fact]
    public void TracksWithTheSameParts_AreTheSameMusic()
    {
        var intro = TestSounds.Silent();
        var loop = TestSounds.Silent();
        var audio = new AudioManager();
        audio.MusicPlayer.Play(new MusicTrack(loop, intro), fade: TimeSpan.Zero);

        audio.MusicPlayer.Play(new MusicTrack(loop, intro), fade: Second);

        Assert.Equal(1f, audio.MusicPlayer.Level);   // Not restarted
        Assert.Same(loop, audio.MusicPlayer.Current);
        audio.StopAll();
    }

    [Fact]
    public void Interrupt_HoldsTheTrack_AndEndInterruptionBringsItBack()
    {
        var audio = new AudioManager();
        var game = new MusicTrack(TestSounds.Silent(), intro: TestSounds.Silent());
        var pause = new MusicTrack(TestSounds.Silent());
        audio.MusicPlayer.Play(game, fade: TimeSpan.Zero);

        audio.MusicPlayer.Interrupt(pause, fade: Second);
        audio.Update(Second);
        Assert.Equal(pause, audio.MusicPlayer.CurrentTrack);
        Assert.Equal(game, audio.MusicPlayer.InterruptedTrack);
        Assert.Equal(1f, audio.MusicPlayer.Level, 0.001f);

        audio.MusicPlayer.EndInterruption(fade: Second);
        audio.Update(TimeSpan.FromSeconds(0.5));
        Assert.Equal(game, audio.MusicPlayer.CurrentTrack);
        Assert.Null(audio.MusicPlayer.InterruptedTrack);
        Assert.Equal(0.5f, audio.MusicPlayer.Level, 0.001f);
        audio.StopAll();
    }

    [Fact]
    public void PlayingSomethingElse_ReplacesAnInterruption()
    {
        var audio = new AudioManager();
        var game = new MusicTrack(TestSounds.Silent());
        var menu = new MusicTrack(TestSounds.Silent());
        audio.MusicPlayer.Play(game, fade: TimeSpan.Zero);
        audio.MusicPlayer.Interrupt(new MusicTrack(TestSounds.Silent()), fade: TimeSpan.Zero);

        audio.MusicPlayer.EndInterruption(fade: TimeSpan.Zero);   // As when quitting from pause...
        audio.MusicPlayer.Play(menu, fade: TimeSpan.Zero);        // ...to the menu

        Assert.Equal(menu, audio.MusicPlayer.CurrentTrack);
        Assert.Null(audio.MusicPlayer.InterruptedTrack);
        audio.StopAll();
    }
}
