using System.Collections.Generic;

namespace CIS580.Simulation;

/// <summary>
/// Everything a simulation is given for one tick
/// </summary>
public readonly struct TickContext
{
    /// <summary>
    /// The number of this tick. The first tick is 0.
    /// </summary>
    public long Tick { get; }

    /// <summary>
    /// The length of a tick, in seconds. This is the same for every tick.
    /// </summary>
    public float DeltaSeconds { get; }

    /// <summary>
    /// One input for each player in the session, in the order they joined
    /// </summary>
    public IReadOnlyList<PlayerInput> Inputs { get; }

    public TickContext(long tick, float deltaSeconds, IReadOnlyList<PlayerInput> inputs)
    {
        Tick = tick;
        DeltaSeconds = deltaSeconds;
        Inputs = inputs;
    }
}
