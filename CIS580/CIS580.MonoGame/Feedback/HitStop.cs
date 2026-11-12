using System;

namespace CIS580.Feedback;

/// <summary>
/// Freezes the game for a moment, e.g. for a few frames when a big hit lands, to give it weight.
/// </summary>
/// <remarks>
/// <para>
/// Pass the frame's elapsed time through <see cref="Apply"/> before running the simulation:
/// while a hit-stop is active, the time is swallowed and the simulation doesn't advance.
/// Everything else (menus, music, screen shake) keeps going.
/// </para>
/// <para>
/// This freezes only this machine's copy of the game. That's fine for local play, but in a
/// networked game every machine must agree on what happens when, so there the freeze belongs
/// inside the simulation (e.g. entities that skip a few ticks after a hit) instead.
/// </para>
/// </remarks>
public sealed class HitStop
{
    /// <summary>
    /// How much longer the game stays frozen
    /// </summary>
    public TimeSpan Remaining { get; private set; }

    /// <summary>
    /// Whether the game is frozen now
    /// </summary>
    public bool IsActive => Remaining > TimeSpan.Zero;

    /// <summary>
    /// Freezes the game. Overlapping hit-stops don't add up: the longer one wins.
    /// </summary>
    /// <param name="duration">How long to freeze for (typically 50 to 150 milliseconds)</param>
    public void Start(TimeSpan duration)
    {
        if (duration > Remaining)
            Remaining = duration;
    }

    /// <summary>
    /// Ends the freeze at once
    /// </summary>
    public void Cancel() => Remaining = TimeSpan.Zero;

    /// <summary>
    /// Uses up the frozen time
    /// </summary>
    /// <param name="elapsed">The time since the last frame</param>
    /// <returns>How much of that time the game should advance by</returns>
    public TimeSpan Apply(TimeSpan elapsed)
    {
        if (!IsActive)
            return elapsed;

        var frozen = elapsed < Remaining ? elapsed : Remaining;
        Remaining -= frozen;
        return elapsed - frozen;
    }
}
