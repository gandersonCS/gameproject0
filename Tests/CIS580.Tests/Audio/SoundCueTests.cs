using System;
using CIS580.Audio;
using CIS580.UI;

namespace CIS580.Tests.Audio;

public class SoundCueTests
{
    [Fact]
    public void NeedsAtLeastOneSound()
    {
        var audio = new AudioManager();
        Assert.Throws<ArgumentException>(() => new SoundCue(audio.Effects));
    }

    [Fact]
    public void MaxInstances_StealsTheOldestByDefault()
    {
        var audio = new AudioManager();
        var cue = new SoundCue(audio.Effects, TestSounds.Silent()) { MaxInstances = 2 };

        Assert.True(cue.Play());
        Assert.True(cue.Play());
        Assert.True(cue.Play());

        Assert.Equal(2, cue.PlayingCount);
        audio.StopAll();
    }

    [Fact]
    public void MaxInstances_WithoutStealing_SkipsNewPlays()
    {
        var audio = new AudioManager();
        var cue = new SoundCue(audio.Effects, TestSounds.Silent()) { MaxInstances = 1, StealOldest = false };

        Assert.True(cue.Play());
        Assert.False(cue.Play());
        Assert.Equal(1, cue.PlayingCount);
        audio.StopAll();
    }

    [Fact]
    public void Cooldown_SkipsPlaysThatComeTooSoon()
    {
        var audio = new AudioManager();
        var cue = new SoundCue(audio.Effects, TestSounds.Silent()) { Cooldown = TimeSpan.FromSeconds(0.1), MaxInstances = 0 };

        Assert.True(cue.Play());
        audio.Update(TimeSpan.FromSeconds(0.05));
        Assert.False(cue.Play());
        audio.Update(TimeSpan.FromSeconds(0.05));
        Assert.True(cue.Play());
        audio.StopAll();
    }

    [Fact]
    public void DoesNotPlay_OnAPausedOrSilentBus()
    {
        var audio = new AudioManager();
        var cue = new SoundCue(audio.Effects, TestSounds.Silent());

        audio.Effects.Paused = true;
        Assert.False(cue.Play());

        audio.Effects.Paused = false;
        audio.Master.Muted = true;
        Assert.False(cue.Play());
    }

    [Fact]
    public void StopAll_StopsOnlyThisCue()
    {
        var audio = new AudioManager();
        var a = new SoundCue(audio.Effects, TestSounds.Silent());
        var b = new SoundCue(audio.Effects, TestSounds.Silent());
        a.Play();
        b.Play();

        a.StopAll();

        Assert.Equal(0, a.PlayingCount);
        Assert.Equal(1, b.PlayingCount);
        audio.StopAll();
    }

    [Fact]
    public void Variations_NeverRepeatTwiceInARow()
    {
        var audio = new AudioManager();
        var cue = new SoundCue(audio.Effects, TestSounds.Silent(0.1), TestSounds.Silent(0.1), TestSounds.Silent(0.1))
        {
            MaxInstances = 0,
        };

        var used = new bool[3];
        int previous = -1;
        for (int i = 0; i < 30; i++)
        {
            Assert.True(cue.Play());
            Assert.NotEqual(previous, cue.LastVariation);
            previous = cue.LastVariation;
            used[previous] = true;
        }
        Assert.All(used, Assert.True);
        audio.StopAll();
    }

    [Fact]
    public void OutOfRangePitchAndPan_AreClamped()
    {
        var audio = new AudioManager();
        var cue = new SoundCue(audio.Effects, TestSounds.Silent()) { PitchVariation = 0.5f };

        Assert.True(cue.Play(pitch: 5f, pan: -5f));
        audio.StopAll();
    }

    [Fact]
    public void UiSoundSet_PlaysTheMatchingCue_AndIgnoresMissingOnes()
    {
        var audio = new AudioManager();
        var move = new SoundCue(audio.Ui, TestSounds.Silent());
        var sounds = new UiSoundSet { Move = move };

        sounds.Play(UiSound.Move);
        sounds.Play(UiSound.Confirm);   // No cue: silent

        Assert.Equal(1, audio.Ui.PlayingCount);
        audio.StopAll();
    }
}
