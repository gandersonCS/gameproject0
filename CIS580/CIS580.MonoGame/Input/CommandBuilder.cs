using System;
using System.Numerics;
using CIS580.Simulation;

namespace CIS580.Input;

/// <summary>
/// Turns a player's input into a <see cref="PlayerCommand"/> for the simulation.
/// </summary>
/// <remarks>
/// This is the bridge between devices and the simulation: it reads the standard movement and
/// look controls, applies the player's <see cref="InputProfile"/>, and reads each of the game's
/// actions using the control named after the action's enum value.
/// </remarks>
public static class CommandBuilder
{
    /// <summary>
    /// Builds this frame's command for a player
    /// </summary>
    /// <typeparam name="TAction">The game's action enum</typeparam>
    /// <param name="source">Where the player's input comes from</param>
    /// <param name="profile">The player's input preferences</param>
    /// <param name="input">The current input state</param>
    public static PlayerCommand Build<TAction>(InputSource source, InputProfile profile, InputState input)
        where TAction : struct, Enum
    {
        var move = ReadStick(source, input,
            StandardControls.MoveLeft, StandardControls.MoveRight,
            StandardControls.MoveUp, StandardControls.MoveDown, profile.DeadZone);
        if (profile.InvertMoveY)
            move.Y = -move.Y;

        var look = ReadStick(source, input,
            StandardControls.LookLeft, StandardControls.LookRight,
            StandardControls.LookUp, StandardControls.LookDown, profile.DeadZone);
        if (profile.InvertLookY)
            look.Y = -look.Y;

        var actions = ActionSet.Empty;
        foreach (var action in ActionNames<TAction>.Values)
        {
            if (source.IsDown(ActionNames<TAction>.NameOf(action), input))
                actions = actions.With(action);
        }

        return new PlayerCommand { Move = move, Look = look, Actions = actions };
    }

    /// <summary>
    /// Combines four directional controls into a vector (X right, Y down) of length 0 to 1,
    /// with a circular dead zone.
    /// </summary>
    private static Vector2 ReadStick(InputSource source, InputState input,
        string left, string right, string up, string down, float deadZone)
    {
        var value = new Vector2(
            source.Value(right, input) - source.Value(left, input),
            source.Value(down, input) - source.Value(up, input));

        float length = value.Length();
        if (length <= deadZone)
            return Vector2.Zero;

        // Rescale so movement starts from zero at the edge of the dead zone, rather than
        // jumping straight to the dead zone's size, and never exceeds a length of 1.
        float scaled = Math.Min((length - deadZone) / (1f - deadZone), 1f);
        return value / length * scaled;
    }

    /// <summary>
    /// The values and names of an action enum, looked up once per enum type
    /// </summary>
    private static class ActionNames<TAction> where TAction : struct, Enum
    {
        public static readonly TAction[] Values = Enum.GetValues<TAction>();
        private static readonly string[] Names = Array.ConvertAll(Values, v => v.ToString());

        public static string NameOf(TAction action) => Names[Array.IndexOf(Values, action)];
    }
}
