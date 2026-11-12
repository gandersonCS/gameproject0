using Microsoft.Xna.Framework.Input;

namespace CIS580.Input;

/// <summary>
/// Default control schemes for the standard controls (movement, looking, menus, pause).
/// Games add bindings for their own actions to these; see the game's DefaultControls class.
/// </summary>
public static class StandardSchemes
{
    private static KeyBinding Key(Keys key) => new(key);
    private static GamePadButtonBinding Button(Buttons button) => new(button);

    /// <summary>
    /// The whole keyboard, for a player who doesn't share it: WASD or the arrow keys to move
    /// and navigate menus, Enter or Space to confirm, Escape to cancel or pause.
    /// </summary>
    public static ControlScheme Keyboard() => new ControlScheme("Keyboard")
        .Bind(StandardControls.MoveLeft, Key(Keys.A), Key(Keys.Left))
        .Bind(StandardControls.MoveRight, Key(Keys.D), Key(Keys.Right))
        .Bind(StandardControls.MoveUp, Key(Keys.W), Key(Keys.Up))
        .Bind(StandardControls.MoveDown, Key(Keys.S), Key(Keys.Down))
        .Bind(StandardControls.Pause, Key(Keys.Escape))
        .Bind(StandardControls.UiUp, Key(Keys.W), Key(Keys.Up))
        .Bind(StandardControls.UiDown, Key(Keys.S), Key(Keys.Down))
        .Bind(StandardControls.UiLeft, Key(Keys.A), Key(Keys.Left))
        .Bind(StandardControls.UiRight, Key(Keys.D), Key(Keys.Right))
        .Bind(StandardControls.UiConfirm, Key(Keys.Enter), Key(Keys.Space))
        .Bind(StandardControls.UiCancel, Key(Keys.Escape), Key(Keys.Back));

    /// <summary>
    /// The left side of a shared keyboard: WASD to move and navigate menus,
    /// Space to confirm, Q to cancel, Escape to pause.
    /// </summary>
    public static ControlScheme KeyboardLeft() => new ControlScheme("Keyboard (Left)")
        .Bind(StandardControls.MoveLeft, Key(Keys.A))
        .Bind(StandardControls.MoveRight, Key(Keys.D))
        .Bind(StandardControls.MoveUp, Key(Keys.W))
        .Bind(StandardControls.MoveDown, Key(Keys.S))
        .Bind(StandardControls.Pause, Key(Keys.Escape))
        .Bind(StandardControls.UiUp, Key(Keys.W))
        .Bind(StandardControls.UiDown, Key(Keys.S))
        .Bind(StandardControls.UiLeft, Key(Keys.A))
        .Bind(StandardControls.UiRight, Key(Keys.D))
        .Bind(StandardControls.UiConfirm, Key(Keys.Space))
        .Bind(StandardControls.UiCancel, Key(Keys.Q));

    /// <summary>
    /// The right side of a shared keyboard: the arrow keys to move and navigate menus,
    /// Enter to confirm, Backspace to cancel or pause.
    /// </summary>
    public static ControlScheme KeyboardRight() => new ControlScheme("Keyboard (Right)")
        .Bind(StandardControls.MoveLeft, Key(Keys.Left))
        .Bind(StandardControls.MoveRight, Key(Keys.Right))
        .Bind(StandardControls.MoveUp, Key(Keys.Up))
        .Bind(StandardControls.MoveDown, Key(Keys.Down))
        .Bind(StandardControls.Pause, Key(Keys.Back))
        .Bind(StandardControls.UiUp, Key(Keys.Up))
        .Bind(StandardControls.UiDown, Key(Keys.Down))
        .Bind(StandardControls.UiLeft, Key(Keys.Left))
        .Bind(StandardControls.UiRight, Key(Keys.Right))
        .Bind(StandardControls.UiConfirm, Key(Keys.Enter))
        .Bind(StandardControls.UiCancel, Key(Keys.Back));

    /// <summary>
    /// A gamepad: left stick or D-pad to move and navigate menus, right stick to look,
    /// A to confirm, B or Back to cancel, Start to pause.
    /// </summary>
    public static ControlScheme GamePad() => new ControlScheme("Gamepad")
        .Bind(StandardControls.MoveLeft, new GamePadAxisBinding(GamePadAxis.LeftStickX, false), Button(Buttons.DPadLeft))
        .Bind(StandardControls.MoveRight, new GamePadAxisBinding(GamePadAxis.LeftStickX, true), Button(Buttons.DPadRight))
        .Bind(StandardControls.MoveUp, new GamePadAxisBinding(GamePadAxis.LeftStickY, true), Button(Buttons.DPadUp))
        .Bind(StandardControls.MoveDown, new GamePadAxisBinding(GamePadAxis.LeftStickY, false), Button(Buttons.DPadDown))
        .Bind(StandardControls.LookLeft, new GamePadAxisBinding(GamePadAxis.RightStickX, false))
        .Bind(StandardControls.LookRight, new GamePadAxisBinding(GamePadAxis.RightStickX, true))
        .Bind(StandardControls.LookUp, new GamePadAxisBinding(GamePadAxis.RightStickY, true))
        .Bind(StandardControls.LookDown, new GamePadAxisBinding(GamePadAxis.RightStickY, false))
        .Bind(StandardControls.Pause, Button(Buttons.Start))
        .Bind(StandardControls.UiUp, Button(Buttons.DPadUp), new GamePadAxisBinding(GamePadAxis.LeftStickY, true))
        .Bind(StandardControls.UiDown, Button(Buttons.DPadDown), new GamePadAxisBinding(GamePadAxis.LeftStickY, false))
        .Bind(StandardControls.UiLeft, Button(Buttons.DPadLeft), new GamePadAxisBinding(GamePadAxis.LeftStickX, false))
        .Bind(StandardControls.UiRight, Button(Buttons.DPadRight), new GamePadAxisBinding(GamePadAxis.LeftStickX, true))
        .Bind(StandardControls.UiConfirm, Button(Buttons.A))
        .Bind(StandardControls.UiCancel, Button(Buttons.B), Button(Buttons.Back));
}
