# MechaHorde

A 2D wave-based side-scrolling shooter where you fight hordes of robots and mechs, built with
MonoGame 3.8.6 (DesktopGL) and .NET 9 for CIS580.

MechaHorde is built on the CIS580 [game architecture](https://github.com/ksu-cis-580/game-architecture):
the architecture provides the base layer (app flow, menus, rebindable input, pausing, saved
settings, audio buses, game feel, and a deterministic simulation), and MechaHorde's own art,
sounds, menu style and gameplay sit on top of it.

## Quick start

```sh
dotnet build MechaHorde.slnx                   # build everything, including content
dotnet run --project MechaHorde                # run the game
dotnet run --project MechaHorde -- --play      # skip the menus and start playing
dotnet test MechaHorde.slnx                    # run the tests
```

In VS Code, open this folder and press F5 ("C#: MechaHorde Debug").

## Controls

| | |
|---|---|
| Move | A / D or Left / Right |
| Jump | W, Up or Space |
| Shoot | Left mouse button (aim with the mouse) |
| Pause | Esc (Start on a gamepad) |
| Menus | Mouse, arrow keys + Enter, or a gamepad |

## Layout

| Folder | What's in it |
|---|---|
| `MechaHorde/` | The game: states, menus, views, audio, input, settings |
| `MechaHorde/Gameplay/` | MechaHorde's gameplay: the `Battlefield` (level), `Player`, `Gun`, `Bullet`, `Enemy`, particles, collisions |
| `MechaHorde/UI/` | MechaHorde's menus (`MechaMenuLayer` gives every menu the panel-button look), theme, and menu background |
| `MechaHorde/Views/` | `LevelView` (draws the level) and the `HudLayer` |
| `MechaHorde.Content/Assets/` | Art, fonts, music and sound effects |
| `MechaHorde.Simulation/` | The architecture's deterministic simulation (not yet used by MechaHorde's gameplay) |
| `CIS580/` | The architecture's shared library (don't edit; it's the template's base layer) |
| `Docs/` | The architecture's documentation |
| `Tests/` | Unit tests |

### Where MechaHorde fits into the architecture

- **Menus.** The architecture's menus (`MainMenuLayer`, `OptionsLayer`, `ControlsLayer`,
  `PauseLayer`) all derive from `MechaMenuLayer`, which draws them in MechaHorde's style
  (futureui panel buttons down the left, the gold title, the blue hover glow) and adds the
  mouse. `GameTheme` sets the fonts, colors and click sounds; `BackgroundLayer` draws the dark red
  sky and the animated planet.
- **Music.** `GameAudio` plays `promise` in the menus and `out` during play.
- **Gameplay.** `InGameState` creates the `Battlefield` (the code that used to run in
  `GameProject0Game` once the campaign started) and updates it while the game isn't paused.
  `LevelView` draws it and `HudLayer` shows the timer and player position over it.
- The gameplay still reads the keyboard and mouse directly; moving it onto the architecture's
  input system and simulation (see `Docs/simulation.md`) is the next step.

The docs in `Docs/` describe the architecture. Where they mention the `WorldView`, MechaHorde's
equivalent is the `LevelView`.

## Assets

Every asset's source and license is listed in [ASSETS.md](ASSETS.md).
