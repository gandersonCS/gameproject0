using Microsoft.Xna.Framework;
using CIS580.Simulation;

namespace MechaHorde.Views;

/// <summary>
/// The color that identifies each player, used everywhere a player is shown (the world, the
/// HUD, the lobby) so people can tell who is who.
/// </summary>
public static class PlayerColors
{
    private static readonly Color[] Colors =
    {
        Color.Green,
        Color.DarkOrange,
        Color.MediumPurple,
        Color.Yellow,
    };

    /// <summary>
    /// The color for the player in the given seat (0 for the first player)
    /// </summary>
    public static Color ForSeat(int seat) => Colors[seat % Colors.Length];

    /// <summary>
    /// The color for a player
    /// </summary>
    public static Color For(PlayerId player) => ForSeat(player.Value - 1);
}
