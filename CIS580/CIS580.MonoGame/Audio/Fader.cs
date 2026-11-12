using System;

namespace CIS580.Audio;

/// <summary>
/// A level (0 to 1) that moves toward a target at a steady rate, for fading sounds in and out
/// </summary>
public struct Fader
{
    /// <summary>
    /// The current level
    /// </summary>
    public float Level { get; private set; }

    /// <summary>
    /// The level being faded toward
    /// </summary>
    public float Target { get; private set; }

    // How much the level changes per second
    private float _rate;

    /// <param name="level">The starting level</param>
    public Fader(float level)
    {
        Level = Target = Math.Clamp(level, 0f, 1f);
        _rate = 0;
    }

    /// <summary>
    /// Whether the level is still changing
    /// </summary>
    public readonly bool IsFading => Level != Target;

    /// <summary>
    /// Starts fading toward a level
    /// </summary>
    /// <param name="target">The level to fade to (0 to 1)</param>
    /// <param name="duration">How long a fade across the whole range (0 to 1) takes; zero jumps there at once</param>
    public void FadeTo(float target, TimeSpan duration)
    {
        Target = Math.Clamp(target, 0f, 1f);
        if (duration <= TimeSpan.Zero)
            Level = Target;
        else
            _rate = 1f / (float)duration.TotalSeconds;
    }

    /// <summary>
    /// Moves the level toward the target
    /// </summary>
    public void Update(TimeSpan elapsed)
    {
        float step = _rate * (float)elapsed.TotalSeconds;
        Level = Level < Target ? Math.Min(Level + step, Target) : Math.Max(Level - step, Target);
    }
}
