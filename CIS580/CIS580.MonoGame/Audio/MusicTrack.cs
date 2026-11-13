using System;
using Microsoft.Xna.Framework.Audio;

namespace CIS580.Audio;

/// <summary>
/// A piece of music for the <see cref="MusicPlayer"/>: an optional intro that plays once, then a
/// loop that repeats for as long as the track plays.
/// </summary>
/// <remarks>
/// <para>
/// Splitting a song into an intro and a loop lets it start properly (a build-up, a drum
/// opening) without going back to that start every time it repeats. Cut the two files so the
/// loop begins exactly where the intro ends, and so the loop's end flows back into its own start.
/// </para>
/// <para>
/// The player starts the loop as the intro finishes, on the nearest frame, so the changeover
/// can be a few milliseconds off. Cut it just before a strong beat, where that goes unnoticed.
/// </para>
/// <para>
/// Tracks are compared by value, so an equal track (the same intro and loop) counts as the same
/// music.
/// </para>
/// </remarks>
public sealed record MusicTrack
{
    /// <param name="loop">The part that repeats</param>
    /// <param name="intro">The part played once before the loop, or null to start with the loop</param>
    public MusicTrack(SoundEffect loop, SoundEffect intro = null)
    {
        Loop = loop ?? throw new ArgumentNullException(nameof(loop));
        Intro = intro;
    }

    /// <summary>
    /// The part that repeats
    /// </summary>
    public SoundEffect Loop { get; }

    /// <summary>
    /// The part played once before the loop, or null if the track starts with the loop
    /// </summary>
    public SoundEffect Intro { get; }
}
