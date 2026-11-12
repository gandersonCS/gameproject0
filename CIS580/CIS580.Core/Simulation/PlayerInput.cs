using System;
using System.Numerics;

namespace CIS580.Simulation;

/// <summary>
/// A player's command for the current tick, along with their command from the previous tick,
/// so the simulation can tell when an action was just pressed or just released.
/// </summary>
public readonly struct PlayerInput
{
    /// <summary>
    /// The player this input belongs to
    /// </summary>
    public PlayerId Player { get; }

    /// <summary>
    /// The player's command for this tick
    /// </summary>
    public PlayerCommand Command { get; }

    /// <summary>
    /// The player's command for the previous tick
    /// </summary>
    public PlayerCommand Previous { get; }

    public PlayerInput(PlayerId player, PlayerCommand command, PlayerCommand previous)
    {
        Player = player;
        Command = command;
        Previous = previous;
    }

    /// <summary>
    /// The desired movement direction (see <see cref="PlayerCommand.Move"/>)
    /// </summary>
    public Vector2 Move => Command.Move;

    /// <summary>
    /// The desired look direction (see <see cref="PlayerCommand.Look"/>)
    /// </summary>
    public Vector2 Look => Command.Look;

    /// <summary>
    /// Determines if the action is held down this tick
    /// </summary>
    public bool IsDown<TAction>(TAction action) where TAction : struct, Enum
        => Command.Actions.Contains(action);

    /// <summary>
    /// Determines if the action was pressed this tick (down now, but not on the previous tick)
    /// </summary>
    public bool WasPressed<TAction>(TAction action) where TAction : struct, Enum
        => Command.Actions.Contains(action) && !Previous.Actions.Contains(action);

    /// <summary>
    /// Determines if the action was released this tick (down on the previous tick, but not now)
    /// </summary>
    public bool WasReleased<TAction>(TAction action) where TAction : struct, Enum
        => !Command.Actions.Contains(action) && Previous.Actions.Contains(action);
}
