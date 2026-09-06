using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;


namespace gameproject0;

public class CoinSprite
{
    private const float ANIMATION_SPEED = 0.1f;

    private double animationTimer;

    private int animationFrame;

    private Vector2 position;

    private Texture2D texture;

    private BoundingCircle bounds;

    /// <summary>
    /// the bounding volume of the sprite
    /// </summary>
    public BoundingCircle Bounds => bounds;

    public bool Collected;

    /// <summary>
    /// Creates a new coin sprite
    /// </summary>
    /// <param name="position">The position of the sprite in the game</param>
    public CoinSprite(Vector2 position)
    {
        this.position = position;
        this.bounds = new BoundingCircle(position - new Vector2(-8,-8), 16);
    }

    /// <summary>
    /// Loads the sprite texture using the provided ContentManager
    /// </summary>
    /// <param name="content">The ContentManager to load with</param>
    public void LoadContent(ContentManager content)
    {
        texture = content.Load<Texture2D>("coins");
    }

    /// <summary>
    /// Draws the animated sprite using the supplied SpriteBatch
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="spriteBatch">The spritebatch to render with</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if(Collected) return;
        animationTimer += gameTime.ElapsedGameTime.TotalSeconds;

        if(animationTimer > ANIMATION_SPEED)
        {
            animationFrame++;
            if (animationFrame > 7) animationFrame = 0;
            animationTimer -= ANIMATION_SPEED;
        }

        var source = new Rectangle(animationFrame * 16, 0, 16, 16);
        spriteBatch.Draw(texture, position, source, Color.White);
    }
}
