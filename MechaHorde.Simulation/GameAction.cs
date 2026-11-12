namespace MechaHorde.Simulation;

/// <summary>
/// The actions a player can take in this game, in addition to moving and looking.
/// </summary>
/// <remarks>
/// Each value is a bit index in a <see cref="CIS580.Simulation.ActionSet"/>, so values must be
/// between 0 and 31. These are placeholders: rename and add actions to suit your game.
/// Menu actions such as pause are not game actions; they are handled outside the simulation.
/// </remarks>
public enum GameAction
{
    Primary = 0,
    Secondary = 1,
}
