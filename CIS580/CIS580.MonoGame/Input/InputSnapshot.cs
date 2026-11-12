using Microsoft.Xna.Framework.Input;

namespace CIS580.Input;

/// <summary>
/// The state of every input device at one moment in time
/// </summary>
public sealed class InputSnapshot
{
    /// <summary>
    /// The state of the keyboard
    /// </summary>
    public KeyboardState Keyboard { get; internal set; }

    /// <summary>
    /// The state of the mouse
    /// </summary>
    public MouseState Mouse { get; internal set; }

    private readonly GamePadState[] _gamePads = new GamePadState[InputState.MaxGamePads];

    /// <summary>
    /// Gets the state of the gamepad with the given index (0 to <see cref="InputState.MaxGamePads"/> - 1).
    /// An index outside that range gives the state of a disconnected gamepad.
    /// </summary>
    public GamePadState GamePad(int index)
        => index >= 0 && index < _gamePads.Length ? _gamePads[index] : GamePadState.Default;

    internal void SetGamePad(int index, GamePadState state) => _gamePads[index] = state;
}
