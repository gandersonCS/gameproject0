using System;
using Microsoft.Xna.Framework;
using CIS580.Input;

namespace CIS580.UI;

/// <summary>
/// A popup asking the player to confirm or cancel, such as "Are you sure you want to quit?"
/// </summary>
/// <example>
/// <code>
/// var prompt = new PromptLayer("Are you sure you want to exit?");
/// prompt.Accepted += source => Game.Exit();
/// Open(prompt);     // from another layer, so the prompt closes along with it
/// </code>
/// </example>
public class PromptLayer : Layer
{
    private readonly string _message;
    private readonly bool _showHints;
    private string _text;

    /// <summary>
    /// Raised when the player confirms, with who confirmed
    /// </summary>
    public event Action<InputSource> Accepted;

    /// <summary>
    /// Raised when the player cancels, with who cancelled
    /// </summary>
    public event Action<InputSource> Cancelled;

    /// <param name="message">The question to ask</param>
    /// <param name="showHints">Whether to list the confirm and cancel controls under the message</param>
    public PromptLayer(string message, bool showHints = true)
    {
        _message = message;
        _showHints = showHints;
        IsPopup = true;
    }

    protected internal override void Load()
    {
        Transition.OnDuration = Theme.PopupTransition;
        Transition.OffDuration = Theme.PopupTransition;

        // The hints are built from the current bindings, so they stay correct if controls are rebound
        _text = _showHints
            ? $"{_message}\n{Ui.DescribeBindings(UiControl.Confirm)} = ok\n{Ui.DescribeBindings(UiControl.Cancel)} = cancel"
            : _message;
    }

    public override void HandleInput(GameTime gameTime)
    {
        if (Ui.WasPressed(UiControl.Confirm, out var source))
        {
            Theme.Sounds.Play(UiSound.Confirm);
            Accepted?.Invoke(source);
            Close();
        }
        else if (Ui.WasPressed(UiControl.Cancel, out source))
        {
            Theme.Sounds.Play(UiSound.Cancel);
            Cancelled?.Invoke(source);
            Close();
        }
    }

    public override void Draw(GameTime gameTime) => DrawMessagePanel(_text);
}
