using System;
using System.Collections.Generic;
using System.Linq;

namespace CIS580.Input;

/// <summary>
/// A player who has joined a <see cref="LocalLobby"/>
/// </summary>
public sealed class LobbySeat
{
    internal LobbySeat(InputSource input) => Input = input;

    /// <summary>
    /// The input source the player joined with
    /// </summary>
    public InputSource Input { get; }

    /// <summary>
    /// Whether the player is ready to start
    /// </summary>
    public bool IsReady { get; internal set; }
}

/// <summary>
/// What happened in a <see cref="LocalLobby"/> during an update
/// </summary>
public enum LobbyChange
{
    Joined,
    Left,
    Ready,
    NotReady,
}

/// <summary>
/// Gathers the players for a local multiplayer game: each person joins by pressing confirm on
/// their device, then marks themselves ready.
/// </summary>
/// <remarks>
/// <para>
/// The lobby is given the input sources people might join with (the <see cref="Candidates"/>),
/// e.g. each gamepad, and each half of a shared keyboard. Each update:
/// </para>
/// <list type="bullet">
/// <item>confirm on a candidate nobody has joined with joins it, taking the next seat</item>
/// <item>confirm on a joined source marks that player ready</item>
/// <item>cancel on a ready player's source marks them not ready; cancel again leaves</item>
/// </list>
/// <para>
/// The lobby only tracks who has joined; a state shows it, and starts the game when
/// <see cref="CanStart"/> (see the game's LobbyState).
/// </para>
/// </remarks>
public sealed class LocalLobby
{
    private readonly List<InputSource> _candidates;
    private readonly List<LobbySeat> _seats = new();

    /// <param name="candidates">The input sources people can join with</param>
    /// <param name="maxPlayers">The most players that can join</param>
    public LocalLobby(IEnumerable<InputSource> candidates, int maxPlayers = 4)
    {
        _candidates = candidates?.ToList() ?? throw new ArgumentNullException(nameof(candidates));
        if (maxPlayers < 1)
            throw new ArgumentOutOfRangeException(nameof(maxPlayers), "There must be room for at least one player.");
        MaxPlayers = maxPlayers;
    }

    /// <summary>
    /// The input sources people can join with
    /// </summary>
    public IReadOnlyList<InputSource> Candidates => _candidates;

    /// <summary>
    /// The players who have joined, in the order they joined
    /// </summary>
    public IReadOnlyList<LobbySeat> Seats => _seats;

    /// <summary>
    /// The most players that can join
    /// </summary>
    public int MaxPlayers { get; }

    /// <summary>
    /// The fewest players needed to start
    /// </summary>
    public int MinPlayers { get; set; } = 2;

    /// <summary>
    /// Whether the game can start: enough players have joined, and they are all ready
    /// </summary>
    public bool CanStart => _seats.Count >= MinPlayers && _seats.All(s => s.IsReady);

    /// <summary>
    /// Whether another player can join
    /// </summary>
    public bool IsFull => _seats.Count >= MaxPlayers;

    /// <summary>
    /// The candidates nobody has joined with yet
    /// </summary>
    public IEnumerable<InputSource> Available => _candidates.Where(c => !_seats.Any(s => s.Input == c));

    /// <summary>
    /// Raised when a player joins, leaves, or changes whether they are ready
    /// </summary>
    public event Action<LobbySeat, LobbyChange> Changed;

    /// <summary>
    /// Handles joining, readying, and leaving
    /// </summary>
    /// <returns>Whether anything changed</returns>
    public bool Update(InputState input)
    {
        bool changed = false;

        // Joined players: confirm to get ready; cancel to stop being ready, or to leave
        foreach (var seat in _seats.ToList())
        {
            if (!seat.IsReady && seat.Input.WasPressed(StandardControls.UiConfirm, input))
                changed |= SetReady(seat, true);
            else if (seat.Input.WasPressed(StandardControls.UiCancel, input))
                changed |= seat.IsReady ? SetReady(seat, false) : Leave(seat);
        }

        // Everyone else: confirm to join
        foreach (var candidate in Available.ToList())
        {
            if (!IsFull && candidate.WasPressed(StandardControls.UiConfirm, input))
            {
                Join(candidate);
                changed = true;
            }
        }

        return changed;
    }

    /// <summary>
    /// Joins a player using one of the candidates (e.g. to add a player without input, for testing)
    /// </summary>
    public LobbySeat Join(InputSource candidate)
    {
        if (!_candidates.Contains(candidate))
            throw new ArgumentException("Only candidates can join.", nameof(candidate));
        if (_seats.Any(s => s.Input == candidate))
            throw new InvalidOperationException($"{candidate.Name} has already joined.");
        if (IsFull)
            throw new InvalidOperationException("The lobby is full.");

        var seat = new LobbySeat(candidate);
        _seats.Add(seat);
        Changed?.Invoke(seat, LobbyChange.Joined);
        return seat;
    }

    /// <summary>
    /// Removes a player from the lobby
    /// </summary>
    public bool Leave(LobbySeat seat)
    {
        if (!_seats.Remove(seat))
            return false;
        Changed?.Invoke(seat, LobbyChange.Left);
        return true;
    }

    /// <summary>
    /// Marks a player ready or not
    /// </summary>
    public bool SetReady(LobbySeat seat, bool ready)
    {
        if (seat.IsReady == ready || !_seats.Contains(seat))
            return false;
        seat.IsReady = ready;
        Changed?.Invoke(seat, ready ? LobbyChange.Ready : LobbyChange.NotReady);
        return true;
    }
}
