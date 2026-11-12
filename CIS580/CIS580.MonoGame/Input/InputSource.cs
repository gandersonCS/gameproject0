using System;
using System.Collections.Generic;
using System.Linq;

namespace CIS580.Input;

/// <summary>
/// Where one player's input comes from: one or more devices, each read through a control scheme.
/// </summary>
/// <remarks>
/// Examples:
/// <list type="bullet">
/// <item>a single player: the keyboard (full keyboard scheme) AND their gamepad (gamepad scheme)</item>
/// <item>two players sharing the keyboard: one source with the keyboard and the left-side scheme,
/// another with the keyboard and the right-side scheme</item>
/// <item>a gamepad player: just their gamepad, with the gamepad scheme</item>
/// </list>
/// When a source has several devices, a control's value is the strongest of them.
/// </remarks>
public sealed class InputSource
{
    /// <summary>
    /// A device, and the control scheme used to read it
    /// </summary>
    public sealed record Assignment(InputDevice Device, ControlScheme Scheme);

    private readonly List<Assignment> _assignments;

    /// <summary>
    /// A name for the source, for display (e.g. "Keyboard (Left)")
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The devices this source reads, and their control schemes
    /// </summary>
    public IReadOnlyList<Assignment> Assignments => _assignments;

    public InputSource(string name, params Assignment[] assignments)
    {
        if (assignments == null || assignments.Length == 0)
            throw new ArgumentException("An input source needs at least one device.", nameof(assignments));

        Name = name;
        _assignments = assignments.ToList();
    }

    /// <summary>
    /// Creates a source that reads a single device through a single scheme
    /// </summary>
    public InputSource(InputDevice device, ControlScheme scheme)
        : this(scheme.Name, new Assignment(device, scheme)) { }

    /// <summary>
    /// Describes the inputs bound to a control, for on-screen hints (e.g. "Esc / Start")
    /// </summary>
    /// <param name="control">The control's name (see <see cref="StandardControls"/>)</param>
    public string DescribeBindings(string control)
        => string.Join(" / ", _assignments
            .SelectMany(a => a.Scheme.GetBindings(control))
            .Select(b => b.DisplayName)
            .Distinct());

    /// <summary>
    /// Determines if this source reads the given device
    /// </summary>
    public bool UsesDevice(InputDevice device) => _assignments.Exists(a => a.Device == device);

    /// <summary>
    /// Gets the value of a control this frame, from 0 (not pressed) to 1 (fully pressed)
    /// </summary>
    public float Value(string control, InputState input) => Value(control, input.Current);

    /// <summary>
    /// Determines if a control is held down this frame
    /// </summary>
    public bool IsDown(string control, InputState input)
        => Value(control, input.Current) >= Binding.PressThreshold;

    /// <summary>
    /// Determines if a control was pressed this frame (down now, but not on the prior frame)
    /// </summary>
    public bool WasPressed(string control, InputState input)
        => Value(control, input.Current) >= Binding.PressThreshold
           && Value(control, input.Previous) < Binding.PressThreshold;

    /// <summary>
    /// Determines if a control was released this frame (down on the prior frame, but not now)
    /// </summary>
    public bool WasReleased(string control, InputState input)
        => Value(control, input.Current) < Binding.PressThreshold
           && Value(control, input.Previous) >= Binding.PressThreshold;

    /// <summary>
    /// Determines if a gamepad this source reads has been disconnected
    /// (e.g. so the game can pause until it is reconnected)
    /// </summary>
    public bool HasLostDevice(InputState input) => _assignments.Exists(a =>
        a.Device.Kind == DeviceKind.GamePad && input.GamePadWasDisconnected(a.Device.GamePadIndex));

    private float Value(string control, InputSnapshot snapshot)
    {
        float value = 0f;
        foreach (var assignment in _assignments)
        {
            foreach (var binding in assignment.Scheme.GetBindings(control))
                value = Math.Max(value, binding.Read(snapshot, assignment.Device));
        }
        return value;
    }

    public override string ToString() => Name;
}
