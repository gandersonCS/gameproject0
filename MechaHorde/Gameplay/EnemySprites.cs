using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MechaHorde.Gameplay;

/// <summary>
/// The enemy sprite sheets, loaded once and shared by every enemy.
/// </summary>
/// <remarks>
/// Each sheet is a single row of equally sized frames (see Assets/Textures/Sprites/Enemies/):
/// <list type="bullet">
/// <item>RobotGunner.png - 64x64 frames: 0 stand, 1 turn, 2 shoot, 3-4 hover, 5 fall, 6 land, 7-11 gibs</item>
/// <item>SecuriBot.png - 13x29 frames: 0-4 green scan, 5-9 red scan, 10-13 opening (split shot)</item>
/// <item>GreenMech.png - 140x108 frames: 0 stand left, 1 stand right, 2-3 shoot left, 4-5 shoot right, 6 jump left, 7 jump right</item>
/// </list>
/// </remarks>
public sealed class EnemySprites
{
    public const int RobotGunnerFrameWidth = 64;
    public const int RobotGunnerFrameHeight = 64;

    public const int SecuriBotFrameWidth = 13;
    public const int SecuriBotFrameHeight = 29;

    public const int GreenMechFrameWidth = 140;
    public const int GreenMechFrameHeight = 108;

    public Texture2D RobotGunner { get; private set; }
    public Texture2D SecuriBot { get; private set; }
    public Texture2D GreenMech { get; private set; }

    public void LoadContent(ContentManager content)
    {
        RobotGunner = content.Load<Texture2D>("Textures/Sprites/Enemies/RobotGunner");
        SecuriBot = content.Load<Texture2D>("Textures/Sprites/Enemies/SecuriBot");
        GreenMech = content.Load<Texture2D>("Textures/Sprites/Enemies/GreenMech");
    }

    /// <summary>
    /// The source rectangle of frame <paramref name="index"/> in a one-row sheet
    /// </summary>
    public static Rectangle Frame(int index, int width, int height) => new(index * width, 0, width, height);
}
