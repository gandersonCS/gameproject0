using System;
using System.Collections.Generic;
using CIS580.Input;

namespace CIS580.UI.Widgets;

/// <summary>
/// One item in a <see cref="Menu"/>
/// </summary>
public abstract class MenuItem
{
    /// <summary>
    /// The text shown for the item
    /// </summary>
    public abstract string Text { get; }

    /// <summary>
    /// Disabled items are drawn greyed out and cannot be selected
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// How strongly the item is drawn as selected (0 to 1); fades as the selection moves
    /// </summary>
    internal float SelectionFade { get; set; }

    /// <summary>
    /// Called when the player chooses the item (e.g. presses Confirm on it)
    /// </summary>
    /// <param name="source">Who chose it</param>
    public virtual void Activate(InputSource source) { }

    /// <summary>
    /// Called when the player presses left (-1) or right (+1) on the item.
    /// Returns true if the item changed.
    /// </summary>
    public virtual bool Adjust(int direction, InputSource source) => false;
}

/// <summary>
/// A menu item that does something when chosen, such as "Play Game"
/// </summary>
public sealed class ButtonItem : MenuItem
{
    private readonly Action<InputSource> _onActivate;

    public ButtonItem(string text, Action<InputSource> onActivate)
    {
        Label = text;
        _onActivate = onActivate;
    }

    /// <summary>
    /// The button's text
    /// </summary>
    public string Label { get; set; }

    public override string Text => Label;

    public override void Activate(InputSource source) => _onActivate?.Invoke(source);
}

/// <summary>
/// A menu item that switches a setting on or off, shown as e.g. "Sound: On"
/// </summary>
public sealed class ToggleItem : MenuItem
{
    private readonly string _label;
    private readonly Func<bool> _get;
    private readonly Action<bool> _set;

    public string OnText { get; init; } = "On";
    public string OffText { get; init; } = "Off";

    /// <param name="label">What the setting is called</param>
    /// <param name="get">Reads the setting</param>
    /// <param name="set">Changes the setting</param>
    public ToggleItem(string label, Func<bool> get, Action<bool> set)
    {
        _label = label;
        _get = get;
        _set = set;
    }

    public override string Text => $"{_label}: {(_get() ? OnText : OffText)}";

    public override void Activate(InputSource source) => _set(!_get());

    public override bool Adjust(int direction, InputSource source)
    {
        _set(!_get());
        return true;
    }
}

/// <summary>
/// A menu item that chooses one of several values, shown as e.g. "Difficulty: Normal".
/// Left and right step through the choices; choosing the item steps forward.
/// </summary>
public sealed class ChoiceItem<T> : MenuItem
{
    private readonly string _label;
    private readonly IReadOnlyList<T> _choices;
    private readonly Func<T> _get;
    private readonly Action<T> _set;
    private readonly Func<T, string> _format;

    /// <param name="label">What the setting is called</param>
    /// <param name="choices">The values to choose from</param>
    /// <param name="get">Reads the setting</param>
    /// <param name="set">Changes the setting</param>
    /// <param name="format">Turns a value into text (ToString() if null)</param>
    public ChoiceItem(string label, IReadOnlyList<T> choices, Func<T> get, Action<T> set, Func<T, string> format = null)
    {
        if (choices == null || choices.Count == 0)
            throw new ArgumentException("There must be at least one choice.", nameof(choices));

        _label = label;
        _choices = choices;
        _get = get;
        _set = set;
        _format = format ?? (value => value?.ToString());
    }

    public override string Text => $"{_label}: {_format(_get())}";

    public override void Activate(InputSource source) => Adjust(1, source);

    public override bool Adjust(int direction, InputSource source)
    {
        int index = IndexOfCurrent();
        int count = _choices.Count;
        _set(_choices[((index + direction) % count + count) % count]);
        return true;
    }

    private int IndexOfCurrent()
    {
        var current = _get();
        for (int i = 0; i < _choices.Count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(_choices[i], current))
                return i;
        }
        return 0;
    }
}

/// <summary>
/// A menu item that chooses a number in a range, shown as e.g. "Lives: 3".
/// Left and right step down and up, stopping at the ends; choosing the item steps up,
/// wrapping around to the minimum.
/// </summary>
public sealed class RangeItem : MenuItem
{
    private readonly string _label;
    private readonly int _min;
    private readonly int _max;
    private readonly int _step;
    private readonly Func<int> _get;
    private readonly Action<int> _set;
    private readonly Func<int, string> _format;

    public RangeItem(string label, int min, int max, int step, Func<int> get, Action<int> set,
        Func<int, string> format = null)
    {
        if (max < min) throw new ArgumentException("max must be at least min.", nameof(max));
        if (step <= 0) throw new ArgumentOutOfRangeException(nameof(step), "Must be greater than zero.");

        _label = label;
        _min = min;
        _max = max;
        _step = step;
        _get = get;
        _set = set;
        _format = format ?? (value => value.ToString());
    }

    public override string Text => $"{_label}: {_format(_get())}";

    public override void Activate(InputSource source)
    {
        int next = _get() + _step;
        _set(next > _max ? _min : next);
    }

    public override bool Adjust(int direction, InputSource source)
    {
        int current = _get();
        int next = Math.Clamp(current + direction * _step, _min, _max);
        _set(next);
        return next != current;
    }
}
