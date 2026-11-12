using System;
using Microsoft.Xna.Framework;

namespace CIS580.Feedback;

/// <summary>
/// Shakes a camera, to make impacts feel powerful.
/// </summary>
/// <remarks>
/// <para>
/// Shaking is driven by "trauma", from 0 (still) to 1 (shaking as hard as allowed). Events
/// add trauma (a light hit 0.2, an explosion 0.6), and it wears off over time. The shake grows
/// with the square of the trauma, so small amounts barely move the view while big ones are
/// violent, and several hits in a row build on each other. (This is the approach from Squirrel
/// Eiserloh's GDC talk "Math for Game Programmers: Juicing Your Cameras With Math".)
/// </para>
/// <para>
/// The motion uses smooth noise rather than random jumps, so the camera wobbles instead of
/// jittering. Every camera has a ScreenShake (Camera2D.Shake), so in split screen each
/// player's view can shake on its own.
/// </para>
/// </remarks>
public sealed class ScreenShake
{
    private float _trauma;
    private float _time;

    /// <summary>
    /// The current trauma, from 0 to 1
    /// </summary>
    public float Trauma
    {
        get => _trauma;
        set => _trauma = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// How far the view moves at full trauma, in pixels
    /// </summary>
    public float MaxOffset { get; set; } = 24f;

    /// <summary>
    /// How far the view turns at full trauma, in radians
    /// </summary>
    public float MaxAngle { get; set; } = 0.05f;

    /// <summary>
    /// How much trauma wears off per second
    /// </summary>
    public float Recovery { get; set; } = 1.5f;

    /// <summary>
    /// How fast the view shakes; higher is more frantic
    /// </summary>
    public float Frequency { get; set; } = 20f;

    /// <summary>
    /// How far the view is moved now, in pixels
    /// </summary>
    public Vector2 Offset { get; private set; }

    /// <summary>
    /// How far the view is turned now, in radians
    /// </summary>
    public float Angle { get; private set; }

    /// <summary>
    /// Adds trauma, making the view shake harder
    /// </summary>
    /// <param name="amount">How much to add (trauma stays at most 1)</param>
    public void Add(float amount) => Trauma += amount;

    /// <summary>
    /// Stops shaking at once
    /// </summary>
    public void Stop()
    {
        Trauma = 0;
        Offset = Vector2.Zero;
        Angle = 0;
    }

    /// <summary>
    /// Wears off the trauma and moves the view
    /// </summary>
    public void Update(TimeSpan elapsed)
    {
        float seconds = (float)elapsed.TotalSeconds;
        _time += seconds;
        Trauma -= Recovery * seconds;

        float shake = _trauma * _trauma;
        float t = _time * Frequency;
        Offset = new Vector2(Noise(t, 0f), Noise(t, 10f)) * (MaxOffset * shake);
        Angle = Noise(t, 20f) * (MaxAngle * shake);
    }

    /// <summary>
    /// Smooth noise from -1 to 1: a few sine waves at unrelated frequencies, added together
    /// </summary>
    private static float Noise(float t, float seed)
        => (MathF.Sin(t * 1.0f + seed) + MathF.Sin(t * 2.3f + seed * 1.7f) + MathF.Sin(t * 3.7f + seed * 2.9f)) / 3f;
}
