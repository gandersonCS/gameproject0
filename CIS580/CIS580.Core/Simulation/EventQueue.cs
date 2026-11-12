using System.Collections.Generic;

namespace CIS580.Simulation;

/// <summary>
/// Collects the events a simulation raises (such as "player scored" or "enemy destroyed"),
/// so that the rest of the game can react to them after the simulation has run.
/// </summary>
/// <remarks>
/// The simulation must not play sounds, spawn particles, or shake the screen itself: those
/// things don't exist on a headless server, and shouldn't happen again when a networked game
/// re-simulates past ticks. Instead, the simulation raises events, and the client reads them
/// once per frame (after the SessionRunner update) and decides how to present them.
/// </remarks>
/// <typeparam name="TEvent">The base type of the game's events</typeparam>
public sealed class EventQueue<TEvent>
{
    private readonly List<TEvent> _events = new();

    /// <summary>
    /// The events raised since the queue was last cleared, oldest first
    /// </summary>
    public IReadOnlyList<TEvent> Pending => _events;

    /// <summary>
    /// Raises an event
    /// </summary>
    public void Raise(TEvent gameEvent) => _events.Add(gameEvent);

    /// <summary>
    /// Discards all pending events. Call this once every consumer has read them.
    /// </summary>
    public void Clear() => _events.Clear();
}
