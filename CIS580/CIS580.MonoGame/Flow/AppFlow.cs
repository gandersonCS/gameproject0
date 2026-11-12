using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using CIS580.Input;
using CIS580.UI;

namespace CIS580.Flow;

/// <summary>
/// Moves the app between its high-level states (main menu, loading, in game, paused, ...).
/// </summary>
/// <remarks>
/// <para>
/// The flow is a stack of <see cref="AppState"/>s: <see cref="Change"/> replaces the whole
/// stack (e.g. main menu to game), <see cref="Push"/> puts a state on top of the current one
/// (e.g. pausing over the game, which keeps the game underneath), and <see cref="Pop"/> returns
/// to the state beneath (e.g. resuming).
/// </para>
/// <para>
/// Requests are applied at the start of the next <see cref="Update"/>, never in the middle of
/// one, so a state or layer can safely request a change from anywhere.
/// </para>
/// </remarks>
public sealed class AppFlow
{
    private readonly List<AppState> _states = new();
    private readonly Queue<Action> _requests = new();

    /// <summary>
    /// The game
    /// </summary>
    public Game Game { get; }

    /// <summary>
    /// The user interface layers
    /// </summary>
    public LayerStack Layers { get; }

    /// <summary>
    /// Menu navigation input
    /// </summary>
    public UiInput Ui { get; }

    /// <summary>
    /// The states, bottom to top
    /// </summary>
    public IReadOnlyList<AppState> States => _states;

    /// <summary>
    /// The topmost state, or null if there is none
    /// </summary>
    public AppState Current => _states.Count > 0 ? _states[^1] : null;

    /// <summary>
    /// Raised after the flow changes state, with a description such as "Push PausedState".
    /// Handy for logging while debugging.
    /// </summary>
    public event Action<string> StateChanged;

    public AppFlow(Game game, LayerStack layers, UiInput ui)
    {
        Game = game;
        Layers = layers;
        Ui = ui;
    }

    /// <summary>
    /// Replaces every state with the given state
    /// </summary>
    public void Change(AppState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _requests.Enqueue(() =>
        {
            while (_states.Count > 0)
                RemoveTop();
            Add(state);
            StateChanged?.Invoke($"Change to {state.GetType().Name}");
        });
    }

    /// <summary>
    /// Puts a state on top of the current one
    /// </summary>
    public void Push(AppState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _requests.Enqueue(() =>
        {
            Current?.Covered();
            Add(state);
            StateChanged?.Invoke($"Push {state.GetType().Name}");
        });
    }

    /// <summary>
    /// Removes the top state, returning to the one beneath
    /// </summary>
    public void Pop()
    {
        _requests.Enqueue(() =>
        {
            if (_states.Count == 0)
                return;
            var popped = RemoveTop();
            Current?.Uncovered();
            StateChanged?.Invoke($"Pop {popped.GetType().Name}");
        });
    }

    /// <summary>
    /// Applies any requested state changes, then updates every state, bottom to top.
    /// Call once per frame, after reading input and before updating the layers.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        // Applying a request may cause states to make more requests; those wait for the next frame
        int pending = _requests.Count;
        for (int i = 0; i < pending; i++)
            _requests.Dequeue()();

        for (int i = 0; i < _states.Count; i++)
            _states[i].Update(gameTime);
    }

    private void Add(AppState state)
    {
        state.Flow = this;
        _states.Add(state);
        state.Enter();
    }

    private AppState RemoveTop()
    {
        var state = _states[^1];
        _states.RemoveAt(_states.Count - 1);
        state.Exit();
        state.CloseLayers();
        state.Flow = null;
        return state;
    }
}
