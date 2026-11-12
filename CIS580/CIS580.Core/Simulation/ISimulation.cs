namespace CIS580.Simulation;

/// <summary>
/// A game simulation that is advanced in fixed-length ticks by a <see cref="SessionRunner"/>.
/// </summary>
/// <remarks>
/// To keep a simulation deterministic (so the same starting state and the same commands
/// always produce the same result, which networking and replays depend upon):
/// <list type="bullet">
/// <item>change state only inside these methods</item>
/// <item>use <see cref="TickContext.DeltaSeconds"/>, never a clock, for time</item>
/// <item>use a seeded <see cref="DeterministicRandom"/>, never System.Random or Guid.NewGuid()</item>
/// <item>read player input only from <see cref="TickContext.Inputs"/></item>
/// <item>iterate over lists, not dictionaries or hash sets, when order affects the outcome</item>
/// </list>
/// </remarks>
public interface ISimulation
{
    /// <summary>
    /// Adds a player to the simulation. Called by the SessionRunner just before a tick.
    /// </summary>
    void AddPlayer(PlayerId player);

    /// <summary>
    /// Removes a player from the simulation. Called by the SessionRunner just before a tick.
    /// </summary>
    void RemovePlayer(PlayerId player);

    /// <summary>
    /// Advances the simulation by one tick
    /// </summary>
    void Tick(in TickContext context);
}
