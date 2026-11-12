using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using CIS580.UI;

namespace CIS580.Flow;

/// <summary>
/// Shows a loading screen while another state loads, then changes to that state.
/// </summary>
/// <remarks>
/// When this state is entered, every layer on screen transitions off and the loading screen
/// comes on. Once the old layers are gone, the target state's <see cref="ILoadable.Load"/>
/// steps are run, as many per frame as fit in <see cref="FrameBudget"/>, and then the flow
/// changes to the target state.
/// </remarks>
public sealed class LoadingState : AppState
{
    private readonly AppState _target;
    private readonly bool _showMessage;
    private LoadingLayer _layer;
    private IEnumerator<float> _steps;
    private bool _finished;

    /// <summary>
    /// How much time loading may use each frame, so the loading screen stays responsive
    /// </summary>
    public TimeSpan FrameBudget { get; set; } = TimeSpan.FromMilliseconds(10);

    /// <param name="target">The state to load and then change to</param>
    /// <param name="showMessage">
    /// Whether to show a loading message and progress; quick loads look better without
    /// </param>
    public LoadingState(AppState target, bool showMessage = true)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _showMessage = showMessage;
    }

    protected internal override void Enter()
    {
        ListenTo(Array.Empty<Input.InputSource>());

        // Clear the screen of every layer (not just this state's) before loading
        _layer = new LoadingLayer(_showMessage);
        Layers.ReplaceAll(_layer);
    }

    protected internal override void Exit()
    {
        _layer.Close();
    }

    public override void Update(GameTime gameTime)
    {
        // Wait until the old layers have gone and the loading layer is on screen
        if (_finished || _layer.Stack == null)
            return;

        if (_steps == null)
        {
            // The target isn't in the flow until loading finishes, but its loading code needs
            // the game (for services and content), so attach it to the flow now
            _target.Flow = Flow;
            _steps = (_target is ILoadable loadable ? loadable.Load() : Array.Empty<float>()).GetEnumerator();
        }

        var timer = Stopwatch.StartNew();
        do
        {
            if (!_steps.MoveNext())
            {
                _finished = true;
                _layer.Progress = 1f;
                Flow.Change(_target);

                // Loading may have taken a while; don't try to catch up on that time
                Game.ResetElapsedTime();
                return;
            }
            _layer.Progress = Math.Clamp(_steps.Current, 0f, 1f);
        }
        while (timer.Elapsed < FrameBudget);
    }
}
