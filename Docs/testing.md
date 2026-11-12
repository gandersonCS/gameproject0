# Testing

[← Overview](README.md)

The solution has two xUnit v3 test projects:

| Project | Tests |
|---|---|
| [`Tests/CIS580.Tests`](../Tests/CIS580.Tests) | The library: simulation runner, random numbers, input (bindings, schemes, sources, menus, lobby, rebinding, JSON), UI (layer stack, menus, transitions), flow, views, audio, feedback, settings |
| [`Tests/MechaHorde.Tests`](../Tests/MechaHorde.Tests) | The game: the simulation's behavior and determinism, and the architecture rules |

Run them all with:

```sh
dotnet test MechaHorde.slnx
```

Or use your editor's test explorer. Shared test settings (xUnit packages, `using Xunit`) are in
[`Tests/Directory.Build.props`](../Tests/Directory.Build.props), so a new test project only needs
its project references.

## The architecture test

[`ArchitectureTests`](../Tests/MechaHorde.Tests/ArchitectureTests.cs) checks that
`CIS580.Core` and `MechaHorde.Simulation` don't reference MonoGame. If it fails, something
in the simulation is using a MonoGame type (often `Microsoft.Xna.Framework.Vector2` instead of
`System.Numerics.Vector2`, or `MathHelper`). Move that code to the client, or replace the type.

## Testing the simulation

The simulation needs no window, graphics, or devices, so it is the easiest and most valuable
part to test. Drive it with scripted players and `runner.Step()`:

```csharp
using System.Numerics;
using CIS580.Simulation;
using MechaHorde.Simulation;

public class MovementTests
{
    [Fact]
    public void HoldingRight_MovesRightAtFullSpeed()
    {
        var session = new GameSession(seed: 1);
        var runner = new SessionRunner(session);
        var player = new PlayerId(1);
        runner.AddPlayer(player, new DelegateCommandSource(_ => new PlayerCommand { Move = Vector2.UnitX }));

        runner.Step();                                   // the player joins on this tick
        var start = session.World.FindPlayer(player).Position;
        for (int i = 0; i < 60; i++)                     // one second
            runner.Step();

        var moved = session.World.FindPlayer(player).Position - start;
        Assert.Equal(PlayerAvatar.Speed, moved.X, precision: 2);
        Assert.Equal(0f, moved.Y);
    }
}
```

`DelegateCommandSource` takes a function of the tick number, so you can script anything:
"press Primary on tick 10", "walk in a circle", "do nothing". To check events, read
`session.Events.Pending` after a step.

**Keep the determinism test passing.** [`GameSessionTests.SameSeedAndCommands_PlayOutIdentically`](../Tests/MechaHorde.Tests/GameSessionTests.cs)
runs two sessions with the same seed and scripted commands and checks they match. When you add
state to the world (scores, enemies, coins), add it to that test's assertions, so a determinism
mistake is caught right away rather than when you try networking.

## Testing input

[`FakeInput`](../Tests/CIS580.Tests/Input/FakeInput.cs) feeds made-up device states into an
`InputState`, one frame at a time, using `InputState.Update(keyboard, mouse, pads)`:

```csharp
var input = new FakeInput();
var source = new InputSource(InputDevice.KeyboardMouse, StandardSchemes.Keyboard());

input.Keys();                    // frame 1: nothing held
input.Keys(Keys.A);              // frame 2: A held
Assert.True(source.WasPressed(StandardControls.MoveLeft, input.State));
```

`FakeInput.Pad(leftStick: new Vector2(-1, 0), buttons: Buttons.A)` builds a connected gamepad
state. With these you can test bindings, `CommandBuilder` output, menu navigation (`UiInput`),
and the lobby, without touching a real device.

## Testing other systems

- **UI:** `LayerStack.UpdateLayerStates` (which layers are covered and focused) is a static
  method that tests can call directly on a list of layers. The library makes its `internal`
  members visible to `CIS580.Tests` with `<InternalsVisibleTo>` in its `.csproj`.
- **Rumble:** pass a fake `setVibration` action to the `Rumble` constructor and record the calls.
- **Hit-stop, screen shake, transitions, faders:** pure logic; advance them with `TimeSpan`s and
  check the results.
- **Audio:** [`TestSounds.Silent()`](../Tests/CIS580.Tests/Audio/TestSounds.cs) makes a real,
  silent `SoundEffect`, and skips the test (`Assert.Skip`) on machines without an audio device.
- **Settings:** point a `SettingsStore` at a temporary folder.

## What isn't automated

Drawing and the running game aren't covered by tests. Check them by running the game. `--play`
skips straight to gameplay, and debug builds log every app state change to the console, which
helps when a menu or transition misbehaves.
