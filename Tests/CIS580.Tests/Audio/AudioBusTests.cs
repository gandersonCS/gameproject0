using System;
using CIS580.Audio;

namespace CIS580.Tests.Audio;

public class AudioBusTests
{
    [Fact]
    public void Volume_IsMultipliedDownTheTree()
    {
        var audio = new AudioManager();
        audio.Master.Volume = 0.5f;
        audio.Effects.Volume = 0.5f;

        Assert.Equal(0.25f, audio.Effects.EffectiveVolume, 0.001f);
        Assert.Equal(0.5f, audio.Music.EffectiveVolume, 0.001f);
    }

    [Fact]
    public void MutingTheMaster_SilencesEveryBus_WithoutChangingVolumes()
    {
        var audio = new AudioManager();
        audio.Master.Muted = true;

        Assert.All(audio.Buses, bus => Assert.Equal(0f, bus.EffectiveVolume));
        Assert.Equal(1f, audio.Effects.Volume);
    }

    [Fact]
    public void PausingABus_PausesTheBusesFeedingIntoIt()
    {
        var audio = new AudioManager();
        var footsteps = audio.CreateBus("Footsteps", audio.Effects);

        audio.Effects.Paused = true;

        Assert.True(footsteps.IsPaused);
        Assert.False(audio.Ui.IsPaused);
    }

    [Fact]
    public void Volume_IsClamped()
    {
        var audio = new AudioManager();
        audio.Music.Volume = 3f;
        Assert.Equal(1f, audio.Music.Volume);
    }

    [Fact]
    public void Update_AppliesVolumeChangesToPlayingSounds()
    {
        var audio = new AudioManager();
        var cue = new SoundCue(audio.Effects, TestSounds.Silent());
        Assert.True(cue.Play());

        audio.Effects.Volume = 0.5f;
        audio.Update(TimeSpan.FromMilliseconds(16));

        Assert.Equal(1, audio.Effects.PlayingCount);
        audio.StopAll();
        Assert.Equal(0, audio.Effects.PlayingCount);
    }
}
