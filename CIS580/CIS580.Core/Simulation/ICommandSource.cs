namespace CIS580.Simulation;

/// <summary>
/// Supplies the commands for one player.
/// </summary>
/// <remarks>
/// The SessionRunner asks each player's command source for a command once per tick.
/// Different kinds of players are just different command sources:
/// <list type="bullet">
/// <item>a local player's source is fed by the input layer (see <see cref="LocalCommandSource"/>)</item>
/// <item>an AI player's source decides on commands itself (see <see cref="DelegateCommandSource"/>)</item>
/// <item>a remote player's source would return commands received over the network</item>
/// <item>a replay's source would return previously recorded commands</item>
/// </list>
/// </remarks>
public interface ICommandSource
{
    /// <summary>
    /// Gets the player's command for the given tick
    /// </summary>
    /// <param name="tick">The number of the tick about to be simulated</param>
    PlayerCommand GetCommand(long tick);
}
