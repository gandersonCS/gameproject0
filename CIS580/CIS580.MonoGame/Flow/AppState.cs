using System.Collections.Generic;
using Microsoft.Xna.Framework;
using CIS580.Input;
using CIS580.UI;

namespace CIS580.Flow;

/// <summary>
/// One high-level state of the app, such as the main menu, loading, playing, or paused.
/// </summary>
/// <remarks>
/// <para>
/// States are the app's controllers: a state decides what is on screen (by showing layers),
/// who the menus listen to, and what happens when the player chooses something. Layers report
/// what the player chose (through callbacks the state gives them), and the state responds,
/// typically by asking the <see cref="AppFlow"/> to change state.
/// </para>
/// <para>
/// Gameplay itself is not an app state: the game in progress lives in the simulation, which an
/// in-game state owns. App states are only about where the player is in the app.
/// </para>
/// </remarks>
public abstract class AppState
{
    private readonly List<Layer> _layers = new();

    /// <summary>
    /// The flow this state is in
    /// </summary>
    public AppFlow Flow { get; internal set; }

    /// <summary>
    /// True if this is the topmost state (e.g. the in-game state is not on top while paused)
    /// </summary>
    public bool IsTop => Flow != null && Flow.Current == this;

    protected Game Game => Flow.Game;
    protected LayerStack Layers => Flow.Layers;
    protected UiInput Ui => Flow.Ui;
    protected InputState Input => Flow.Layers.Input;

    /// <summary>
    /// Called when the state becomes part of the flow. Show its layers here.
    /// </summary>
    protected internal virtual void Enter() { }

    /// <summary>
    /// Called when the state leaves the flow. Its layers are closed automatically afterward.
    /// </summary>
    protected internal virtual void Exit() { }

    /// <summary>
    /// Called when another state is pushed on top of this one
    /// </summary>
    protected internal virtual void Covered() { }

    /// <summary>
    /// Called when the state above this one is popped, making this the top state again
    /// </summary>
    protected internal virtual void Uncovered() { }

    /// <summary>
    /// Updates the state. Called every frame for every state in the flow, bottom to top,
    /// so check <see cref="IsTop"/> for things that should only happen on the top state.
    /// </summary>
    public virtual void Update(GameTime gameTime) { }

    /// <summary>
    /// Shows a layer belonging to this state; it is closed when the state exits
    /// </summary>
    protected T Show<T>(T layer) where T : Layer
    {
        _layers.Add(layer);
        Layers.Push(layer);
        return layer;
    }

    /// <summary>
    /// Sets which input sources the menus listen to
    /// </summary>
    protected void ListenTo(IEnumerable<InputSource> sources)
    {
        Ui.Sources.Clear();
        Ui.Sources.AddRange(sources);
    }

    internal void CloseLayers()
    {
        foreach (var layer in _layers)
            layer.Close();
        _layers.Clear();
    }
}
