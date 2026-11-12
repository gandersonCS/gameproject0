using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace CIS580.Audio;

/// <summary>
/// Plays background music: one looping track at a time, cross-fading from one track to the next.
/// </summary>
/// <remarks>
/// <para>
/// Tracks are ordinary <see cref="SoundEffect"/>s (see the content builder), played as looping
/// instances, so two can play at once while one fades out and the next fades in. MonoGame's
/// Song and MediaPlayer can only play one track at a time, so they can't cross-fade.
/// </para>
/// <para>
/// The music can also be "ducked": turned down while something else needs attention, such as
/// the pause menu, then brought back up.
/// </para>
/// </remarks>
public sealed class MusicPlayer
{
    /// <summary>
    /// How long cross-fades take, unless told otherwise
    /// </summary>
    public static readonly TimeSpan DefaultFade = TimeSpan.FromSeconds(1.5);

    // A track that is playing, and how far it has faded in
    private sealed class Track
    {
        public SoundEffect Sound;
        public SoundEffectInstance Instance;
        public Fader Fade;
    }

    private readonly List<Track> _fadingOut = new();
    private Track _current;
    private Fader _duck = new(1f);

    internal MusicPlayer(AudioBus bus)
    {
        Bus = bus;
    }

    /// <summary>
    /// The bus the music plays on
    /// </summary>
    public AudioBus Bus { get; }

    /// <summary>
    /// The track playing (or fading in), or null if there is none
    /// </summary>
    public SoundEffect Current => _current?.Sound;

    /// <summary>
    /// Whether the music is ducked
    /// </summary>
    public bool IsDucked => _duck.Target < 1f;

    /// <summary>
    /// The level the music is playing at before the bus volume is applied: the current track's
    /// fade multiplied by the duck level (0 to 1)
    /// </summary>
    public float Level => (_current?.Fade.Level ?? 0f) * _duck.Level;

    /// <summary>
    /// Plays a track, cross-fading from whatever is playing. Asking for the track that is already
    /// playing does nothing, so states can simply ask for their music whenever they start.
    /// </summary>
    /// <param name="track">The track to play (it loops)</param>
    /// <param name="fade">How long the cross-fade takes (null for <see cref="DefaultFade"/>)</param>
    public void Play(SoundEffect track, TimeSpan? fade = null)
    {
        ArgumentNullException.ThrowIfNull(track);
        if (_current?.Sound == track)
        {
            _current.Fade.FadeTo(1f, fade ?? DefaultFade);   // In case it was fading out
            return;
        }

        FadeOutCurrent(fade ?? DefaultFade);

        var instance = track.CreateInstance();
        instance.IsLooped = true;
        instance.Volume = 0f;
        _current = new Track { Sound = track, Instance = instance, Fade = new Fader(0f) };
        _current.Fade.FadeTo(1f, fade ?? DefaultFade);
        instance.Play();
    }

    /// <summary>
    /// Fades out the music
    /// </summary>
    /// <param name="fade">How long the fade takes (null for <see cref="DefaultFade"/>; zero stops at once)</param>
    public void Stop(TimeSpan? fade = null) => FadeOutCurrent(fade ?? DefaultFade);

    /// <summary>
    /// Turns the music down, e.g. while the game is paused
    /// </summary>
    /// <param name="level">How loud the ducked music is (0 to 1)</param>
    /// <param name="fade">How long turning down takes</param>
    public void Duck(float level = 0.3f, TimeSpan? fade = null)
        => _duck.FadeTo(level, fade ?? TimeSpan.FromSeconds(0.3));

    /// <summary>
    /// Brings ducked music back up
    /// </summary>
    /// <param name="fade">How long turning up takes</param>
    public void Unduck(TimeSpan? fade = null)
        => _duck.FadeTo(1f, fade ?? TimeSpan.FromSeconds(0.5));

    /// <summary>
    /// Advances the fades, and stops tracks that have faded out
    /// </summary>
    internal void Update(TimeSpan elapsed)
    {
        _duck.Update(elapsed);
        float volume = Bus.EffectiveVolume * _duck.Level;
        bool paused = Bus.IsPaused;

        if (_current != null)
            UpdateTrack(_current, elapsed, volume, paused);

        for (int i = _fadingOut.Count - 1; i >= 0; i--)
        {
            var track = _fadingOut[i];
            UpdateTrack(track, elapsed, volume, paused);
            if (track.Fade.Level <= 0f)
            {
                track.Instance.Dispose();
                _fadingOut.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Stops all music at once
    /// </summary>
    internal void StopAll()
    {
        FadeOutCurrent(TimeSpan.Zero);
        foreach (var track in _fadingOut)
            track.Instance.Dispose();
        _fadingOut.Clear();
    }

    private static void UpdateTrack(Track track, TimeSpan elapsed, float volume, bool paused)
    {
        if (!paused)
            track.Fade.Update(elapsed);
        track.Instance.Volume = Math.Clamp(track.Fade.Level * volume, 0f, 1f);

        if (paused && track.Instance.State == SoundState.Playing)
            track.Instance.Pause();
        else if (!paused && track.Instance.State == SoundState.Paused)
            track.Instance.Resume();
    }

    private void FadeOutCurrent(TimeSpan fade)
    {
        if (_current == null)
            return;

        _current.Fade.FadeTo(0f, fade);
        if (_current.Fade.Level <= 0f)
            _current.Instance.Dispose();
        else
            _fadingOut.Add(_current);
        _current = null;
    }
}
