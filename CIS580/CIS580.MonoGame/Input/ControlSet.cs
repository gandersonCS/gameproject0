using System;
using System.Collections.Generic;
using System.Linq;

namespace CIS580.Input;

/// <summary>
/// The game's control schemes, shared by everything that reads input, and editable by the player.
/// </summary>
/// <remarks>
/// <para>
/// There is one <see cref="ControlScheme"/> of each kind (e.g. "Keyboard", "Gamepad"), and
/// every input source uses these same objects. So when a player rebinds a control, the change
/// takes effect everywhere at once: in menus, in the game, and in on-screen hints.
/// </para>
/// <para>
/// The set remembers the default schemes, so a scheme can be reset, and so only the
/// <see cref="GetChanges">changes</see> the player made need to be saved. Saving only the
/// changes means that new controls (or new default bindings) added in a later version of the
/// game still reach players who have saved controls.
/// </para>
/// </remarks>
public sealed class ControlSet
{
    private readonly List<ControlScheme> _defaults;
    private readonly List<ControlScheme> _schemes;

    /// <param name="defaults">The default schemes, with different names</param>
    public ControlSet(IEnumerable<ControlScheme> defaults)
    {
        _defaults = defaults?.Select(s => s.Clone()).ToList() ?? throw new ArgumentNullException(nameof(defaults));
        if (_defaults.Select(s => s.Name).Distinct().Count() != _defaults.Count)
            throw new ArgumentException("Each scheme needs a different name.", nameof(defaults));
        _schemes = _defaults.Select(s => s.Clone()).ToList();
    }

    /// <summary>
    /// The schemes, in the order they were given
    /// </summary>
    public IReadOnlyList<ControlScheme> Schemes => _schemes;

    /// <summary>
    /// Finds a scheme by name
    /// </summary>
    public ControlScheme this[string name]
        => _schemes.Find(s => s.Name == name) ?? throw new KeyNotFoundException($"There is no scheme named \"{name}\".");

    /// <summary>
    /// The default bindings for a control in a scheme
    /// </summary>
    public IReadOnlyList<Binding> DefaultBindings(ControlScheme scheme, string control)
        => DefaultOf(scheme).GetBindings(control);

    /// <summary>
    /// Puts a scheme's bindings back to the defaults
    /// </summary>
    public void ResetToDefaults(ControlScheme scheme)
    {
        var defaults = DefaultOf(scheme);
        scheme.Controls.Clear();
        foreach (var (control, bindings) in defaults.Controls)
            scheme.SetBindings(control, bindings);
    }

    /// <summary>
    /// Puts every scheme back to the defaults
    /// </summary>
    public void ResetAllToDefaults()
    {
        foreach (var scheme in _schemes)
            ResetToDefaults(scheme);
    }

    /// <summary>
    /// The controls the player has changed from the defaults, as schemes containing only those
    /// controls (schemes with no changes are left out). Save these.
    /// </summary>
    public List<ControlScheme> GetChanges()
    {
        var changes = new List<ControlScheme>();
        foreach (var scheme in _schemes)
        {
            var defaults = DefaultOf(scheme);
            var changed = new ControlScheme(scheme.Name);
            foreach (var control in scheme.Controls.Keys.Union(defaults.Controls.Keys))
            {
                var bindings = scheme.GetBindings(control);
                if (!bindings.SequenceEqual(defaults.GetBindings(control)))
                    changed.SetBindings(control, bindings);
            }
            if (changed.Controls.Count > 0)
                changes.Add(changed);
        }
        return changes;
    }

    /// <summary>
    /// Applies saved changes (from <see cref="GetChanges"/>) on top of the current bindings.
    /// Changes to schemes this set doesn't have are ignored.
    /// </summary>
    public void ApplyChanges(IEnumerable<ControlScheme> changes)
    {
        if (changes == null)
            return;

        foreach (var changed in changes)
        {
            var scheme = _schemes.Find(s => s.Name == changed?.Name);
            if (scheme == null)
                continue;
            foreach (var (control, bindings) in changed.Controls)
                scheme.SetBindings(control, bindings?.Where(b => b != null) ?? Enumerable.Empty<Binding>());
        }
    }

    private ControlScheme DefaultOf(ControlScheme scheme)
    {
        int index = _schemes.IndexOf(scheme);
        if (index < 0)
            throw new ArgumentException("That scheme isn't part of this set.", nameof(scheme));
        return _defaults[index];
    }
}
