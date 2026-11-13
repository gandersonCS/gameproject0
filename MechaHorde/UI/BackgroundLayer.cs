using Microsoft.Xna.Framework;
using CIS580.UI;

namespace MechaHorde.UI;

/// <summary>
/// The background behind the menus: MechaHorde's dark red sky with the animated planet
/// drifting across it. It is a backdrop, so it stays visible as menus come and go.
/// </summary>
public class BackgroundLayer : Layer
{
    private static readonly Color SkyTop = new(14, 6, 18);
    private static readonly Color SkyBottom = new(120, 22, 32);

    private readonly PlanetSprite _planet = new();

    public BackgroundLayer()
    {
        IsBackdrop = true;
        InputMode = InputMode.None;
    }

    protected override void Load()
    {
        _planet.LoadContent(Game.Content);
    }

    public override void Update(GameTime gameTime)
    {
        _planet.Update(gameTime, (int)UiSize.X);
    }

    public override void Draw(GameTime gameTime)
    {
        float brightness = Transition.Alpha;

        // Drawn in UI units, so the planet is the same size relative to the screen at any resolution
        SpriteBatch.Begin(transformMatrix: UiTransform);
        // A dusk sky, the same palette as the level: near black at the top, glowing red at the bottom
        var screen = UiSize;
        const int bands = 48;
        float bandHeight = screen.Y / bands;
        for (int i = 0; i < bands; i++)
        {
            float t = i / (bands - 1f);
            var color = Color.Lerp(SkyTop, SkyBottom, t * t);
            SpriteBatch.Draw(Stack.BlankTexture,
                new Rectangle(0, (int)(i * bandHeight), (int)screen.X + 1, (int)bandHeight + 2), color * brightness);
        }
        _planet.Color = new Color(brightness, brightness, brightness);
        _planet.Draw(gameTime, SpriteBatch);
        SpriteBatch.End();
    }
}
