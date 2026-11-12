using System;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace CIS580.Input;

/// <summary>
/// Detects the next input a player presses on a device, for "press a key to bind" menus.
/// </summary>
public static class BindingCapture
{
    // These Buttons values are really stick directions and triggers, which are captured as
    // analog GamePadAxisBindings instead.
    private static readonly Buttons[] CapturableButtons = Enum.GetValues<Buttons>()
        .Where(b => b is not (Buttons.LeftThumbstickUp or Buttons.LeftThumbstickDown
            or Buttons.LeftThumbstickLeft or Buttons.LeftThumbstickRight
            or Buttons.RightThumbstickUp or Buttons.RightThumbstickDown
            or Buttons.RightThumbstickLeft or Buttons.RightThumbstickRight
            or Buttons.LeftTrigger or Buttons.RightTrigger))
        .ToArray();

    private static readonly GamePadAxisBinding[] CapturableAxes = Enum.GetValues<GamePadAxis>()
        .SelectMany(axis => axis is GamePadAxis.LeftTrigger or GamePadAxis.RightTrigger
            ? new[] { new GamePadAxisBinding(axis, true) }
            : new[] { new GamePadAxisBinding(axis, true), new GamePadAxisBinding(axis, false) })
        .ToArray();

    private static readonly MouseButtonBinding[] CapturableMouseButtons = Enum.GetValues<MouseButton>()
        .Select(b => new MouseButtonBinding(b))
        .ToArray();

    /// <summary>
    /// Finds an input that was newly pressed on the device this frame
    /// </summary>
    /// <param name="input">The input state</param>
    /// <param name="device">The device to watch</param>
    /// <param name="binding">A binding for the pressed input, if one was found</param>
    /// <returns>True if an input was newly pressed</returns>
    public static bool TryCapture(InputState input, InputDevice device, out Binding binding)
    {
        binding = device.Kind == DeviceKind.KeyboardMouse
            ? CaptureKeyboardMouse(input)
            : CaptureGamePad(input, device);
        return binding != null;
    }

    private static Binding CaptureKeyboardMouse(InputState input)
    {
        foreach (var key in input.Current.Keyboard.GetPressedKeys())
        {
            if (input.Previous.Keyboard.IsKeyUp(key))
                return new KeyBinding(key);
        }

        return CapturableMouseButtons.FirstOrDefault(b => NewlyPressed(b, input, InputDevice.KeyboardMouse));
    }

    private static Binding CaptureGamePad(InputState input, InputDevice device)
    {
        // Prefer analog axes, so pushing a stick binds the stick rather than a pseudo-button
        var axis = CapturableAxes.FirstOrDefault(a => NewlyPressed(a, input, device));
        if (axis != null)
            return axis;

        foreach (var button in CapturableButtons)
        {
            if (input.WasButtonPressed(button, device.GamePadIndex))
                return new GamePadButtonBinding(button);
        }
        return null;
    }

    private static bool NewlyPressed(Binding binding, InputState input, InputDevice device)
        => binding.Read(input.Current, device) >= Binding.PressThreshold
           && binding.Read(input.Previous, device) < Binding.PressThreshold;
}
