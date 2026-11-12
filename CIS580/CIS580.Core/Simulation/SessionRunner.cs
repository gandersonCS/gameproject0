using System;
using System.Collections.Generic;

namespace CIS580.Simulation;

/// <summary>
/// Runs a simulation at a fixed tick rate, gathering each player's command every tick.
/// </summary>
/// <remarks>
/// <para>
/// Games render at whatever rate the hardware allows, but a simulation is much easier to
/// reason about (and can be made deterministic) if it always advances by the same amount of
/// time. The runner accumulates the real time passed to <see cref="Update"/>, and runs as
/// many whole ticks as fit, carrying any leftover time over to the next update.
/// </para>
/// <para>
/// This class uses no MonoGame types, so it can also drive a simulation inside a headless
/// server. Note that floating-point results can differ slightly between CPU architectures,
/// so networked games that rely on determinism should run the same build on every machine.
/// </para>
/// </remarks>
public sealed class SessionRunner
{
    private readonly ISimulation _simulation;

    // Players in the order they joined, which is the order their inputs are given to the simulation
    private readonly List<PlayerEntry> _players = new();
    private readonly List<PlayerInput> _inputs = new();

    private readonly List<PlayerId> _pendingRemovals = new();
    private readonly List<PlayerEntry> _pendingAdditions = new();

    private TimeSpan _accumulated;

    private sealed class PlayerEntry
    {
        public PlayerId Id;
        public ICommandSource Source;
        public PlayerCommand Previous;
    }

    /// <summary>
    /// The number of ticks run so far, which is also the number of the next tick to run
    /// </summary>
    public long Tick { get; private set; }

    /// <summary>
    /// The number of ticks per second of simulated time
    /// </summary>
    public int TicksPerSecond { get; }

    /// <summary>
    /// The length of one tick
    /// </summary>
    public TimeSpan TickDuration { get; }

    /// <summary>
    /// The most ticks a single call to Update will run.
    /// </summary>
    /// <remarks>
    /// If the game falls far behind (for instance, while paused in a debugger), running every
    /// missed tick would make the next frame even slower, falling further behind. Instead,
    /// excess time beyond this many ticks is discarded, and the game simply slows down.
    /// </remarks>
    public int MaxTicksPerUpdate { get; set; } = 5;

    /// <summary>
    /// How far the simulation is between its last tick and its next tick, from 0 to 1.
    /// Views can use this to interpolate positions for smoother drawing.
    /// </summary>
    public float Alpha => (float)(_accumulated.Ticks / (double)TickDuration.Ticks);

    /// <summary>
    /// The players currently in the session, in the order they joined
    /// </summary>
    public IEnumerable<PlayerId> Players
    {
        get { foreach (var player in _players) yield return player.Id; }
    }

    /// <summary>
    /// Creates a runner for the simulation
    /// </summary>
    /// <param name="simulation">The simulation to run</param>
    /// <param name="ticksPerSecond">The number of ticks per second of simulated time</param>
    public SessionRunner(ISimulation simulation, int ticksPerSecond = 60)
    {
        if (ticksPerSecond <= 0)
            throw new ArgumentOutOfRangeException(nameof(ticksPerSecond), "Must be greater than zero.");

        _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
        TicksPerSecond = ticksPerSecond;
        TickDuration = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / ticksPerSecond);
    }

    /// <summary>
    /// Adds a player to the session, starting with the next tick
    /// </summary>
    /// <param name="player">The player's id, which must not already be in use</param>
    /// <param name="source">Where the player's commands come from</param>
    public void AddPlayer(PlayerId player, ICommandSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (HasPlayer(player))
            throw new InvalidOperationException($"{player} is already in the session.");

        _pendingAdditions.Add(new PlayerEntry { Id = player, Source = source });
    }

    /// <summary>
    /// Removes a player from the session, starting with the next tick
    /// </summary>
    public void RemovePlayer(PlayerId player)
    {
        int pending = _pendingAdditions.FindIndex(p => p.Id == player);
        if (pending >= 0)
            _pendingAdditions.RemoveAt(pending);   // They never actually joined
        else if (_players.Exists(p => p.Id == player) && !_pendingRemovals.Contains(player))
            _pendingRemovals.Add(player);
    }

    /// <summary>
    /// Determines if the player is in the session, or will be as of the next tick
    /// </summary>
    public bool HasPlayer(PlayerId player)
        => (_players.Exists(p => p.Id == player) && !_pendingRemovals.Contains(player))
           || _pendingAdditions.Exists(p => p.Id == player);

    /// <summary>
    /// Advances the simulation by the given amount of real time, running as many ticks as fit.
    /// </summary>
    /// <param name="elapsed">The time since the last update</param>
    /// <returns>The number of ticks that were run</returns>
    public int Update(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(elapsed), "Elapsed time cannot be negative.");

        _accumulated += elapsed;

        int ticksRun = 0;
        while (_accumulated >= TickDuration && ticksRun < MaxTicksPerUpdate)
        {
            _accumulated -= TickDuration;
            Step();
            ticksRun++;
        }

        // If we hit the limit, discard the backlog rather than trying to catch up later
        if (_accumulated >= TickDuration)
            _accumulated = TimeSpan.FromTicks(_accumulated.Ticks % TickDuration.Ticks);

        return ticksRun;
    }

    /// <summary>
    /// Runs exactly one tick, regardless of elapsed time.
    /// Useful for tests, and for stepping through a paused game one tick at a time.
    /// </summary>
    public void Step()
    {
        ApplyRosterChanges();

        _inputs.Clear();
        foreach (var player in _players)
        {
            var command = player.Source.GetCommand(Tick);
            _inputs.Add(new PlayerInput(player.Id, command, player.Previous));
            player.Previous = command;
        }

        _simulation.Tick(new TickContext(Tick, (float)TickDuration.TotalSeconds, _inputs));
        Tick++;
    }

    /// <summary>
    /// Applies players joining and leaving at a tick boundary, so that every machine in a
    /// networked game can apply them on the same tick.
    /// </summary>
    private void ApplyRosterChanges()
    {
        foreach (var player in _pendingRemovals)
        {
            _players.RemoveAll(p => p.Id == player);
            _simulation.RemovePlayer(player);
        }
        _pendingRemovals.Clear();

        foreach (var player in _pendingAdditions)
        {
            _players.Add(player);
            _simulation.AddPlayer(player.Id);
        }
        _pendingAdditions.Clear();
    }
}
