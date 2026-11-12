using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Audio;

namespace CIS580.Audio;

/// <summary>
/// A sound effect the game plays by name of purpose ("jump", "hit") rather than by file, with
/// rules for how it plays.
/// </summary>
/// <remarks>
/// <para>
/// Hearing exactly the same sound over and over quickly becomes tiresome, so a cue can pick
/// randomly from several <see cref="Variations"/> and vary each play's pitch and volume a little.
/// It can also limit how many copies play at once (<see cref="MaxInstances"/>) and how soon it
/// can play again (<see cref="Cooldown"/>), so that fifty coins collected on the same frame
/// don't make one deafening sound.
/// </para>
/// <para>
/// Sound is part of the presentation, not the simulation, so the randomness here deliberately
/// does not use the simulation's random numbers: playing a sound must never change the game.
/// </para>
/// </remarks>
public sealed class SoundCue
{
    private readonly SoundEffect[] _variations;
    private int _lastVariation = -1;

    /// <summary>
    /// The index of the variation played last (-1 before the first play)
    /// </summary>
    internal int LastVariation => _lastVariation;
    private TimeSpan? _lastPlayed;

    /// <param name="bus">The bus to play on (e.g. the audio manager's Effects or Ui bus)</param>
    /// <param name="variations">The sounds to choose from; at least one</param>
    public SoundCue(AudioBus bus, params SoundEffect[] variations)
    {
        Bus = bus ?? throw new ArgumentNullException(nameof(bus));
        if (variations == null || variations.Length == 0 || variations.Any(v => v == null))
            throw new ArgumentException("A cue needs at least one sound.", nameof(variations));
        _variations = variations;
    }

    /// <summary>
    /// The bus the cue plays on
    /// </summary>
    public AudioBus Bus { get; }

    /// <summary>
    /// The sounds the cue chooses from
    /// </summary>
    public IReadOnlyList<SoundEffect> Variations => _variations;

    /// <summary>
    /// The cue's volume, from 0 to 1
    /// </summary>
    public float Volume { get; set; } = 1f;

    /// <summary>
    /// How much each play's volume may vary, as a fraction (0.1 means up to 10% quieter)
    /// </summary>
    public float VolumeVariation { get; set; }

    /// <summary>
    /// How much each play's pitch may vary up or down, in octaves (0.1 is a bit over a semitone)
    /// </summary>
    public float PitchVariation { get; set; }

    /// <summary>
    /// The most copies of this cue that may play at once (zero for no limit)
    /// </summary>
    public int MaxInstances { get; set; } = 4;

    /// <summary>
    /// When the limit is reached, whether to stop the oldest copy to make room (true) or not
    /// to play the new one (false)
    /// </summary>
    public bool StealOldest { get; set; } = true;

    /// <summary>
    /// The shortest time between plays; plays sooner than this are skipped
    /// </summary>
    public TimeSpan Cooldown { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// The number of copies of this cue playing now
    /// </summary>
    public int PlayingCount => Bus.InstancesOf(this).Count();

    /// <summary>
    /// Plays the cue
    /// </summary>
    /// <param name="volume">The volume for this play, from 0 to 1 (multiplied by the cue's own volume)</param>
    /// <param name="pan">Where the sound is, from -1 (left) to 1 (right)</param>
    /// <param name="pitch">A pitch shift for this play, in octaves (-1 to 1)</param>
    /// <returns>Whether the sound played (it may be skipped by the cue's limits, or a paused bus)</returns>
    public bool Play(float volume = 1f, float pan = 0f, float pitch = 0f)
    {
        var audio = Bus.Manager;
        if (Bus.IsPaused || Bus.EffectiveVolume <= 0f)
            return false;
        if (_lastPlayed is { } last && audio.Now - last < Cooldown)
            return false;

        if (MaxInstances > 0)
        {
            var playing = Bus.InstancesOf(this).ToList();
            if (playing.Count >= MaxInstances)
            {
                if (!StealOldest)
                    return false;
                Bus.Stop(playing[0]);
            }
        }

        var random = audio.Random;
        var instance = _variations[ChooseVariation(random)].CreateInstance();
        instance.Pan = Math.Clamp(pan, -1f, 1f);
        instance.Pitch = Math.Clamp(pitch + Spread(random, PitchVariation), -1f, 1f);
        float playVolume = Volume * volume * (1f - VolumeVariation * random.NextSingle());

        Bus.Start(instance, playVolume, this);
        _lastPlayed = audio.Now;
        return true;
    }

    /// <summary>
    /// Stops every copy of this cue that is playing
    /// </summary>
    public void StopAll()
    {
        foreach (var instance in Bus.InstancesOf(this).ToList())
            Bus.Stop(instance);
    }

    /// <summary>
    /// Picks a variation at random, avoiding the one played last time (when there's a choice)
    /// </summary>
    private int ChooseVariation(Random random)
    {
        if (_variations.Length == 1)
            return 0;

        int index = random.Next(_variations.Length - 1);
        if (index >= _lastVariation && _lastVariation >= 0)
            index++;
        _lastVariation = index;
        return index;
    }

    /// <summary>
    /// A random amount between -amount and +amount
    /// </summary>
    private static float Spread(Random random, float amount) => amount * (random.NextSingle() * 2f - 1f);
}
