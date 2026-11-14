# Assets

All assets used in this project, with their provenance and license.
Content assets live in `MechaHorde.Content/Assets/`.

## Textures

- `Textures/Sprites/Sara/SaraMain.png`, `Textures/Sprites/Sara/SaraShooting.png` - "Space Sara!"
  (no helmet) main and shooting frames (`warpsara-nohelmet-anim-sheet-alpha.png`,
  `warpsara-nohelmet-shooting-sheet-alpha.png`), created by Emcee Flesher of OpenGameArt
  [https://opengameart.org/content/space-sara](https://opengameart.org/content/space-sara),
  released under OGA-BY 3.0. Based on an original character by ansimuz (CC0)
  [https://opengameart.org/content/warped-caves-pixel-art-pack](https://opengameart.org/content/warped-caves-pixel-art-pack)
- `Textures/Sprites/M484SpaceSoldier.png` - created by Master484 of OpenGameArt
  [https://opengameart.org/content/space-soldier-m484-games](https://opengameart.org/content/space-soldier-m484-games),
  released under the public domain
- `Textures/Menu/futureui1.png` - created by bart of OpenGameArt
  [https://opengameart.org/content/sci-fi-ui-panels](https://opengameart.org/content/sci-fi-ui-panels),
  released under the public domain
- `Textures/Menu/planet1.png`, `Textures/Menu/Beta 6.png`, `Textures/Menu/noShadow.png` - created by
  StumpyStrust of OpenGameArt [https://opengameart.org/content/beta-6](https://opengameart.org/content/beta-6),
  released under the public domain
- `Textures/Level/background.png`, `Textures/Level/midground.png`, `Textures/Level/foreground.png` -
  Warped City, created by ansimuz of OpenGameArt
  [https://opengameart.org/content/warped-city](https://opengameart.org/content/warped-city),
  released under the public domain
- `Textures/Sprites/OpenGunnerMechs.png`, `Textures/Sprites/Enemies/GreenMech.png` (the green mech
  frames cut from it) - `OpenGunnerMechs.png` from the "Open Gunner Starter Kit", created by Master484
  of OpenGameArt [https://opengameart.org/content/open-gunner-starter-kit](https://opengameart.org/content/open-gunner-starter-kit),
  released under CC-BY 3.0 / OGA-BY 3.0
- `Textures/Sprites/Enemies/RobotGunner.png` - the robot gunner frames (`robotgunner.zip`) packed into
  one sheet, from "Mannyquest Robot Gunner", created by Suppercut of OpenGameArt
  [https://opengameart.org/content/mannyquest-robot-gunner](https://opengameart.org/content/mannyquest-robot-gunner),
  released under CC0 (public domain)
- `Textures/Sprites/Enemies/SecuriBot.png` - the Securi-Bot frames (`securityrobot.png`) packed into
  one sheet, from "Security Robot", created by NARFNra of OpenGameArt
  [https://opengameart.org/content/security-robot](https://opengameart.org/content/security-robot),
  released under CC-BY-SA 3.0. Because of share-alike, this repacked sheet is also released under
  CC-BY-SA 3.0
- `Textures/Effects/drop.png` - rain drop particle
- `Textures/Pickups/spark_block.png` - the spark's texture: the "punch" block's ambient occlusion
  (`punch_a.png`) on dark metal, with its glow (`punch_emit.png`) added on top, from the Block Pack
  (see Models below), released under CC0 (public domain)
- `Textures/gradient.png` - procedurally generated for the CIS580 game architecture, released under
  the public domain

## Models

- `Models/scrap_gear.obj` - `brokengear.obj` from "Broken gear", created by Savino of OpenGameArt
  [https://opengameart.org/content/broken-gear](https://opengameart.org/content/broken-gear),
  released under CC0 (public domain)
- `Models/scrap_propeller.obj` - `brokenpropellor.obj` from "Propellor", created by Savino of
  OpenGameArt [https://opengameart.org/content/propellor](https://opengameart.org/content/propellor),
  released under CC0 (public domain)
- Both were made in Art of Illusion 2.8.1 (2011). Centred, scaled so their largest side is 2 units,
  and triangulated (with flat normals added to the gear) for the game.
- `Models/spark_hip.obj`, `spark_kick.obj`, `spark_bomb.obj`, `spark_nuke.obj`, `spark_orb.obj` -
  `block-hip.fbx`, `block-kick.fbx`, `bomb.fbx`, `nuke.fbx` and `orb.fbx` from the Block Pack, converted
  the same way as `spark_block.obj` below, with matching textures in `Textures/Pickups/` (each made
  from the model's base and glow textures). CC0 (public domain)
- `Models/spark_block.obj` - `block-punch.fbx` from the Block Pack (block pack + shaders),
  released under CC0 (public domain). Converted to OBJ, centred, and scaled so its largest side is
  2 units. TODO: add the pack's page link

## Fonts

- `Fonts/LiberationSans-Regular.ttf` - Liberation Sans 2.1.5, from the Liberation Fonts project
  [https://github.com/liberationfonts/liberation-fonts](https://github.com/liberationfonts/liberation-fonts),
  released under the SIL Open Font License 1.1 (see `Fonts/LiberationSans-LICENSE.txt`).
  Used by all the `.spritefont` files (`aquire`, `robotheroes`, `menufont`, `gamefont`).
- The Aquire font (by Sesohq) and Robot Heroes font (by HAWTPIXEL), from Fontspace
  [https://www.fontspace.com/category/futuristic](https://www.fontspace.com/category/futuristic),
  give `aquire.spritefont` and `robotheroes.spritefont` their names.

## Audio

- `Audio/Music/uprising_intro.ogg`, `Audio/Music/uprising_loop.ogg` (main menu) - edited from:

  > 'Uprising' by Scott Buckley - released under CC-BY 4.0. www.scottbuckley.com.au

  [https://www.scottbuckley.com.au/library/uprising](https://www.scottbuckley.com.au/library/uprising)
  (CC-BY 4.0: [https://creativecommons.org/licenses/by/4.0/](https://creativecommons.org/licenses/by/4.0/)).
  Changes: cut into an intro and a loop; the heroic section was removed.
- `Audio/Music/emergent_loop.ogg` (pause screen) - edited from:

  > 'Emergent' by Scott Buckley - released under CC-BY 4.0. www.scottbuckley.com.au

  [https://www.scottbuckley.com.au/library/emergent](https://www.scottbuckley.com.au/library/emergent)
  (CC-BY 4.0: [https://creativecommons.org/licenses/by/4.0/](https://creativecommons.org/licenses/by/4.0/)).
  Changes: cut into a loop.
- `Audio/Music/calamity_intro.ogg`, `Audio/Music/calamity_loop.ogg` (gameplay) - edited from
  "Calamity (Epic Orchestral Metal)" by Esiltir of OpenGameArt
  [https://opengameart.org/content/calamity-epic-orchestral-metal](https://opengameart.org/content/calamity-epic-orchestral-metal),
  released under CC-BY 4.0. Changes: cut into an intro and a loop. (The author asks to be credited
  and sent a message about games that use it.)
- `Audio/Music/promise.wav` (no longer played) - created by hothxcc of OpenGameArt
  [https://opengameart.org/content/promise](https://opengameart.org/content/promise),
  released under the public domain
- `Audio/Music/out.mp3` (no longer played) - created by G_P of OpenGameArt
  [https://opengameart.org/content/out](https://opengameart.org/content/out),
  released under the public domain
- `Audio/Sfx/Shots/*`, `Audio/Sfx/Launches/*`, `Audio/Sfx/cg1.wav` - created by Michel Baradari of
  OpenGameArt [https://opengameart.org/content/chaingun-pistol-rifle-shotgun-shots](https://opengameart.org/content/chaingun-pistol-rifle-shotgun-shots),
  released under Creative Commons
- `Audio/Sfx/Click_*.wav` - UI Sound Library, created by Little Robot Sound Factory of OpenGameArt
  [https://opengameart.org/content/ui-sound-effects-library](https://opengameart.org/content/ui-sound-effects-library),
  released under Creative Commons
- `Audio/Sfx/player_join.wav`, `Audio/Sfx/player_leave.wav` - procedurally generated for the CIS580
  game architecture, released under the public domain

Gameplay sound effects (converted to 44.1 kHz 16-bit WAV and peak-normalized; the footsteps and
heavy clangs were trimmed and faded from the cast iron recordings):

- `Audio/Sfx/Weapons/rifle_shot_1.wav`, `Audio/Sfx/Weapons/rifle_shot_2.wav` - "Futuristic Weapons /
  Assault Rifle", created by Michael Klier of OpenGameArt
  [https://opengameart.org/node/47021](https://opengameart.org/node/47021),
  released under CC-BY-SA 3.0
- `Audio/Sfx/Weapons/laser_rifle.wav` - "Laser rifle" (made for Red Eclipse), created by Skorpio of
  OpenGameArt [https://opengameart.org/content/laser-rifle](https://opengameart.org/content/laser-rifle),
  released under CC-BY-SA 3.0
- `Audio/Sfx/Weapons/burst_fire.wav` - "Laser weapon burst fire", created by celestialghost8 of
  OpenGameArt [https://opengameart.org/content/laser-weapon-burst-fire](https://opengameart.org/content/laser-weapon-burst-fire),
  released under CC0 (public domain)
- `Audio/Sfx/Robots/mechanical_explosion.wav` - "Mechanical Explosion", created by Spring Spring of
  OpenGameArt [https://opengameart.org/content/mechanical-explosion](https://opengameart.org/content/mechanical-explosion),
  released under CC0 (public domain)
- `Audio/Sfx/Impacts/clink_*.wav`, `Audio/Sfx/Impacts/thud_*.wav`, `Audio/Sfx/Player/pickup.wav`,
  `Audio/Sfx/Waves/wave_gong.wav` - "Metal Impact Sounds", created by Brian MacIntosh (BMacZero) of
  OpenGameArt [https://opengameart.org/content/metal-impact-sounds](https://opengameart.org/content/metal-impact-sounds),
  released under CC0 (public domain)
- `Audio/Sfx/Player/step_*.wav`, `Audio/Sfx/Impacts/clang_heavy_*.wav` - "33 metal clang sounds from
  cast iron pans", created by bart of OpenGameArt
  [https://opengameart.org/node/5249](https://opengameart.org/node/5249),
  released under CC0 (public domain)
- `Audio/Sfx/Player/hurt.wav`, `Audio/Sfx/Player/shield.wav`, `Audio/Sfx/Robots/ground_pound.wav`,
  `Audio/Sfx/Waves/warcry.wav` - "Osare.10 Sound Pack" (made for Osare.10 for Mr.Pfunked), submitted
  by HaelDB to OpenGameArt [https://opengameart.org/content/osare-10-sound-pack](https://opengameart.org/content/osare-10-sound-pack),
  released under CC-BY 3.0 (the page no longer shows its author or license; archived copies list
  CC-BY 3.0)

## Downloaded, not yet in the game

These came from the OpenGameArt collection (see its CREDITS.TXT). They aren't in
`MechaHorde.Content/Assets/` yet; move an entry up to its section above when you add the asset.

| Asset | Author | License | Notes |
|---|---|---|---|
| [Warped City 2](https://opengameart.org/content/warped-city-2) | ansimuz | CC0 | Credit optional ("by Ansimuz") |
| [Space Sara!](https://opengameart.org/content/space-sara) helmet sheets | Emcee Flesher | OGA-BY 3.0 | Based on ansimuz's character (CC0) |
| [Platform Shmup Hero: Warpgal](https://opengameart.org/content/platform-shmup-hero-warpgal) | Emcee Flesher | OGA-BY 3.0 / CC0 | Original character by ansimuz (CC0) |
| [Super Dead Space Gunner Merc Redux](https://opengameart.org/content/super-dead-space-gunner-merc-redux-platform-shmup-hero) | Emcee Flesher | CC-BY 4.0 | |
| [Open Gunner Starter Kit](https://opengameart.org/content/open-gunner-starter-kit) (other sheets) | Master484 | CC-BY 3.0 / OGA-BY 3.0 | |
| [mech(skeletal)](https://opengameart.org/content/mechskeletal) | Centurion_of_war | CC-BY 4.0 | |
| [Mech-48x48](https://opengameart.org/content/mech-48x48) | Redshrike | CC0 | |
| [Side robot with a gun](https://opengameart.org/content/side-robot-with-a-gun) | TheNess | CC0 | |
| [Big Red Mecha](https://opengameart.org/content/big-red-mecha) | DieMango | CC-BY 4.0 | |
| [Space Dimensions (8bit/Retro Version)](https://opengameart.org/content/space-dimensions-8bitretro-version) | Matthew Pablo | CC-BY 3.0 | Follow his attribution instructions: [matthewpablo.com/services](http://www.matthewpablo.com/services) |
| [Theme of Com-Mecha](https://opengameart.org/content/theme-of-com-mecha) | Matthew Pablo | CC-BY 3.0 | Same as above |
| [red engines blue wings & boss_battle_06_alternate](https://opengameart.org/content/red-engines-blue-wings-bossbattle06alternate-zhelanov-vs-nene) | glitchart | CC-BY 4.0 | Remix of "red engines blue wings" by Zhelanov (CC-BY 4.0) and "boss_battle_06_alternate" by nene (CC0) |
| [Kick their ass and Curse of the Moon](https://opengameart.org/content/kick-their-ass-and-curse-of-the-moon-zhelanov-vs-vitalezzz) | glitchart | CC-BY 3.0 | Remix of "Kick their ass" by Alexandr Zhelanov (CC-BY 3.0) and "Curse of the Moon" by vitalezzz (CC0) |
| [Extreme Binary DnB](https://opengameart.org/content/extreme-binary-dnb) | Gobusto | CC-BY-SA 3.0 | |
| [Laser shot](https://opengameart.org/content/laser-shot) | Gumichan01 | CC-BY-SA 3.0 | Credit as "Gumichan01" |

## Code

- The CIS580 game architecture [https://github.com/ksu-cis-580/game-architecture](https://github.com/ksu-cis-580/game-architecture)
- Debug assistance from Gemini

## Application icon

- `MechaHorde/Icon.ico`, `MechaHorde/Icon.bmp` - the default icons from the MonoGame project
  templates [https://monogame.net](https://monogame.net), released under the Microsoft Public License (Ms-PL).
