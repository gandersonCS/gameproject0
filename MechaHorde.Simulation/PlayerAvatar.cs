using System.Numerics;
using CIS580.Simulation;

namespace MechaHorde.Simulation;

/// <summary>
/// A player's presence in the world. (A placeholder: replace it with your game's player.)
/// </summary>
public sealed class PlayerAvatar
{
    /// <summary>
    /// How fast the avatar moves, in world units per second
    /// </summary>
    public const float Speed = 480f;

    /// <summary>
    /// The player controlling this avatar
    /// </summary>
    public PlayerId Owner { get; }

    /// <summary>
    /// The avatar's position in the world
    /// </summary>
    public Vector2 Position { get; private set; }

    public PlayerAvatar(PlayerId owner, Vector2 position)
    {
        Owner = owner;
        Position = position;
    }

    /// <summary>
    /// Moves the avatar according to its player's input
    /// </summary>
    public void ApplyInput(in PlayerInput input, float deltaSeconds)
    {
        // Never trust a command: one from a modified client on a network could ask to move
        // at any speed, so limit it to the allowed range here, in the simulation.
        var move = input.Move;
        if (move.LengthSquared() > 1f)
            move = Vector2.Normalize(move);

        Position += move * Speed * deltaSeconds;
    }
}
