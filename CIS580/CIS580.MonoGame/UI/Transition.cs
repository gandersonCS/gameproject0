using System;

namespace CIS580.UI;

/// <summary>
/// Where a layer is in its transition
/// </summary>
public enum TransitionState
{
    /// <summary>Fully off screen</summary>
    Hidden,
    /// <summary>Coming on screen</summary>
    Entering,
    /// <summary>Fully on screen</summary>
    Visible,
    /// <summary>Going off screen</summary>
    Exiting,
}

/// <summary>
/// Animates a layer on and off screen.
/// </summary>
/// <remarks>
/// A transition is a single number, <see cref="Position"/>, that moves from 0 (hidden) to 1
/// (visible) as the layer comes on screen, and back to 0 as it goes off. Layers use it however
/// they like when drawing: to fade (<see cref="Alpha"/>), to slide (<see cref="Offset"/>), etc.
/// </remarks>
public sealed class Transition
{
    /// <summary>
    /// How long it takes to come fully on screen
    /// </summary>
    public TimeSpan OnDuration { get; set; } = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// How long it takes to go fully off screen
    /// </summary>
    public TimeSpan OffDuration { get; set; } = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// How far on screen the layer is, from 0 (hidden) to 1 (visible)
    /// </summary>
    public float Position { get; private set; }

    /// <summary>
    /// Where the layer is in its transition
    /// </summary>
    public TransitionState State { get; private set; } = TransitionState.Hidden;

    /// <summary>
    /// An opacity for fading the layer in and out (the same as Position)
    /// </summary>
    public float Alpha => Position;

    /// <summary>
    /// How far the layer is from its resting place, from 1 (hidden) to 0 (visible), eased so
    /// that movement slows as the layer settles. Multiply by a distance to slide a layer in.
    /// </summary>
    public float Offset => (1f - Position) * (1f - Position);

    /// <summary>
    /// Advances the transition toward visible or hidden
    /// </summary>
    /// <param name="elapsed">The time since the last update</param>
    /// <param name="visible">Whether the layer should be visible</param>
    public void Update(TimeSpan elapsed, bool visible)
    {
        var duration = visible ? OnDuration : OffDuration;
        float step = duration <= TimeSpan.Zero ? 1f : (float)(elapsed / duration);

        Position = Math.Clamp(Position + (visible ? step : -step), 0f, 1f);

        State = visible
            ? (Position < 1f ? TransitionState.Entering : TransitionState.Visible)
            : (Position > 0f ? TransitionState.Exiting : TransitionState.Hidden);
    }
}
