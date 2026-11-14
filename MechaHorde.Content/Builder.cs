/// <summary>
/// Entry point for the Content Builder, which builds the files in Assets/ into .xnb content
/// according to the rules in the Builder class below.
/// </summary>
/// <remarks>
/// The game project runs this automatically (see CIS580/Build/CIS580.Content.targets), passing
/// the platform and directories on the command line. Running it with no arguments uses the
/// defaults below, which is handy for debugging the builder itself.
/// Content is loaded in the game by its path relative to Assets/, without an extension:
/// Assets/Fonts/menufont.spritefont is loaded with Content.Load&lt;SpriteFont&gt;("Fonts/menufont").
/// </remarks>

using Microsoft.Xna.Framework.Content.Pipeline;
using MonoGame.Framework.Content.Pipeline.Builder;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

var defaultArgs = new ContentBuilderParams()
{
    Mode = ContentBuilderMode.Builder,
    WorkingDirectory = $"{AppContext.BaseDirectory}../../",
    SourceDirectory = "Assets",
    Platform = TargetPlatform.DesktopGL
};

var builder = new Builder();

if (args is not null && args.Length > 0)
    builder.Run(args);
else
    builder.Run(defaultArgs);

return builder.FailedToBuild > 0 ? -1 : 0;

/// <summary>
/// Defines which assets are built, and how.
/// </summary>
public class Builder : ContentBuilder
{
    public override IContentCollection GetContentCollection()
    {
        var content = new ContentCollection();

        // Each folder is built with the default importer and processor for its file type.
        // A specific asset can instead be given its own importer, processor and parameters, e.g.:
        //
        //     content.Include("Textures/sprites.png", new TextureImporter(),
        //         new TextureProcessor() { ColorKeyEnabled = false });
        //
        // and files can be copied as-is (e.g. level data) rather than built:
        //
        //     content.IncludeCopy<WildcardRule>("Levels/*.json");

        // A "*" also matches folders, so "Textures/*.png" includes Textures/Menu/, Textures/Level/,
        // etc., and "Audio/Sfx/*.wav" includes Audio/Sfx/Shots/ and Audio/Sfx/Launches/.
        content.Include<WildcardRule>("Fonts/*.spritefont");
        content.Include<WildcardRule>("Textures/*.png");

        // 3D models (the Scraplight pickups), imported with Assimp and built as Models. Each is
        // centred on its origin with its largest side 2 units long; their textures are loaded
        // separately (from Textures/Pickups/) and set on the models' effects in code.
        // FbxImporter (despite the name) reads .obj files too, through the same Assimp importer,
        // but in its XNA-compatible mode, which turns materials into BasicMaterialContent. The
        // plain OpenAssetImporter leaves them as generic MaterialContent, which the game can't
        // load at runtime ("Could not find ContentTypeReader ... MaterialContent").
        foreach (var model in new[] { "scrap_gear", "scrap_propeller",
            "spark_block", "spark_hip", "spark_kick", "spark_bomb", "spark_nuke", "spark_orb" })
            content.Include($"Models/{model}.obj", new FbxImporter(), new ModelProcessor());

        // Both sound effects and music are built as SoundEffects (.wav files import that way by
        // default), so music tracks can be cross-faded by the CIS580.Audio music player.
        content.Include<WildcardRule>("Audio/Sfx/*.wav");
        content.Include<WildcardRule>("Audio/Music/*.wav");

        // An .mp3 would import as a Song by default, which the music player can't cross-fade,
        // so the gameplay music is built as a SoundEffect too
        content.Include("Audio/Music/out.mp3", new Mp3Importer(), new SoundEffectProcessor());

        // Likewise .ogg imports as a Song by default. Each song is an intro and a loop (see
        // Assets/Audio/README.md); .ogg keeps them small in the repository, and they're built
        // into plain sound data so the loop points stay exact.
        foreach (var track in new[]
        {
            "uprising_intro", "uprising_loop",   // Menu
            "calamity_intro", "calamity_loop",   // Gameplay
            "emergent_loop",                     // Pause menu
        })
        {
            content.Include($"Audio/Music/{track}.ogg", new OggImporter(), new SoundEffectProcessor());
        }

        return content;
    }
}
