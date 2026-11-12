namespace CIS580.Simulation;

/// <summary>
/// A command source for a player on this machine, which the game submits commands to as it
/// reads input.
/// </summary>
/// <remarks>
/// The game reads input once per frame, but the simulation runs on its own fixed tick, so a
/// frame may have zero, one, or several ticks. To make sure a quick tap is never missed, any
/// action that is pressed in ANY frame submitted since the last tick is included in the next
/// tick's command, even if it was released again before that tick ran.
/// </remarks>
public sealed class LocalCommandSource : ICommandSource
{
    private PlayerCommand _latest;
    private ActionSet _pressedSinceLastTick;

    /// <summary>
    /// Submits the player's current command. Call this every frame.
    /// </summary>
    public void Submit(in PlayerCommand command)
    {
        _latest = command;
        _pressedSinceLastTick = _pressedSinceLastTick.Union(command.Actions);
    }

    /// <inheritdoc/>
    public PlayerCommand GetCommand(long tick)
    {
        var command = _latest;
        command.Actions = command.Actions.Union(_pressedSinceLastTick);
        _pressedSinceLastTick = ActionSet.Empty;
        return command;
    }
}
