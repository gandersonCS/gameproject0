namespace CIS580.Simulation;

/// <summary>
/// Identifies a player within a game session.
/// </summary>
/// <remarks>
/// This is the game's own notion of a player, and is deliberately unrelated to any input
/// device: a player may be using a gamepad, sharing the keyboard, or be a remote player
/// connected over a network. It is a small value type so that it can be sent over a
/// network or saved in a replay.
/// </remarks>
/// <param name="Value">The numeric id of the player</param>
public readonly record struct PlayerId(int Value)
{
    public override string ToString() => $"Player {Value}";
}
