using System;
using System.Collections.Generic;

namespace CIS580.Input;

/// <summary>
/// The menu navigation controls
/// </summary>
public enum UiControl
{
    Up,
    Down,
    Left,
    Right,
    Confirm,
    Cancel,
}

/// <summary>
/// Menu navigation input, read from a chosen set of input sources.
/// </summary>
/// <remarks>
/// <para>
/// Menus ask this class whether a navigation control was pressed, and which source pressed it,
/// rather than reading devices themselves. Whoever owns the menus decides which sources are
/// listened to through <see cref="Sources"/>: for example, every device on the title screen,
/// or only the player who paused in the pause menu.
/// </para>
/// <para>
/// Holding a direction repeats it (after <see cref="RepeatDelay"/>, every
/// <see cref="RepeatInterval"/>), so a player can scroll through a long list by holding down.
/// A control only counts once it has been pressed while listened to, so a button held down
/// when a menu opens does not immediately trigger anything in it.
/// </para>
/// </remarks>
public sealed class UiInput
{
    private static readonly UiControl[] AllControls = Enum.GetValues<UiControl>();

    private readonly InputState _input;

    // How long each (source, control) has been held, for controls pressed while listened to
    private readonly Dictionary<(InputSource, UiControl), TimeSpan> _held = new();

    // The (source, control) pairs that fired this frame, in source order
    private readonly List<(InputSource Source, UiControl Control)> _fired = new();

    /// <summary>
    /// The input sources menus currently listen to
    /// </summary>
    public List<InputSource> Sources { get; } = new();

    /// <summary>
    /// How long a direction must be held before it starts repeating
    /// </summary>
    public TimeSpan RepeatDelay { get; set; } = TimeSpan.FromSeconds(0.4);

    /// <summary>
    /// How often a held direction repeats
    /// </summary>
    public TimeSpan RepeatInterval { get; set; } = TimeSpan.FromSeconds(0.1);

    public UiInput(InputState input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }

    /// <summary>
    /// Gets the name of the standard control for a navigation control
    /// </summary>
    public static string ControlName(UiControl control) => control switch
    {
        UiControl.Up => StandardControls.UiUp,
        UiControl.Down => StandardControls.UiDown,
        UiControl.Left => StandardControls.UiLeft,
        UiControl.Right => StandardControls.UiRight,
        UiControl.Confirm => StandardControls.UiConfirm,
        UiControl.Cancel => StandardControls.UiCancel,
        _ => throw new ArgumentOutOfRangeException(nameof(control)),
    };

    /// <summary>
    /// Updates which controls fired this frame. Call once per frame, after InputState.Update().
    /// </summary>
    /// <param name="elapsed">The time since the last frame</param>
    public void Update(TimeSpan elapsed)
    {
        _fired.Clear();

        foreach (var source in Sources)
        {
            foreach (var control in AllControls)
            {
                var key = (source, control);
                string name = ControlName(control);

                if (source.WasPressed(name, _input))
                {
                    _held[key] = TimeSpan.Zero;
                    _fired.Add(key);
                }
                else if (source.IsDown(name, _input) && _held.TryGetValue(key, out var held))
                {
                    var nowHeld = held + elapsed;
                    _held[key] = nowHeld;
                    if (IsDirection(control) && RepeatCount(nowHeld) > RepeatCount(held))
                        _fired.Add(key);
                }
                else
                {
                    _held.Remove(key);
                }
            }
        }

        // Forget sources that are no longer listened to
        if (_held.Count > 0)
        {
            var stale = new List<(InputSource, UiControl)>();
            foreach (var key in _held.Keys)
                if (!Sources.Contains(key.Item1)) stale.Add(key);
            foreach (var key in stale)
                _held.Remove(key);
        }
    }

    /// <summary>
    /// Determines if the control fired this frame (was pressed, or repeated while held),
    /// and if so, by which source
    /// </summary>
    public bool WasPressed(UiControl control, out InputSource source)
    {
        foreach (var fired in _fired)
        {
            if (fired.Control == control)
            {
                source = fired.Source;
                return true;
            }
        }

        source = null;
        return false;
    }

    /// <summary>
    /// Determines if the control fired this frame (was pressed, or repeated while held)
    /// </summary>
    public bool WasPressed(UiControl control) => WasPressed(control, out _);

    /// <summary>
    /// Lists the inputs bound to a control across the sources being listened to, for showing
    /// hints such as "Enter, Space, A = ok"
    /// </summary>
    public string DescribeBindings(UiControl control)
    {
        var names = new List<string>();
        foreach (var source in Sources)
        {
            foreach (var assignment in source.Assignments)
            {
                foreach (var binding in assignment.Scheme.GetBindings(ControlName(control)))
                {
                    if (!names.Contains(binding.DisplayName))
                        names.Add(binding.DisplayName);
                }
            }
        }
        return string.Join(", ", names);
    }

    private static bool IsDirection(UiControl control) => control <= UiControl.Right;

    private int RepeatCount(TimeSpan held)
        => held < RepeatDelay ? 0 : 1 + (int)((held - RepeatDelay).Ticks / RepeatInterval.Ticks);
}
