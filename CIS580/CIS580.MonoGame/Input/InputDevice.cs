namespace CIS580.Input;

/// <summary>
/// The kinds of input device
/// </summary>
public enum DeviceKind
{
    /// <summary>
    /// The keyboard and mouse, treated as one device
    /// </summary>
    KeyboardMouse,

    /// <summary>
    /// A gamepad
    /// </summary>
    GamePad,
}

/// <summary>
/// Identifies one input device: the keyboard and mouse, or a particular gamepad
/// </summary>
/// <param name="Kind">The kind of device</param>
/// <param name="GamePadIndex">For a gamepad, which one (0 to 3)</param>
public readonly record struct InputDevice(DeviceKind Kind, int GamePadIndex = 0)
{
    /// <summary>
    /// The keyboard and mouse
    /// </summary>
    public static InputDevice KeyboardMouse => new(DeviceKind.KeyboardMouse);

    /// <summary>
    /// The gamepad with the given index (0 to 3)
    /// </summary>
    public static InputDevice ForGamePad(int index) => new(DeviceKind.GamePad, index);

    public override string ToString()
        => Kind == DeviceKind.GamePad ? $"Gamepad {GamePadIndex + 1}" : "Keyboard & Mouse";
}
