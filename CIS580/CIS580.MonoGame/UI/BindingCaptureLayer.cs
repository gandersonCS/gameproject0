using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using CIS580.Input;

namespace CIS580.UI;

/// <summary>
/// A popup that waits for the player to press an input to bind to a control, as in
/// "Press a key for Jump".
/// </summary>
/// <remarks>
/// The first input newly pressed on any of the watched devices is captured (see
/// <see cref="BindingCapture"/>). A few inputs are reserved for cancelling instead, so the
/// player can always back out; those can't be bound here. The popup also gives up after a while,
/// in case the player walks away.
/// </remarks>
public class BindingCaptureLayer : Layer
{
    private readonly IReadOnlyList<InputDevice> _devices;
    private readonly HashSet<Binding> _cancelInputs;
    private readonly string _text;
    private TimeSpan _waited;
    private bool _ready;

    /// <summary>
    /// Raised with the input the player pressed
    /// </summary>
    public event Action<Binding> Captured;

    /// <summary>
    /// Raised if the player cancels, or the popup gives up waiting
    /// </summary>
    public event Action Cancelled;

    /// <summary>
    /// How long to wait for an input before giving up
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <param name="message">What to ask, e.g. "Press a key for Jump"</param>
    /// <param name="devices">The devices to watch</param>
    /// <param name="cancelInputs">Inputs that cancel rather than being captured (e.g. Esc)</param>
    public BindingCaptureLayer(string message, IEnumerable<InputDevice> devices, IEnumerable<Binding> cancelInputs)
    {
        _devices = devices?.ToList() ?? throw new ArgumentNullException(nameof(devices));
        _cancelInputs = new HashSet<Binding>(cancelInputs ?? Enumerable.Empty<Binding>());

        string cancel = string.Join(" / ", _cancelInputs.Select(b => b.DisplayName));
        _text = _cancelInputs.Count > 0 ? $"{message}\n({cancel} to cancel)" : message;
        IsPopup = true;
    }

    protected internal override void Load()
    {
        Transition.OnDuration = Theme.PopupTransition;
        Transition.OffDuration = Theme.PopupTransition;
    }

    public override void HandleInput(GameTime gameTime)
    {
        // Skip the frame the popup opened on, so the press that opened it isn't captured
        if (!_ready)
        {
            _ready = true;
            return;
        }

        _waited += gameTime.ElapsedGameTime;
        if (_waited >= Timeout)
        {
            Cancel();
            return;
        }

        foreach (var device in _devices)
        {
            if (!BindingCapture.TryCapture(Input, device, out var binding))
                continue;

            if (_cancelInputs.Contains(binding))
            {
                Cancel();
            }
            else
            {
                Theme.Sounds.Play(UiSound.Confirm);
                Captured?.Invoke(binding);
                Close();
            }
            return;
        }
    }

    public override void Draw(GameTime gameTime) => DrawMessagePanel(_text);

    private void Cancel()
    {
        Theme.Sounds.Play(UiSound.Cancel);
        Cancelled?.Invoke();
        Close();
    }
}
