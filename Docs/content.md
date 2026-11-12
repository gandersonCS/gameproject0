# Content

[← Overview](README.md)

Game assets (fonts, textures, sounds, music) live in `MechaHorde.Content/Assets/` and are
built into MonoGame's `.xnb` format by a **Content Builder project**. This is MonoGame 3.8.5+'s
code-based replacement for the older `.mgcb` file and MGCB Editor. You won't find a `.mgcb`
file in this project; rules for building content are written in C#.

## How it works

```
MechaHorde.Content/
  Builder.cs           which assets are built, and how
  Assets/
    Fonts/             menufont.spritefont, gamefont.spritefont, LiberationSans-Regular.ttf
    Textures/          background.png, gradient.png
    Audio/Sfx/         ui_move.wav, ui_confirm.wav, ui_back.wav, player_join.wav, player_leave.wav
    Audio/Music/       menu_loop.wav, game_loop.wav
```

1. Building the game project runs the `BuildContent` target from
   [`CIS580/Build/CIS580.Content.targets`](../CIS580/Build/CIS580.Content.targets) before
   compiling.
2. That target builds `MechaHorde.Content` (a small console program) and runs it with the
   platform (`DesktopGL`), the `Assets` folder, and the game's output folder.
3. The builder builds whatever [`Builder.cs`](../MechaHorde.Content/Builder.cs) includes,
   writing `.xnb` files to the game's output `Content/` folder. Only changed assets are rebuilt.

The game project opts in with two properties and an import (already set up in
`MechaHorde.csproj`):

```xml
<MonoGamePlatform>DesktopGL</MonoGamePlatform>
<ContentBuilderProject>..\MechaHorde.Content\MechaHorde.Content.csproj</ContentBuilderProject>
...
<Import Project="..\CIS580\Build\CIS580.Content.targets" />
```

## Loading content

Content is loaded by its path relative to `Assets/`, **without the extension**:

```csharp
var font  = content.Load<SpriteFont>("Fonts/menufont");     // Assets/Fonts/menufont.spritefont
var tex   = content.Load<Texture2D>("Textures/background"); // Assets/Textures/background.png
var sound = content.Load<SoundEffect>("Audio/Sfx/ui_move"); // Assets/Audio/Sfx/ui_move.wav
```

There are two content managers, with different lifetimes:

| Manager | Lifetime | Used for |
|---|---|---|
| `Game.Content` | The whole run | The UI theme (fonts, panel texture, UI sounds), menu background, music, `GameAudio`'s cues |
| `InGameState`'s own `ContentManager` | One game | Gameplay content (`gamefont`, and your sprites). Unloaded when the game ends. |

Load gameplay assets in `InGameState.Load()` with its content manager, so they're freed when the
player returns to the menu.

## Adding assets

### Files in an existing folder

`Builder.cs` includes whole folders with wildcard rules:

```csharp
content.Include<WildcardRule>("Fonts/*.spritefont");
content.Include<WildcardRule>("Textures/*.png");
content.Include<WildcardRule>("Audio/Sfx/*.wav");
content.Include<WildcardRule>("Audio/Music/*.wav");
```

So a new `.png` in `Textures/`, or a `.wav` in `Audio/Sfx/`, is built automatically: drop it in
and rebuild. Then **add it to [`ASSETS.md`](../ASSETS.md)** with where it came from and its
license.

### New folders or file types

Add a rule to `Builder.GetContentCollection()`:

```csharp
content.Include<WildcardRule>("Sprites/*.png");
```

To give one asset its own importer, processor, or settings:

```csharp
content.Include("Textures/sprites.png", new TextureImporter(),
    new TextureProcessor() { ColorKeyEnabled = false });
```

To copy files unchanged instead of building them (e.g. level data as JSON):

```csharp
content.IncludeCopy<WildcardRule>("Levels/*.json");
```

These examples are also in the comments in `Builder.cs`.

### Fonts

A `.spritefont` file is an XML description of a font. This project's fonts use the bundled
**Liberation Sans** (`Fonts/LiberationSans-Regular.ttf`, SIL Open Font License), referenced **by
file name** in the `<FontName>` element. That way the build doesn't depend on fonts installed on
your machine. To add a size or style, copy `menufont.spritefont`, change `<Size>` (or `<Style>`),
and load it by its new name. To use a different font, put its `.ttf` in `Fonts/`, reference it
by file name, and record its license in `ASSETS.md`.

### Audio

Both sound effects and music are `.wav` files built as `SoundEffect`. Music is built that way so
the [music player](audio.md#musicplayer) can cross-fade between tracks. Make music loops
seamless, and keep them reasonably short, since WAV files are large.

## Placeholders

Every texture and sound in the project is a procedurally generated placeholder (see
`ASSETS.md` and [`Assets/Audio/README.md`](../MechaHorde.Content/Assets/Audio/README.md)).
Replace them with your own, keeping the same names to avoid changing code. Update `ASSETS.md`
when you do.

## Upgrading MonoGame

All package versions are in [`Directory.Packages.props`](../Directory.Packages.props). Change
`MonoGameVersion` to upgrade. The content pipeline's tool packages (`MonoGame.Tool.*`,
`MonoGame.Library.*`) must match the versions `MonoGame.Framework.Content.Pipeline` depends on.
If they don't, the build fails with `NU1605: Detected package downgrade`, naming the version it
needs. See [Getting Started](getting-started.md#troubleshooting-the-build) for other build
problems.

## Debugging the builder

The builder is an ordinary console program. Running it with no arguments
(`dotnet run --project MechaHorde.Content`) builds `Assets/` with default settings for
DesktopGL, which is handy for seeing the full error output for an asset that won't build.
Content build errors appear in the game's build output as lines starting with `[E]`.
