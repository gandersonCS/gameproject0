using System;
using System.Collections.Generic;
using System.Linq;
using CIS580.Feedback;
using CIS580.Input;
using CIS580.Simulation;
using MechaHorde.Simulation;
using MechaHorde.Views;

namespace MechaHorde.Feedback;

/// <summary>
/// Decides how the game "feels" when things happen: screen shake, gamepad rumble, and hit-stop.
/// </summary>
/// <remarks>
/// <para>
/// Like <see cref="Audio.GameAudio"/> for sound, this turns the simulation's events into
/// effects the players notice, without the simulation knowing anything about them. Keeping it
/// in one place makes the game's feel easy to tune.
/// </para>
/// <para>
/// For example, when you add a "player hit" event, you might respond with:
/// <code>
/// case PlayerHit hit:
///     _world.Shake(0.4f);                                          // Shake the view
///     _rumble.Play(PlayerFor(hit.Player), 0.8f, 0.4f, TimeSpan.FromMilliseconds(200));
///     _hitStop.Start(TimeSpan.FromMilliseconds(80));               // Freeze for a moment
///     break;
/// </code>
/// </para>
/// </remarks>
public sealed class GameFeedback
{
    private readonly IReadOnlyList<LocalPlayer> _players;
    private readonly LevelView _world;
    private readonly Rumble _rumble;
    private readonly HitStop _hitStop;

    /// <param name="players">The players on this machine</param>
    /// <param name="world">The view of the world (for screen shake)</param>
    /// <param name="rumble">Vibrates the players' gamepads</param>
    /// <param name="hitStop">Freezes the game for a moment</param>
    public GameFeedback(IReadOnlyList<LocalPlayer> players, LevelView world, Rumble rumble, HitStop hitStop)
    {
        _players = players ?? throw new ArgumentNullException(nameof(players));
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _rumble = rumble ?? throw new ArgumentNullException(nameof(rumble));
        _hitStop = hitStop ?? throw new ArgumentNullException(nameof(hitStop));
    }

    /// <summary>
    /// Responds to something that happened in the game
    /// </summary>
    public void OnGameEvent(GameEvent gameEvent)
    {
        switch (gameEvent)
        {
            case PlayerJoined joined:
                // A short buzz, so players can tell which gamepad is theirs
                var player = PlayerFor(joined.Player);
                if (player != null)
                    _rumble.Play(player, 0.2f, 0.5f, TimeSpan.FromMilliseconds(150));
                break;
        }
    }

    /// <summary>
    /// Finds the local player with a simulation id (null if they play on another machine)
    /// </summary>
    private LocalPlayer PlayerFor(PlayerId id) => _players.FirstOrDefault(p => p.Id == id);
}
