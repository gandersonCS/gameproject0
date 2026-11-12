using System;
using System.Collections.Generic;
using System.Linq;
using CIS580.Input;
using MechaHorde.Simulation;

namespace MechaHorde.Input;

/// <summary>
/// The game's controls as the player has set them up, and the input sources built from them.
/// </summary>
/// <remarks>
/// <para>
/// There is one scheme of each kind, shared by every input source, so rebinding a control (on
/// the Controls screen) changes it everywhere at once. The schemes start as the
/// <see cref="DefaultControls"/>, with the player's saved changes applied on top.
/// </para>
/// <para>
/// A single instance is shared through the game's services.
/// </para>
/// </remarks>
public sealed class GameControls
{
    public GameControls()
    {
        Set = new ControlSet(new[]
        {
            DefaultControls.Keyboard(),
            DefaultControls.KeyboardLeft(),
            DefaultControls.KeyboardRight(),
            DefaultControls.GamePad(),
        });
    }

    /// <summary>
    /// All the schemes, with their defaults (for resetting and saving)
    /// </summary>
    public ControlSet Set { get; }

    /// <summary>
    /// The whole keyboard and mouse, for a player who isn't sharing them
    /// </summary>
    public ControlScheme Keyboard => Set.Schemes[0];

    /// <summary>
    /// The left side of a shared keyboard
    /// </summary>
    public ControlScheme KeyboardLeft => Set.Schemes[1];

    /// <summary>
    /// The right side of a shared keyboard
    /// </summary>
    public ControlScheme KeyboardRight => Set.Schemes[2];

    /// <summary>
    /// A gamepad
    /// </summary>
    public ControlScheme GamePad => Set.Schemes[3];

    /// <summary>
    /// The controls players can rebind: the ones used during play. These must not share inputs
    /// with each other. (The menu controls can't be rebound, so players can't lock themselves
    /// out of the menus.)
    /// </summary>
    public static IReadOnlyList<string> Rebindable { get; } = new[]
        {
            StandardControls.MoveUp, StandardControls.MoveDown,
            StandardControls.MoveLeft, StandardControls.MoveRight,
        }
        .Concat(Enum.GetNames<GameAction>())
        .Append(StandardControls.Pause)
        .ToArray();

    /// <summary>
    /// The devices a scheme is used with, e.g. for listening for an input to bind
    /// </summary>
    public IReadOnlyList<InputDevice> DevicesFor(ControlScheme scheme) => scheme == GamePad
        ? Enumerable.Range(0, InputState.MaxGamePads).Select(InputDevice.ForGamePad).ToArray()
        : new[] { InputDevice.KeyboardMouse };

    /// <summary>
    /// The input source for a single player, who can use the keyboard and mouse or a gamepad
    /// interchangeably
    /// </summary>
    /// <param name="gamePadIndex">Which gamepad the player uses</param>
    public InputSource SinglePlayer(int gamePadIndex) => new("Player",
        new InputSource.Assignment(InputDevice.KeyboardMouse, Keyboard),
        new InputSource.Assignment(InputDevice.ForGamePad(gamePadIndex), GamePad));

    /// <summary>
    /// The input source for a single player who chose to play using the given source (e.g. the
    /// keyboard, or a particular gamepad, in the main menu)
    /// </summary>
    public InputSource SinglePlayerFor(InputSource chosenWith)
    {
        foreach (var assignment in chosenWith.Assignments)
        {
            if (assignment.Device.Kind == DeviceKind.GamePad)
                return SinglePlayer(assignment.Device.GamePadIndex);
        }
        return SinglePlayer(0);
    }

    /// <summary>
    /// The input sources menus listen to when any player may use them: the whole keyboard,
    /// and every gamepad
    /// </summary>
    public InputSource[] AllDevices()
    {
        var sources = new InputSource[1 + InputState.MaxGamePads];
        sources[0] = new InputSource(InputDevice.KeyboardMouse, Keyboard);
        for (int i = 0; i < InputState.MaxGamePads; i++)
            sources[i + 1] = new InputSource(InputDevice.ForGamePad(i), GamePad);
        return sources;
    }

    /// <summary>
    /// The input sources people can join a local multiplayer game with: each half of a
    /// shared keyboard, and each gamepad
    /// </summary>
    public InputSource[] LobbyCandidates()
    {
        var sources = new InputSource[2 + InputState.MaxGamePads];
        sources[0] = new InputSource(InputDevice.KeyboardMouse, KeyboardLeft);
        sources[1] = new InputSource(InputDevice.KeyboardMouse, KeyboardRight);
        for (int i = 0; i < InputState.MaxGamePads; i++)
            sources[i + 2] = new InputSource($"Gamepad {i + 1}", new InputSource.Assignment(InputDevice.ForGamePad(i), GamePad));
        return sources;
    }
}
