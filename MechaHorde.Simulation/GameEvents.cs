using CIS580.Simulation;

namespace MechaHorde.Simulation;

/// <summary>
/// Base type for the events the simulation raises for the rest of the game to react to
/// (e.g. by playing a sound). Add your own events by deriving from this record.
/// </summary>
/// <param name="Tick">The tick on which the event happened</param>
public abstract record GameEvent(long Tick);

/// <summary>
/// A player joined the game
/// </summary>
public sealed record PlayerJoined(long Tick, PlayerId Player) : GameEvent(Tick);

/// <summary>
/// A player left the game
/// </summary>
public sealed record PlayerLeft(long Tick, PlayerId Player) : GameEvent(Tick);
