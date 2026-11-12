using System;
using CIS580.Simulation;

namespace CIS580.Input;

/// <summary>
/// A player on this machine: who they are in the simulation, where their input comes from,
/// and their input preferences.
/// </summary>
public sealed class LocalPlayer
{
    /// <summary>
    /// The player's id in the simulation
    /// </summary>
    public PlayerId Id { get; }

    /// <summary>
    /// Where the player's input comes from
    /// </summary>
    public InputSource Input { get; }

    /// <summary>
    /// The player's input preferences (e.g. invert Y)
    /// </summary>
    public InputProfile Profile { get; }

    /// <summary>
    /// Feeds the player's commands to the simulation; add this to the SessionRunner
    /// </summary>
    public LocalCommandSource Commands { get; } = new();

    public LocalPlayer(PlayerId id, InputSource input, InputProfile profile)
    {
        Id = id;
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    /// <summary>
    /// Reads the player's input and submits this frame's command
    /// </summary>
    /// <typeparam name="TAction">The game's action enum</typeparam>
    public void SubmitCommand<TAction>(InputState input) where TAction : struct, Enum
        => Commands.Submit(CommandBuilder.Build<TAction>(Input, Profile, input));

    /// <summary>
    /// Submits a command that does nothing (e.g. while the player is in a menu)
    /// </summary>
    public void SubmitIdle() => Commands.Submit(PlayerCommand.Empty);

    public override string ToString() => $"{Id} ({Input.Name})";
}
