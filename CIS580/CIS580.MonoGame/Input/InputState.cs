using Microsoft.Xna.Framework.Input;

namespace CIS580.Input;

/// <summary>
/// Tracks the current and prior frame's state of the keyboard, mouse, and gamepads, so that
/// we can tell both whether an input is held down, and whether it was newly pressed this frame.
/// </summary>
/// <remarks>
/// The game should call Update() once at the start of each frame. Most code should not read
/// devices from this class directly, but through an <see cref="InputSource"/>, which applies
/// the player's (rebindable) control scheme.
/// </remarks>
public class InputState
{
    /// <summary>
    /// The number of gamepads tracked
    /// </summary>
    public const int MaxGamePads = 4;

    private InputSnapshot _current = new();
    private InputSnapshot _previous = new();

    private readonly bool[] _gamePadWasConnected = new bool[MaxGamePads];

    /// <summary>
    /// The state of the input devices this frame
    /// </summary>
    public InputSnapshot Current => _current;

    /// <summary>
    /// The state of the input devices on the prior frame
    /// </summary>
    public InputSnapshot Previous => _previous;

    /// <summary>
    /// Reads the current state of the keyboard, mouse, and gamepads.
    /// </summary>
    public void Update()
    {
        for (int i = 0; i < MaxGamePads; i++)
        {
            // Dead zones are applied by the CommandBuilder, according to the player's InputProfile
            _polledGamePads[i] = GamePad.GetState(i, GamePadDeadZone.None);
        }

        Update(Keyboard.GetState(), Mouse.GetState(), _polledGamePads);
    }

    private readonly GamePadState[] _polledGamePads = new GamePadState[MaxGamePads];

    /// <summary>
    /// Updates the input state with the given device states, rather than reading the devices.
    /// This is useful for testing, and for playing back recorded input.
    /// </summary>
    public void Update(KeyboardState keyboard, MouseState mouse, params GamePadState[] gamePads)
    {
        // Swap the snapshots, so the current state becomes the previous state without allocating
        (_previous, _current) = (_current, _previous);

        _current.Keyboard = keyboard;
        _current.Mouse = mouse;
        for (int i = 0; i < MaxGamePads; i++)
        {
            var state = gamePads != null && i < gamePads.Length ? gamePads[i] : GamePadState.Default;
            _current.SetGamePad(i, state);
            if (state.IsConnected)
                _gamePadWasConnected[i] = true;
        }
    }

    /// <summary>
    /// Determines if the gamepad has been connected at any point since the game started
    /// </summary>
    public bool GamePadWasConnected(int index)
        => index >= 0 && index < MaxGamePads && _gamePadWasConnected[index];

    /// <summary>
    /// Determines if a gamepad that was connected has since been disconnected
    /// </summary>
    public bool GamePadWasDisconnected(int index)
        => GamePadWasConnected(index) && !_current.GamePad(index).IsConnected;

    /// <summary>
    /// Determines if a key is currently held down.
    /// </summary>
    public bool IsKeyDown(Keys key) => _current.Keyboard.IsKeyDown(key);

    /// <summary>
    /// Determines if a key was newly pressed during this frame.
    /// </summary>
    public bool WasKeyPressed(Keys key) => _current.Keyboard.IsKeyDown(key) && _previous.Keyboard.IsKeyUp(key);

    /// <summary>
    /// Determines if a gamepad button is currently held down.
    /// </summary>
    public bool IsButtonDown(Buttons button, int gamePad) => _current.GamePad(gamePad).IsButtonDown(button);

    /// <summary>
    /// Determines if a gamepad button was newly pressed during this frame.
    /// </summary>
    public bool WasButtonPressed(Buttons button, int gamePad)
        => _current.GamePad(gamePad).IsButtonDown(button) && _previous.GamePad(gamePad).IsButtonUp(button);
}
