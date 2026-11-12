using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace CIS580.Audio;

/// <summary>
/// A group of sounds sharing a volume control, like a channel on a mixing desk.
/// </summary>
/// <remarks>
/// Buses form a tree: each sound's volume is multiplied by its bus's volume, that bus's parent's
/// volume, and so on up to the master bus. So turning down the "Effects" bus quietens every
/// sound effect, and muting the master bus silences everything. Changes apply to sounds that
/// are already playing, too. Buses are created by the <see cref="AudioManager"/>.
/// </remarks>
public sealed class AudioBus
{
    // A sound playing on this bus, and the volume it was asked to play at
    private sealed record Voice(SoundEffectInstance Instance, float Volume, SoundCue Cue);

    private readonly List<Voice> _voices = new();
    private float _volume = 1f;

    internal AudioBus(AudioManager manager, string name, AudioBus parent)
    {
        Manager = manager;
        Name = name;
        Parent = parent;
    }

    /// <summary>
    /// The audio manager this bus belongs to
    /// </summary>
    public AudioManager Manager { get; }

    public string Name { get; }

    /// <summary>
    /// The bus this one feeds into, or null for the master bus
    /// </summary>
    public AudioBus Parent { get; }

    /// <summary>
    /// This bus's own volume, from 0 (silent) to 1 (full)
    /// </summary>
    public float Volume
    {
        get => _volume;
        set => _volume = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Silences the bus (and the buses feeding into it) without changing its volume
    /// </summary>
    public bool Muted { get; set; }

    /// <summary>
    /// Pauses the sounds on this bus (and the buses feeding into it); they resume where they
    /// left off when unpaused. New sounds don't start while the bus is paused.
    /// </summary>
    public bool Paused { get; set; }

    /// <summary>
    /// The volume sounds on this bus actually play at, taking the parent buses into account
    /// </summary>
    public float EffectiveVolume => Muted ? 0f : Volume * (Parent?.EffectiveVolume ?? 1f);

    /// <summary>
    /// Whether this bus or one of its parents is paused
    /// </summary>
    public bool IsPaused => Paused || (Parent?.IsPaused ?? false);

    /// <summary>
    /// The number of sounds playing (or paused) on this bus
    /// </summary>
    public int PlayingCount => _voices.Count;

    /// <summary>
    /// Stops every sound playing on this bus
    /// </summary>
    public void StopAll()
    {
        foreach (var voice in _voices)
            voice.Instance.Dispose();
        _voices.Clear();
    }

    /// <summary>
    /// Starts playing a sound on this bus. The bus takes care of the instance from now on,
    /// and disposes of it when it finishes.
    /// </summary>
    internal void Start(SoundEffectInstance instance, float volume, SoundCue cue)
    {
        instance.Volume = Math.Clamp(volume * EffectiveVolume, 0f, 1f);
        instance.Play();
        _voices.Add(new Voice(instance, volume, cue));
    }

    /// <summary>
    /// The sounds a cue is playing on this bus, oldest first
    /// </summary>
    internal IEnumerable<SoundEffectInstance> InstancesOf(SoundCue cue)
    {
        foreach (var voice in _voices)
            if (voice.Cue == cue)
                yield return voice.Instance;
    }

    /// <summary>
    /// Stops one of the sounds playing on this bus
    /// </summary>
    internal void Stop(SoundEffectInstance instance)
    {
        int index = _voices.FindIndex(v => v.Instance == instance);
        if (index >= 0)
        {
            _voices[index].Instance.Dispose();
            _voices.RemoveAt(index);
        }
    }

    /// <summary>
    /// Forgets sounds that have finished, and applies volume and pause changes to the rest
    /// </summary>
    internal void Update()
    {
        float volume = EffectiveVolume;
        bool paused = IsPaused;

        for (int i = _voices.Count - 1; i >= 0; i--)
        {
            var voice = _voices[i];
            var instance = voice.Instance;

            if (instance.State == SoundState.Stopped)
            {
                instance.Dispose();
                _voices.RemoveAt(i);
                continue;
            }

            instance.Volume = Math.Clamp(voice.Volume * volume, 0f, 1f);
            if (paused && instance.State == SoundState.Playing)
                instance.Pause();
            else if (!paused && instance.State == SoundState.Paused)
                instance.Resume();
        }
    }
}
