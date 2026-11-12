# Getting Started

[← Overview](README.md)

## Requirements

- The **.NET 9 SDK**. The solution file uses the newer `.slnx` format, which needs SDK 9.0.200 or
  later. Check your version with `dotnet --version`.
- An editor: Visual Studio 2022, JetBrains Rider, or VS Code with the C# Dev Kit. A VS Code
  launch configuration is included in `.vscode/launch.json`.

You don't need to install MonoGame or the MGCB editor separately. Everything comes from NuGet,
and the versions are pinned in [`Directory.Packages.props`](../Directory.Packages.props).

## Build, run, and test

From the repository root:

```sh
dotnet build MechaHorde.slnx                      # build everything, including content
dotnet run --project MechaHorde                   # run the game
dotnet run --project MechaHorde -- --play         # skip the menus and start playing
dotnet test MechaHorde.slnx                       # run all the tests
```

The first build takes longer, because NuGet restores the content pipeline tools and the content
builder compiles every asset. After that, assets are only rebuilt when they change. See
[Content](content.md) for how that works.

`--play` jumps straight into a single-player game with the menus skipped. It is the quickest way
to try out gameplay changes.

## What you'll see

1. **Main menu** — *Play Game*, *Local Multiplayer*, *Options*, *Exit*. Any keyboard or gamepad
   can use it.
2. **Play Game** starts a single-player game on whichever device you chose it with. The world is
   blue; your avatar is the green text `// TODO`, and `Insert Gameplay Here` wanders around on
   its own.
3. **Pause** (Esc, or Start on a gamepad) opens a menu over the dimmed game: *Resume Game*,
   *Options*, *Quit to Main Menu*, *Exit Game*.
4. **Local Multiplayer** opens a lobby. Each person joins with their own device (a gamepad, or
   one half of the keyboard) and presses confirm again to mark themselves ready. Once at least
   two players are ready, a three-second countdown starts the game.
5. **Options** edits difficulty, player color, sound, music and effects volume, and vibration.
   Its *Controls* screen rebinds the in-game controls and turns axis inversion on or off.

## Default controls

| | Keyboard (one player) | Keyboard, left half | Keyboard, right half | Gamepad |
|---|---|---|---|---|
| Move | WASD or arrow keys | WASD | Arrow keys | Left stick or D-pad |
| Look | — | — | — | Right stick |
| Primary action | Space or left mouse | Space | Enter | A or right trigger |
| Secondary action | Left Shift or right mouse | Left Shift | Right Shift | X or left trigger |
| Pause | Esc | Esc | Backspace | Start |
| Menu: move | WASD or arrow keys | WASD | Arrow keys | D-pad or left stick |
| Menu: confirm | Enter or Space | Space | Enter | A |
| Menu: cancel / back | Esc or Backspace | Q | Backspace | B or Back |

A single player can switch freely between the keyboard and their gamepad. The two keyboard
halves are used only when two people share the keyboard in local multiplayer. These defaults are
defined in [`StandardSchemes`](../CIS580/CIS580.MonoGame/Input/StandardSchemes.cs) (movement,
menus, pause) and [`DefaultControls`](../MechaHorde/Input/DefaultControls.cs) (this game's
actions). See [Input](input.md).

## Debug output

Debug builds print to the console:

```
[Settings] /Users/you/Library/Application Support/MechaHorde
[AppFlow] Change to MainMenuState
[AppFlow] Change to LoadingState
[AppFlow] Change to InGameState
[AppFlow] Push PausedState
```

The `[AppFlow]` lines are the quickest way to see what the app is doing when a menu or state
transition misbehaves. See [App Flow](flow.md).

## Where settings are saved

Settings, input preferences, and rebound controls are saved as JSON in a `MechaHorde`
folder inside the user's local application data folder:

| OS | Folder |
|---|---|
| Windows | `%LOCALAPPDATA%\MechaHorde` |
| macOS | `~/Library/Application Support/MechaHorde` |
| Linux | `~/.local/share/MechaHorde` |

Delete the folder to go back to the defaults. See [Settings](settings.md).

## Where to start reading code

1. [`MechaHordeGame.cs`](../MechaHorde/MechaHordeGame.cs): the
   whole frame loop and every shared service, in about 120 lines.
2. [`InGameState.cs`](../MechaHorde/States/InGameState.cs): how a game in progress is wired
   together (simulation, players, views, audio, feedback).
3. [`GameSession.cs`](../MechaHorde.Simulation/GameSession.cs) and
   [`World.cs`](../MechaHorde.Simulation/World.cs): the placeholder simulation you will
   replace.
4. [`WorldView.cs`](../MechaHorde/Views/WorldView.cs): the placeholder drawing you will
   replace.

Then work through the [walkthrough](walkthrough.md), which adds a small feature end to end.

## Troubleshooting the build

| Symptom | Cause and fix |
|---|---|
| `NU1605: Detected package downgrade: MonoGame.Tool...` after changing `MonoGameVersion` | The content pipeline's tool packages (`MonoGame.Tool.*`, `MonoGame.Library.*`) must match the versions `MonoGame.Framework.Content.Pipeline` depends on. Set each one to the version the error message names, in `Directory.Packages.props`. |
| `[E] ... An error occurred trying to start process 'ffprobe'` while building audio | The tool packages must be referenced **directly** in `MechaHorde.Content.csproj`, not only through other packages, so their executables are copied to the output. Don't remove those references. |
| `NETSDK1004: Assets file ... project.assets.json not found` in the content project | The game project must reference the content project (`ReferenceOutputAssembly="false"`) so `dotnet restore` reaches it. [`CIS580.Content.targets`](../CIS580/Build/CIS580.Content.targets) adds this reference. Keep its `<Import>` in the game's `.csproj`. |
