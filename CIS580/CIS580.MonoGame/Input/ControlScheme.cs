using System;
using System.Collections.Generic;
using System.Linq;

namespace CIS580.Input;

/// <summary>
/// A named set of controls, each bound to one or more physical inputs.
/// </summary>
/// <remarks>
/// <para>
/// A control is anything the player can do, identified by name: the standard controls in
/// <see cref="StandardControls"/> (moving, looking, menu navigation, pausing) plus the
/// game's own actions, which use the names of the game's action enum values.
/// </para>
/// <para>
/// A scheme is just data, so players can rebind it in-game and it can be saved as JSON
/// (see <see cref="InputJson"/>). Several players can share the keyboard by using different
/// schemes, such as one using WASD and another using the arrow keys.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var scheme = new ControlScheme("Keyboard")
///     .Bind(StandardControls.MoveLeft, new KeyBinding(Keys.A), new KeyBinding(Keys.Left))
///     .Bind(nameof(GameAction.Jump), new KeyBinding(Keys.Space));
/// </code>
/// </example>
public sealed class ControlScheme
{
    /// <summary>
    /// The scheme's name, for display and for saving
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The bindings for each control, by control name
    /// </summary>
    public Dictionary<string, List<Binding>> Controls { get; init; } = new();

    public ControlScheme() : this("Unnamed") { }

    public ControlScheme(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the bindings for a control (empty if the control is unbound)
    /// </summary>
    public IReadOnlyList<Binding> GetBindings(string control)
        => Controls.TryGetValue(control, out var bindings) ? bindings : Array.Empty<Binding>();

    /// <summary>
    /// Adds bindings to a control, keeping any it already has.
    /// Returns this scheme, so calls can be chained.
    /// </summary>
    public ControlScheme Bind(string control, params Binding[] bindings)
    {
        if (!Controls.TryGetValue(control, out var list))
            Controls[control] = list = new List<Binding>();

        foreach (var binding in bindings)
        {
            if (!list.Contains(binding))
                list.Add(binding);
        }
        return this;
    }

    /// <summary>
    /// Replaces all of a control's bindings
    /// </summary>
    public void SetBindings(string control, IEnumerable<Binding> bindings)
        => Controls[control] = bindings.Distinct().ToList();

    /// <summary>
    /// Removes a binding from a control
    /// </summary>
    public void Unbind(string control, Binding binding)
    {
        if (Controls.TryGetValue(control, out var list))
            list.Remove(binding);
    }

    /// <summary>
    /// Finds the controls that use a binding, for detecting conflicts when rebinding
    /// </summary>
    public IEnumerable<string> FindControlsUsing(Binding binding)
        => Controls.Where(c => c.Value.Contains(binding)).Select(c => c.Key);

    /// <summary>
    /// Binds a control to a single input, as in a "press a key for Jump" menu.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The control's old bindings are replaced. If another control in <paramref name="group"/>
    /// already uses the input, the input is taken from it; if that leaves it with nothing, it
    /// gets the old bindings instead (so the two controls swap, and nothing is left unbound).
    /// </para>
    /// <para>
    /// The group is the controls that are used at the same time and so mustn't share inputs,
    /// e.g. all the in-game controls. Controls outside the group may share the input: the menu
    /// controls deliberately use the same keys as movement.
    /// </para>
    /// </remarks>
    /// <param name="control">The control to bind</param>
    /// <param name="binding">The input to bind it to</param>
    /// <param name="group">The controls that mustn't share inputs with this one</param>
    /// <returns>The controls the input was taken from</returns>
    public IReadOnlyList<string> Rebind(string control, Binding binding, IEnumerable<string> group)
    {
        ArgumentNullException.ThrowIfNull(binding);
        var oldBindings = GetBindings(control).ToList();
        var displaced = new List<string>();

        foreach (var other in group ?? Enumerable.Empty<string>())
        {
            if (other == control || !GetBindings(other).Contains(binding))
                continue;

            Unbind(other, binding);
            if (GetBindings(other).Count == 0)
                SetBindings(other, oldBindings.Where(b => b != binding));
            displaced.Add(other);
        }

        SetBindings(control, new[] { binding });
        return displaced;
    }

    /// <summary>
    /// Creates an independent copy of this scheme, e.g. so a player can customize a default scheme
    /// </summary>
    public ControlScheme Clone(string name = null) => new(name ?? Name)
    {
        Controls = Controls.ToDictionary(c => c.Key, c => new List<Binding>(c.Value)),
    };
}
