using System;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using CIS580.Input;
using CIS580.UI;
using CIS580.UI.Widgets;
using MechaHorde.Input;
using MechaHorde.Settings;

namespace MechaHorde.UI;

/// <summary>
/// The controls menu: rebinding the in-game controls of each control scheme, and inverting
/// the vertical axes. Changes take effect immediately, even in a game in progress, and are
/// saved when the options menu closes.
/// </summary>
public class ControlsLayer : MechaMenuLayer
{
    // The inputs that cancel rebinding rather than being bound
    private static readonly Binding[] KeyboardCancel = { new KeyBinding(Keys.Escape) };
    private static readonly Binding[] GamePadCancel = { new GamePadButtonBinding(Buttons.Back) };

    private GameControls _controls;
    private ControlScheme _scheme;

    /// <param name="overGame">Whether the menu is shown over a game in progress (see OptionsLayer)</param>
    public ControlsLayer(bool overGame = false) : base("Controls")
    {
        // Over the game, the world shows dimly behind the menu
        DimBackground = overGame;
    }

    protected override void Load()
    {
        base.Load();

        var settings = Game.Services.GetService<UserSettings>();
        _controls = settings.Controls;
        _scheme = _controls.Keyboard;
        var profile = settings.Input;

        // Which scheme to edit, then a row for each control showing its current bindings
        Menu.Add(new ChoiceItem<ControlScheme>("Controls for", _controls.Set.Schemes,
            () => _scheme, scheme => _scheme = scheme, scheme => scheme.Name));
        foreach (var control in GameControls.Rebindable)
            Menu.Add(new BindingItem(this, control));

        Menu.Add(new ToggleItem("Invert move up/down", () => profile.InvertMoveY, value => profile.InvertMoveY = value))
            .Add(new ToggleItem("Invert look up/down", () => profile.InvertLookY, value => profile.InvertLookY = value))
            .Add(new ButtonItem("Reset to defaults", _ => ConfirmReset()))
            .Add(new ButtonItem("Back", OnCancel));
    }

    /// <summary>
    /// Waits for the player to press the input to bind a control to
    /// </summary>
    private void Rebind(string control)
    {
        var scheme = _scheme;
        bool gamePad = scheme == _controls.GamePad;
        string what = gamePad ? "a button or stick" : "a key";

        var capture = new BindingCaptureLayer($"Press {what} for {ControlName(control)}",
            _controls.DevicesFor(scheme), gamePad ? GamePadCancel : KeyboardCancel);
        capture.Captured += binding => scheme.Rebind(control, binding, GameControls.Rebindable);
        Open(capture);
    }

    private void ConfirmReset()
    {
        var scheme = _scheme;
        var prompt = new PromptLayer($"Reset the {scheme.Name} controls to their defaults?");
        prompt.Accepted += _ => _controls.Set.ResetToDefaults(scheme);
        Open(prompt);
    }

    /// <summary>
    /// A friendlier name for a control, e.g. "MoveUp" becomes "Move up"
    /// </summary>
    private static string ControlName(string control)
        => string.Concat(control.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + char.ToLower(c) : c.ToString()));

    /// <summary>
    /// A menu item showing a control and its bindings in the scheme being edited, e.g.
    /// "Move up: W, Up". Choosing it rebinds the control.
    /// </summary>
    private sealed class BindingItem : MenuItem
    {
        private readonly ControlsLayer _owner;
        private readonly string _control;

        public BindingItem(ControlsLayer owner, string control)
        {
            _owner = owner;
            _control = control;
        }

        public override string Text
        {
            get
            {
                var bindings = _owner._scheme.GetBindings(_control);
                string inputs = bindings.Count > 0 ? string.Join(", ", bindings.Select(b => b.DisplayName)) : "(none)";
                return $"{ControlName(_control)}: {inputs}";
            }
        }

        public override void Activate(InputSource source) => _owner.Rebind(_control);
    }
}
