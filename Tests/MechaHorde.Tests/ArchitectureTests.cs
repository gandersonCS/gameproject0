using System.Linq;
using System.Reflection;
using CIS580.Simulation;
using MechaHorde.Simulation;

namespace MechaHorde.Tests;

/// <summary>
/// Tests that enforce the project's architecture rules
/// </summary>
public class ArchitectureTests
{
    [Theory]
    [InlineData(typeof(SessionRunner))]     // CIS580.Core
    [InlineData(typeof(GameSession))]       // MechaHorde.Simulation
    public void SimulationAssemblies_DoNotDependOnMonoGame(System.Type typeInAssembly)
    {
        // The simulation must be able to run in a headless server, where MonoGame's graphics,
        // audio and input are unavailable. Put anything that needs MonoGame in the game client.
        var references = typeInAssembly.Assembly.GetReferencedAssemblies().Select(a => a.Name);

        Assert.DoesNotContain(references, name => name.StartsWith("MonoGame"));
    }
}
