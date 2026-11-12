using System;

namespace CIS580.Simulation;

/// <summary>
/// A command source that calls a function to decide each tick's command.
/// Useful for simple AI players, and for scripting players in tests.
/// </summary>
/// <example>
/// <code>
/// // A player that always moves right
/// var bot = new DelegateCommandSource(tick => new PlayerCommand { Move = Vector2.UnitX });
/// </code>
/// </example>
public sealed class DelegateCommandSource : ICommandSource
{
    private readonly Func<long, PlayerCommand> _getCommand;

    public DelegateCommandSource(Func<long, PlayerCommand> getCommand)
    {
        _getCommand = getCommand ?? throw new ArgumentNullException(nameof(getCommand));
    }

    /// <inheritdoc/>
    public PlayerCommand GetCommand(long tick) => _getCommand(tick);
}
