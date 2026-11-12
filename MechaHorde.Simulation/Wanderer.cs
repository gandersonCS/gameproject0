using System;
using System.Numerics;

namespace MechaHorde.Simulation;

/// <summary>
/// Something that jitters randomly around a home position. (A placeholder: replace it with your game's entities.)
/// </summary>
public sealed class Wanderer
{
    /// <summary>
    /// How strongly the wanderer is randomly pushed around, in world units per second
    /// </summary>
    public const float JitterSpeed = 600f;

    /// <summary>
    /// How strongly the wanderer is pulled back toward home; higher is stronger
    /// </summary>
    public const float Stiffness = 3f;

    /// <summary>
    /// The wanderer's position in the world
    /// </summary>
    public Vector2 Position { get; private set; }

    /// <summary>
    /// The position the wanderer is pulled toward
    /// </summary>
    public Vector2 Home { get; }

    public Wanderer(Vector2 position, Vector2 home)
    {
        Position = position;
        Home = home;
    }

    public void Update(World world, float deltaSeconds)
    {
        // Random jitter, using the world's deterministic random number generator
        var jitter = new Vector2(world.Random.NextFloat(-0.5f, 0.5f), world.Random.NextFloat(-0.5f, 0.5f));
        Position += jitter * JitterSpeed * deltaSeconds;

        // Ease back toward home. Using an exponential makes the easing behave the same
        // regardless of the tick rate.
        float t = 1f - MathF.Exp(-Stiffness * deltaSeconds);
        Position = Vector2.Lerp(Position, Home, t);
    }
}
