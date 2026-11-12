namespace CIS580.Input;

/// <summary>
/// One player's input preferences, applied when their input is turned into commands.
/// </summary>
/// <remarks>
/// These are applied in the input layer (by the <see cref="CommandBuilder"/>), never in the
/// simulation, so a player's preferences don't affect anyone else's game.
/// </remarks>
public sealed class InputProfile
{
    /// <summary>
    /// Inverts the vertical look/aim axis, so pushing up looks down (as in flight controls)
    /// </summary>
    public bool InvertLookY { get; set; }

    /// <summary>
    /// Inverts the vertical movement axis
    /// </summary>
    public bool InvertMoveY { get; set; }

    /// <summary>
    /// Stick movements smaller than this (0 to 1) are ignored, so a worn stick that doesn't
    /// quite center doesn't make the player drift
    /// </summary>
    public float DeadZone { get; set; } = 0.2f;
}
