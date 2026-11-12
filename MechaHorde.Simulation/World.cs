using System.Collections.Generic;
using System.Numerics;
using CIS580.Simulation;

namespace MechaHorde.Simulation;

/// <summary>
/// Everything that exists in the game world.
/// </summary>
/// <remarks>
/// The world is measured in its own units, not in screen pixels: how it appears on screen
/// (its scale, which part is visible, split screen, etc.) is up to the views that draw it.
/// </remarks>
public sealed class World
{
    /// <summary>
    /// The width of the world, in world units
    /// </summary>
    public const float Width = 800f;

    /// <summary>
    /// The height of the world, in world units
    /// </summary>
    public const float Height = 480f;

    // Where the first player appears; each later player appears a little further down
    private static readonly Vector2 SpawnPosition = new(100, 100);
    private static readonly Vector2 SpawnSpacing = new(0, 80);

    // A list (rather than a dictionary) keeps the players in a predictable order
    private readonly List<PlayerAvatar> _players = new();

    /// <summary>
    /// The random number generator for everything in the world.
    /// </summary>
    public DeterministicRandom Random { get; }

    /// <summary>
    /// The players' avatars, in the order they joined
    /// </summary>
    public IReadOnlyList<PlayerAvatar> Players => _players;

    /// <summary>
    /// The placeholder wandering entity
    /// </summary>
    public Wanderer Wanderer { get; }

    public World(ulong seed)
    {
        Random = new DeterministicRandom(seed);
        Wanderer = new Wanderer(new Vector2(100, 100), new Vector2(Width / 2, 200));
    }

    /// <summary>
    /// Finds the avatar belonging to the player, or null if they have none
    /// </summary>
    public PlayerAvatar FindPlayer(PlayerId player)
        => _players.Find(p => p.Owner == player);

    internal PlayerAvatar AddPlayer(PlayerId player)
    {
        var avatar = new PlayerAvatar(player, SpawnPosition + SpawnSpacing * _players.Count);
        _players.Add(avatar);
        return avatar;
    }

    internal void RemovePlayer(PlayerId player)
        => _players.RemoveAll(p => p.Owner == player);
}
