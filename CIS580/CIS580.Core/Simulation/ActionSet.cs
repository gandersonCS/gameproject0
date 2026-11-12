using System;

namespace CIS580.Simulation;

/// <summary>
/// A set of game actions (such as "jump" or "fire") that are held down, stored as bits.
/// </summary>
/// <remarks>
/// Each game defines its own actions as an enum, whose values are used as bit indices,
/// so they must be in the range 0 to 31:
/// <code>
/// public enum GameAction { Jump, Fire, Interact }
///
/// var actions = ActionSet.Empty.With(GameAction.Jump);
/// if (actions.Contains(GameAction.Jump)) { ... }
/// </code>
/// Storing the actions as a single number keeps commands small and easy to send over a network.
/// </remarks>
public readonly record struct ActionSet(uint Bits)
{
    /// <summary>
    /// The maximum number of distinct actions an ActionSet can hold
    /// </summary>
    public const int Capacity = 32;

    /// <summary>
    /// An ActionSet with no actions in it
    /// </summary>
    public static readonly ActionSet Empty = new(0);

    /// <summary>
    /// True if no actions are in the set
    /// </summary>
    public bool IsEmpty => Bits == 0;

    /// <summary>
    /// Determines if the action is in the set
    /// </summary>
    public bool Contains<TAction>(TAction action) where TAction : struct, Enum
        => (Bits & Mask(action)) != 0;

    /// <summary>
    /// Returns a copy of this set with the action added
    /// </summary>
    public ActionSet With<TAction>(TAction action) where TAction : struct, Enum
        => new(Bits | Mask(action));

    /// <summary>
    /// Returns a copy of this set with the action removed
    /// </summary>
    public ActionSet Without<TAction>(TAction action) where TAction : struct, Enum
        => new(Bits & ~Mask(action));

    /// <summary>
    /// Returns a set containing the actions in either set
    /// </summary>
    public ActionSet Union(ActionSet other) => new(Bits | other.Bits);

    private static uint Mask<TAction>(TAction action) where TAction : struct, Enum
    {
        int index = Convert.ToInt32(action);
        if (index < 0 || index >= Capacity)
            throw new ArgumentOutOfRangeException(nameof(action),
                $"Action {action} has value {index}; action enum values must be between 0 and {Capacity - 1}.");
        return 1u << index;
    }
}
