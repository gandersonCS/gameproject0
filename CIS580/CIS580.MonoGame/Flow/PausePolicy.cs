using System.Collections.Generic;
using System.Linq;
using CIS580.Input;

namespace CIS580.Flow;

/// <summary>
/// Who may pause the game
/// </summary>
public enum PauseRights
{
    /// <summary>Any player may pause</summary>
    AnyPlayer,
    /// <summary>Only the first player (e.g. the host) may pause</summary>
    FirstPlayer,
    /// <summary>No one may pause (e.g. a competitive online match)</summary>
    NoOne,
}

/// <summary>
/// Who controls the pause menu once the game is paused
/// </summary>
public enum PauseMenuControl
{
    /// <summary>Only the player who paused</summary>
    PausingPlayer,
    /// <summary>Any player</summary>
    AnyPlayer,
}

/// <summary>
/// The rules for pausing. Every game decides these differently, so they are settings here
/// rather than built into the pause code; for rules these settings can't express, derive from
/// this class and override its methods.
/// </summary>
public class PausePolicy
{
    /// <summary>
    /// Who may pause the game
    /// </summary>
    public PauseRights WhoCanPause { get; set; } = PauseRights.AnyPlayer;

    /// <summary>
    /// Who controls the pause menu
    /// </summary>
    public PauseMenuControl MenuControl { get; set; } = PauseMenuControl.PausingPlayer;

    /// <summary>
    /// Whether pausing stops the simulation. A networked game usually can't stop the shared
    /// simulation for one player, so its pause menu appears while the game carries on.
    /// </summary>
    public bool PausesSimulation { get; set; } = true;

    /// <summary>
    /// Whether to pause when a player's gamepad is disconnected
    /// </summary>
    public bool PauseWhenDeviceLost { get; set; } = true;

    /// <summary>
    /// Determines if a player may pause the game
    /// </summary>
    public virtual bool CanPause(LocalPlayer player, IReadOnlyList<LocalPlayer> players) => WhoCanPause switch
    {
        PauseRights.AnyPlayer => true,
        PauseRights.FirstPlayer => players.Count > 0 && players[0] == player,
        _ => false,
    };

    /// <summary>
    /// Gets the input sources the pause menu listens to
    /// </summary>
    /// <param name="pausedBy">The player who paused</param>
    /// <param name="players">All the local players</param>
    public virtual IEnumerable<InputSource> MenuSources(LocalPlayer pausedBy, IReadOnlyList<LocalPlayer> players)
        => MenuControl == PauseMenuControl.AnyPlayer
            ? players.Select(p => p.Input)
            : new[] { pausedBy.Input };
}
