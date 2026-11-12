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
}
