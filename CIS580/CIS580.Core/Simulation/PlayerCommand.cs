using System.Numerics;

namespace CIS580.Simulation;

/// <summary>
/// What one player wants to do during one simulation tick.
/// </summary>
/// <remarks>
/// Commands are the ONLY way player input reaches the simulation. The simulation never reads
/// keyboards or gamepads; instead, the input layer turns device input into commands. This is
/// what lets the same simulation be driven by local players, AI, recorded replays, or players
/// on the other end of a network connection.
/// </remarks>
public struct PlayerCommand
{
    /// <summary>
    /// The desired movement direction, with a length between 0 and 1.
    /// X is right, and Y is down (matching screen coordinates).
    /// </summary>
    public Vector2 Move;

    /// <summary>
    /// The desired look or aim direction, with a length between 0 and 1.
    /// X is right, and Y is down (matching screen coordinates).
    /// </summary>
    public Vector2 Look;

    /// <summary>
    /// The actions that are held down
    /// </summary>
    public ActionSet Actions;

    /// <summary>
    /// A command that does nothing
    /// </summary>
    public static readonly PlayerCommand Empty = default;
}
