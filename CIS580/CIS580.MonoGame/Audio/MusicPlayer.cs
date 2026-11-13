using System;
using System.Collections.Generic;
using System.Diagnostics;
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
/// A <see cref="MusicTrack"/> can have an intro that plays once before its loop, so a song can
/// begin properly and then repeat just its main part.
/// </para>
/// <para>
/// The music can also be "ducked": turned down while something else needs attention, then
/// brought back up. Or it can be <see cref="Interrupt">interrupted</see> by other music (such as
/// the pause menu's) and later carry on from where it stopped.
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
        public MusicTrack Music;
        public SoundEffectInstance Intro;   // null once the loop has taken over, or if there's no intro
        public SoundEffectInstance Loop;
        public Fader Fade;
        public bool Suspended;              // Interrupted: fades out, then holds its place
        public readonly Stopwatch IntroClock = new();   // How much of the intro has played
    }

    private readonly List<Track> _fadingOut = new();
    private Track _current;
    private Track _interrupted;             // Waiting to carry on after an interruption
    private bool _currentIsInterruption;    // Whether _current was started by Interrupt
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
    /// The loop of the track playing (or fading in), or null if there is none
    /// </summary>
    public SoundEffect Current => _current?.Music.Loop;

    /// <summary>
    /// The track playing (or fading in), or null if there is none
    /// </summary>
    public MusicTrack CurrentTrack => _current?.Music;

    /// <summary>
    /// The track waiting to carry on after an <see cref="Interrupt"/>, or null if there is none
    /// </summary>
    public MusicTrack InterruptedTrack => _interrupted?.Music;

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
    /// Plays a looping track (with no intro), cross-fading from whatever is playing
    /// </summary>
    /// <param name="track">The track to play (it loops)</param>
    /// <param name="fade">How long the cross-fade takes (null for <see cref="DefaultFade"/>)</param>
    public void Play(SoundEffect track, TimeSpan? fade = null)
    {
        ArgumentNullException.ThrowIfNull(track);
        Play(new MusicTrack(track), fade);
    }

    /// <summary>
    /// Plays a track (its intro, if it has one, then its loop), cross-fading from whatever is
    /// playing. Asking for the track that is already playing does nothing, so states can simply
    /// ask for their music whenever they start. Asking for the interrupted track ends the
    /// interruption; any other track replaces both.
    /// </summary>
    /// <param name="track">The track to play</param>
    /// <param name="fade">How long the cross-fade takes (null for <see cref="DefaultFade"/>)</param>
    public void Play(MusicTrack track, TimeSpan? fade = null)
    {
        ArgumentNullException.ThrowIfNull(track);
        if (_current?.Music == track)
        {
            _current.Fade.FadeTo(1f, fade ?? DefaultFade);   // In case it was fading out
            _currentIsInterruption = false;
            return;
        }
        if (_interrupted?.Music == track)
        {
            EndInterruption(fade);
            return;
        }

        DiscardInterrupted();
        FadeOutCurrent(fade ?? DefaultFade);
        _current = Start(track, fade ?? DefaultFade);
    }

    /// <summary>
    /// Fades out the music
    /// </summary>
    /// <param name="fade">How long the fade takes (null for <see cref="DefaultFade"/>; zero stops at once)</param>
    public void Stop(TimeSpan? fade = null)
    {
        DiscardInterrupted();
        FadeOutCurrent(fade ?? DefaultFade);
    }

    /// <summary>
    /// Plays other music for a while, e.g. on the pause menu. The current track fades out and
    /// holds its place, then carries on from there when <see cref="EndInterruption"/> is called.
    /// Interrupting again replaces the interrupting music; the original track still waits.
    /// </summary>
    /// <param name="track">The music to play meanwhile (it starts from its beginning)</param>
    /// <param name="fade">How long the cross-fade takes (null for <see cref="DefaultFade"/>)</param>
    public void Interrupt(MusicTrack track, TimeSpan? fade = null)
    {
        ArgumentNullException.ThrowIfNull(track);
        if (_currentIsInterruption && _current?.Music == track)
            return;

        if (_interrupted == null && _current != null && !_currentIsInterruption)
        {
            _interrupted = _current;
            _interrupted.Suspended = true;
            _interrupted.Fade.FadeTo(0f, fade ?? DefaultFade);
            _current = null;
        }
        else
        {
            FadeOutCurrent(fade ?? DefaultFade);
        }
        _current = Start(track, fade ?? DefaultFade);
        _currentIsInterruption = true;
    }

    /// <summary>
    /// Fades out the music started by <see cref="Interrupt"/>, and fades the interrupted track
    /// (if there was one) back in from where it stopped. Does nothing if there's no interruption.
    /// </summary>
    /// <param name="fade">How long the cross-fade takes (null for <see cref="DefaultFade"/>)</param>
    public void EndInterruption(TimeSpan? fade = null)
    {
        if (!_currentIsInterruption && _interrupted == null)
            return;

        if (_currentIsInterruption)
            FadeOutCurrent(fade ?? DefaultFade);
        else
            DiscardInterrupted();   // Something else was played meanwhile, so that carries on

        if (_interrupted != null)
        {
            _current = _interrupted;
            _interrupted = null;
            _current.Suspended = false;
            _current.Fade.FadeTo(1f, fade ?? DefaultFade);
        }
    }

    /// <summary>
    /// Turns the music down, e.g. while a dialog is open
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
    /// Advances the fades, moves tracks from their intros to their loops, and stops tracks that
    /// have faded out
    /// </summary>
    internal void Update(TimeSpan elapsed)
    {
        _duck.Update(elapsed);
        float volume = Bus.EffectiveVolume * _duck.Level;
        bool paused = Bus.IsPaused;

        if (_current != null)
            UpdateTrack(_current, elapsed, volume, paused);
        if (_interrupted != null)
            UpdateTrack(_interrupted, elapsed, volume, paused);

        for (int i = _fadingOut.Count - 1; i >= 0; i--)
        {
            var track = _fadingOut[i];
            UpdateTrack(track, elapsed, volume, paused);
            if (track.Fade.Level <= 0f)
            {
                DisposeTrack(track);
                _fadingOut.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Stops all music at once
    /// </summary>
    internal void StopAll()
    {
        DiscardInterrupted();
        FadeOutCurrent(TimeSpan.Zero);
        foreach (var track in _fadingOut)
            DisposeTrack(track);
        _fadingOut.Clear();
    }

    private static Track Start(MusicTrack music, TimeSpan fade)
    {
        var track = new Track { Music = music, Fade = new Fader(0f) };
        track.Fade.FadeTo(1f, fade);

        track.Loop = music.Loop.CreateInstance();
        track.Loop.IsLooped = true;
        track.Loop.Volume = 0f;

        if (music.Intro != null)
        {
            track.Intro = music.Intro.CreateInstance();
            track.Intro.Volume = 0f;
            track.Intro.Play();
            track.IntroClock.Start();
        }
        else
        {
            track.Loop.Play();
        }
        return track;
    }

    private static void UpdateTrack(Track track, TimeSpan elapsed, float volume, bool paused)
    {
        if (!paused)
            track.Fade.Update(elapsed);
        float level = Math.Clamp(track.Fade.Level * volume, 0f, 1f);
        track.Loop.Volume = level;
        if (track.Intro != null)
            track.Intro.Volume = level;

        // An interrupted track keeps playing while it fades out, then holds its place
        if (paused || (track.Suspended && track.Fade.Level <= 0f))
        {
            Pause(track.Intro);
            Pause(track.Loop);
            track.IntroClock.Stop();
            return;
        }

        Resume(track.Intro);
        Resume(track.Loop);

        if (track.Intro != null)
        {
            track.IntroClock.Start();

            // Start the loop on the frame closest to the end of the intro, rather than waiting to
            // see the intro stop (which can only be seen up to a frame late)
            var remaining = track.Music.Intro.Duration - track.IntroClock.Elapsed;
            if (remaining <= elapsed / 2 || track.Intro.State == SoundState.Stopped)
            {
                track.Loop.Play();
                track.Intro.Dispose();
                track.Intro = null;
                track.IntroClock.Stop();
            }
        }
    }

    private static void Pause(SoundEffectInstance instance)
    {
        if (instance?.State == SoundState.Playing)
            instance.Pause();
    }

    private static void Resume(SoundEffectInstance instance)
    {
        if (instance?.State == SoundState.Paused)
            instance.Resume();
    }

    private static void DisposeTrack(Track track)
    {
        track.Intro?.Dispose();
        track.Loop.Dispose();
    }

    private void DiscardInterrupted()
    {
        if (_interrupted == null)
            return;
        DisposeTrack(_interrupted);
        _interrupted = null;
    }

    private void FadeOutCurrent(TimeSpan fade)
    {
        _currentIsInterruption = false;
        if (_current == null)
            return;

        _current.Fade.FadeTo(0f, fade);
        if (_current.Fade.Level <= 0f)
            DisposeTrack(_current);
        else
            _fadingOut.Add(_current);
        _current = null;
    }
}
