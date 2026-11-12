using System;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework.Input;

namespace CIS580.Input;

/// <summary>
/// A single physical input (a key, a button, one direction of a stick, ...) that can be bound
/// to a control.
/// </summary>
/// <remarks>
/// A binding reads as a value from 0 (not pressed) to 1 (fully pressed). Digital inputs such as
/// keys are always 0 or 1; analog inputs such as sticks and triggers can be anywhere between.
/// Bindings are records, so two bindings to the same input are equal, which makes it easy to
/// find conflicts when rebinding. The attributes below let bindings be saved as JSON.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(KeyBinding), "key")]
[JsonDerivedType(typeof(MouseButtonBinding), "mouse")]
[JsonDerivedType(typeof(GamePadButtonBinding), "button")]
[JsonDerivedType(typeof(GamePadAxisBinding), "axis")]
public abstract record Binding
{
    /// <summary>
    /// The value at or above which a binding counts as "pressed"
    /// </summary>
    public const float PressThreshold = 0.5f;

    /// <summary>
    /// The kind of device this binding reads
    /// </summary>
    [JsonIgnore]
    public abstract DeviceKind DeviceKind { get; }

    /// <summary>
    /// Reads the binding's value, from 0 to 1, from the given device
    /// </summary>
    /// <param name="snapshot">The state of the input devices</param>
    /// <param name="device">The device to read; if it is not of this binding's kind, the value is 0</param>
    public abstract float Read(InputSnapshot snapshot, InputDevice device);

    /// <summary>
    /// A short, human-readable name for the binding, for display in menus
    /// </summary>
    [JsonIgnore]
    public abstract string DisplayName { get; }

    public override string ToString() => DisplayName;
}

/// <summary>
/// A keyboard key
/// </summary>
public sealed record KeyBinding(Keys Key) : Binding
{
    [JsonIgnore] // Worked out from the binding, so not saved
    public override DeviceKind DeviceKind => DeviceKind.KeyboardMouse;

    public override float Read(InputSnapshot snapshot, InputDevice device)
        => device.Kind == DeviceKind.KeyboardMouse && snapshot.Keyboard.IsKeyDown(Key) ? 1f : 0f;

    [JsonIgnore] // Worked out from the binding, so not saved
    public override string DisplayName => Key switch
    {
        Keys.Back => "Backspace",      // MonoGame's name for it is easily confused with a gamepad's Back button
        Keys.Escape => "Esc",
        _ => Key.ToString(),
    };
}

/// <summary>
/// The mouse buttons
/// </summary>
public enum MouseButton
{
    Left,
    Right,
    Middle,
    XButton1,
    XButton2,
}

/// <summary>
/// A mouse button
/// </summary>
public sealed record MouseButtonBinding(MouseButton Button) : Binding
{
    [JsonIgnore] // Worked out from the binding, so not saved
    public override DeviceKind DeviceKind => DeviceKind.KeyboardMouse;

    public override float Read(InputSnapshot snapshot, InputDevice device)
    {
        if (device.Kind != DeviceKind.KeyboardMouse)
            return 0f;

        var mouse = snapshot.Mouse;
        var state = Button switch
        {
            MouseButton.Left => mouse.LeftButton,
            MouseButton.Right => mouse.RightButton,
            MouseButton.Middle => mouse.MiddleButton,
            MouseButton.XButton1 => mouse.XButton1,
            MouseButton.XButton2 => mouse.XButton2,
            _ => ButtonState.Released,
        };
        return state == ButtonState.Pressed ? 1f : 0f;
    }

    [JsonIgnore] // Worked out from the binding, so not saved
    public override string DisplayName => $"Mouse {Button}";
}

/// <summary>
/// A gamepad button
/// </summary>
public sealed record GamePadButtonBinding(Buttons Button) : Binding
{
    [JsonIgnore] // Worked out from the binding, so not saved
    public override DeviceKind DeviceKind => DeviceKind.GamePad;

    public override float Read(InputSnapshot snapshot, InputDevice device)
        => device.Kind == DeviceKind.GamePad && snapshot.GamePad(device.GamePadIndex).IsButtonDown(Button) ? 1f : 0f;

    [JsonIgnore] // Worked out from the binding, so not saved
    public override string DisplayName => Button.ToString();
}

/// <summary>
/// The analog axes of a gamepad
/// </summary>
public enum GamePadAxis
{
    LeftStickX,
    LeftStickY,
    RightStickX,
    RightStickY,
    LeftTrigger,
    RightTrigger,
}

/// <summary>
/// One direction of a gamepad axis, such as "left stick, pushed left" or "right trigger".
/// </summary>
/// <param name="Axis">The axis to read</param>
/// <param name="Positive">
/// True to read the axis's positive direction (right or up for sticks), false to read its
/// negative direction (left or down). Triggers only have a positive direction.
/// </param>
public sealed record GamePadAxisBinding(GamePadAxis Axis, bool Positive = true) : Binding
{
    [JsonIgnore] // Worked out from the binding, so not saved
    public override DeviceKind DeviceKind => DeviceKind.GamePad;

    public override float Read(InputSnapshot snapshot, InputDevice device)
    {
        if (device.Kind != DeviceKind.GamePad)
            return 0f;

        var pad = snapshot.GamePad(device.GamePadIndex);
        float value = Axis switch
        {
            GamePadAxis.LeftStickX => pad.ThumbSticks.Left.X,
            GamePadAxis.LeftStickY => pad.ThumbSticks.Left.Y,
            GamePadAxis.RightStickX => pad.ThumbSticks.Right.X,
            GamePadAxis.RightStickY => pad.ThumbSticks.Right.Y,
            GamePadAxis.LeftTrigger => pad.Triggers.Left,
            GamePadAxis.RightTrigger => pad.Triggers.Right,
            _ => 0f,
        };
        return Math.Clamp(Positive ? value : -value, 0f, 1f);
    }

    [JsonIgnore] // Worked out from the binding, so not saved
    public override string DisplayName => Axis switch
    {
        GamePadAxis.LeftStickX => Positive ? "Left Stick Right" : "Left Stick Left",
        GamePadAxis.LeftStickY => Positive ? "Left Stick Up" : "Left Stick Down",
        GamePadAxis.RightStickX => Positive ? "Right Stick Right" : "Right Stick Left",
        GamePadAxis.RightStickY => Positive ? "Right Stick Up" : "Right Stick Down",
        GamePadAxis.LeftTrigger => "Left Trigger",
        GamePadAxis.RightTrigger => "Right Trigger",
        _ => Axis.ToString(),
    };
}
